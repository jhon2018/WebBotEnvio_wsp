using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Entities;

namespace WahaSender.Api.Services;

/// <summary>
/// Motor de envío masivo. Corre como IHostedService independiente del pipeline HTTP.
///
/// Ciclo de vida:
///   1. Verifica ModoEnvioActivo (Play/Pause) leyendo IEnvioStateService.
///   2. Si Paused → espera 5s y vuelve a chequear (loop de bajo consumo).
///   3. Si Play → cuenta envíos de hoy. Si ya alcanzó el límite → duerme hasta mañana.
///   4. Toma el siguiente DetalleEnvio en estado Pendiente (FIFO por Id).
///   5. Selecciona aleatoriamente 1 plantilla activa, interpola {Nombre}.
///   6. POST a WAHA con X-Api-Key. Maneja errores HTTP sin reintentar.
///   7. Actualiza estado a Procesado o Error con timestamp y ACK.
///   8. Aplica Task.Delay aleatorio [DelayMin, DelayMax] segundos.
///   9. Actualiza estado del LoteEnvio si todos sus detalles están terminados.
/// </summary>
public sealed class WahaSenderBackgroundService : BackgroundService
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly IEnvioStateService     _stateService;
    private readonly IHttpClientFactory     _httpClientFactory;
    private readonly ILogger<WahaSenderBackgroundService> _logger;
    private readonly IWebHostEnvironment    _env;
    private readonly IConfiguration         _config;

    // Intervalo de polling cuando el motor está en pausa.
    private static readonly TimeSpan PauseCheckInterval = TimeSpan.FromSeconds(5);

    // Intervalo de espera cuando se alcanzó el límite diario.
    private static readonly TimeSpan LimiteDiarioCheckInterval = TimeSpan.FromMinutes(10);

    public WahaSenderBackgroundService(
        IServiceScopeFactory   scopeFactory,
        IEnvioStateService     stateService,
        IHttpClientFactory     httpClientFactory,
        ILogger<WahaSenderBackgroundService> logger,
        IWebHostEnvironment    env,
        IConfiguration         config)
    {
        _scopeFactory      = scopeFactory;
        _stateService      = stateService;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
        _env               = env;
        _config            = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 WahaSenderBackgroundService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // ── 1. Verificar estado Play/Pause ──────────────────────────────
                if (!_stateService.EstaActivo)
                {
                    _logger.LogDebug("⏸ Motor en pausa. Esperando {Interval}s...", PauseCheckInterval.TotalSeconds);
                    await EsperarConCancelacion(PauseCheckInterval, stoppingToken);
                    continue;
                }

                // ── 2. Crear scope DI para operaciones de base de datos ─────────
                // BackgroundService es singleton pero DbContext debe ser scoped.
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // ── 3. Leer configuración actual ────────────────────────────────
                var config = await db.Configuraciones
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync(stoppingToken);

                if (config is null)
                {
                    _logger.LogError("❌ No se encontró registro de Configuracion en la BD. Esperando 30s...");
                    await EsperarConCancelacion(TimeSpan.FromSeconds(30), stoppingToken);
                    continue;
                }

                // Re-verificar ModoEnvioActivo desde BD (para sincronía tras restart).
                if (!config.ModoEnvioActivo)
                {
                    _stateService.Pausar();
                    continue;
                }

                // ── 4. Verificar límite diario (Opción B acordada) ──────────────
                var hoyUtc    = DateTime.UtcNow.Date;
                var mananaUtc = hoyUtc.AddDays(1);

                var enviadosHoy = await db.DetallesEnvios
                    .CountAsync(d =>
                        d.Estado == EstadoDetalle.Procesado &&
                        d.FechaProcesado >= hoyUtc &&
                        d.FechaProcesado < mananaUtc,
                        stoppingToken);

                if (enviadosHoy >= config.LimiteDiarioActual)
                {
                    _logger.LogInformation(
                        "📊 Límite diario alcanzado ({Enviados}/{Limite}). Esperando {Interval} min.",
                        enviadosHoy, config.LimiteDiarioActual, LimiteDiarioCheckInterval.TotalMinutes);

                    await EsperarConCancelacion(LimiteDiarioCheckInterval, stoppingToken);
                    continue;
                }

                // ── 5. Obtener el siguiente contacto Pendiente (FIFO por Id) ────
                var detalle = await db.DetallesEnvios
                    .Include(d => d.Lote)
                    .Where(d => d.Estado == EstadoDetalle.Pendiente)
                    .OrderBy(d => d.Id)
                    .FirstOrDefaultAsync(stoppingToken);

                if (detalle is null)
                {
                    _logger.LogDebug("📭 Sin registros Pendientes. Esperando 10s...");
                    await EsperarConCancelacion(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                // ── 6. Marcar lote como En Progreso si aún está Pendiente ───────
                if (detalle.Lote is not null && detalle.Lote.Estado == EstadoLote.Pendiente)
                {
                    var lote = await db.LotesEnvios.FindAsync(new object[] { detalle.LoteId }, stoppingToken);
                    if (lote is not null)
                    {
                        lote.Estado = EstadoLote.EnProgreso;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                // ── 7. Seleccionar plantilla aleatoria activa ───────────────────
                var plantillas = await db.PlantillasMensajes
                    .Where(p => p.Activo)
                    .AsNoTracking()
                    .ToListAsync(stoppingToken);

                if (plantillas.Count == 0)
                {
                    _logger.LogError("❌ No hay plantillas activas. El motor no puede continuar.");
                    await EsperarConCancelacion(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                var rng            = Random.Shared;
                var plantilla      = plantillas[rng.Next(plantillas.Count)];
                var mensajeFinal   = plantilla.CuerpoTexto.Replace("{Nombre}", detalle.NombreCliente);
                var chatId         = $"{detalle.NumeroCelular}@c.us";

                // Guardar el mensaje asignado para trazabilidad antes de enviar.
                detalle.MensajeAsignado = mensajeFinal;

                // ── 8. Seleccionar imagen aleatoria (si hay alguna disponible) ──────────
                // NOTA: sendImage requiere WAHA Plus. Con el motor WEBJS gratuito se usa
                // sendText siempre. Si existe una imagen, se adjunta su URL al final del
                // mensaje para que WhatsApp genere la vista previa automáticamente.
                string? imagenUrl = SeleccionarImagenAleatoria(rng);

                // ── 9. Enviar POST a WAHA usando sendImage si hay imagen, de lo contrario sendText ─────────────────────────
                _logger.LogInformation(
                    "📤 Enviando mensaje a {ChatId} | Plantilla #{IndPlantilla} | Imagen: {Img} | Enviados hoy: {Hoy}/{Limite}",
                    chatId, plantilla.Indice,
                    imagenUrl is not null ? Path.GetFileName(imagenUrl) : "ninguna",
                    enviadosHoy + 1, config.LimiteDiarioActual);

                bool exitoso;
                int? ackCode;
                string? mensajeError;

                if (imagenUrl is not null)
                {
                    _logger.LogInformation("📸 Intentando enviar imagen nativa a {ChatId}...", chatId);
                    (exitoso, ackCode, mensajeError) = await EnviarImagenAWahaAsync(
                        config, chatId, mensajeFinal, imagenUrl, stoppingToken);

                    // Si falla el envío de la imagen por cualquier motivo de la versión o API, hacemos un fallback silencioso
                    // a sendText enviando el mensaje de texto puro con el enlace abajo para no perder el envío.
                    if (!exitoso)
                    {
                        _logger.LogWarning("⚠️ Falló sendImage nativo ({Error}). Usando fallback sendText + URL...", mensajeError);
                        var textoFallback = $"{mensajeFinal}\n\n{imagenUrl}";
                        (exitoso, ackCode, mensajeError) = await EnviarAWahaAsync(
                            config, chatId, textoFallback, stoppingToken);
                    }
                }
                else
                {
                    (exitoso, ackCode, mensajeError) = await EnviarAWahaAsync(
                        config, chatId, mensajeFinal, stoppingToken);
                }

                // ── 10. Actualizar estado del DetalleEnvio ──────────────────────────────────
                detalle.FechaProcesado = DateTime.UtcNow;

                if (exitoso)
                {
                    detalle.Estado      = EstadoDetalle.Procesado;
                    detalle.WahaAckCode = ackCode;
                    detalle.MensajeError = null;
                    _logger.LogInformation("✅ Enviado OK → {ChatId} | ACK: {Ack}", chatId, ackCode);
                }
                else
                {
                    detalle.Estado       = EstadoDetalle.Error;
                    detalle.MensajeError = mensajeError;
                    _logger.LogWarning("❌ Error enviando a {ChatId}: {Error}", chatId, mensajeError);
                }

                await db.SaveChangesAsync(stoppingToken);

                // ── 11. Verificar si el lote padre quedó completado ──────────────────
                await ActualizarEstadoLoteAsync(db, detalle.LoteId, stoppingToken);

                // ── 12. Delay aleatorio anti-spam ────────────────────────────────────
                var delaySegundos = rng.Next(config.DelayMinSegundos, config.DelayMaxSegundos + 1);
                _logger.LogDebug("⏳ Esperando {Delay}s antes del próximo envío...", delaySegundos);
                await EsperarConCancelacion(TimeSpan.FromSeconds(delaySegundos), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Cancelación limpia al detener la aplicación. No es un error.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error inesperado en el BackgroundService. Reintentando en 15s...");
                await EsperarConCancelacion(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 WahaSenderBackgroundService detenido.");
    }

    // ─── Métodos privados ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Escanea wwwroot/imagenes/ y devuelve la URL pública de una imagen elegida
    /// aleatoriamente. Retorna null si no hay imágenes disponibles.
    /// </summary>
    private string? SeleccionarImagenAleatoria(Random rng)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var carpeta = Path.Combine(webRoot, "imagenes");

        if (!Directory.Exists(carpeta))
            return null;

        var archivos = Directory.GetFiles(carpeta)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();

        if (archivos.Length == 0)
            return null;

        var nombreImagen = Path.GetFileName(archivos[rng.Next(archivos.Length)]);
        var baseUrl = _config["ImagenesPublicBaseUrl"]?.TrimEnd('/') ?? "http://host.docker.internal:5000/imagenes";
        return $"{baseUrl}/{nombreImagen}";
    }

    /// <summary>
    /// Realiza el POST HTTP a WAHA para enviar un mensaje de texto puro.
    /// </summary>
    private async Task<(bool Exitoso, int? AckCode, string? MensajeError)> EnviarAWahaAsync(
        Entities.Configuracion config,
        string chatId,
        string texto,
        CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WahaClient");

            var payload = new WahaPayload
            {
                Session = config.WahaSession,
                ChatId  = chatId,
                Text    = texto
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpointUrl = config.WahaEndpointUrl.Replace("/sendText", "/sendImage");
            // Para enviar texto puro, nos aseguramos de que termine en /sendText
            var textEndpoint = config.WahaEndpointUrl.Contains("/sendImage")
                ? config.WahaEndpointUrl.Replace("/sendImage", "/sendText")
                : config.WahaEndpointUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, textEndpoint)
            {
                Content = content
            };
            request.Headers.Add("X-Api-Key", config.WahaApiKey);
            request.Headers.Add("Accept", "application/json");

            using var response = await client.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                int? ack = ExtraerAck(responseBody);
                return (true, ack, null);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                return (false, null, $"HTTP {(int)response.StatusCode}: {errorBody[..Math.Min(errorBody.Length, 300)]}");
            }
        }
        catch (TaskCanceledException)
        {
            return (false, null, "Timeout: WAHA no respondió en 30 segundos.");
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"HttpRequestException: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error inesperado: {ex.Message}");
        }
    }

    /// <summary>
    /// Envía una imagen nativa a WAHA con un pie de foto (caption) usando el endpoint /api/sendImage.
    /// </summary>
    private async Task<(bool Exitoso, int? AckCode, string? MensajeError)> EnviarImagenAWahaAsync(
        Entities.Configuracion config,
        string chatId,
        string caption,
        string url,
        CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("WahaClient");

            var payload = new WahaImagePayload
            {
                Session = config.WahaSession,
                ChatId  = chatId,
                Caption = caption,
                File = new WahaFile { Url = url }
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var imageEndpoint = config.WahaEndpointUrl.Contains("/sendText")
                ? config.WahaEndpointUrl.Replace("/sendText", "/sendImage")
                : config.WahaEndpointUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, imageEndpoint)
            {
                Content = content
            };
            request.Headers.Add("X-Api-Key", config.WahaApiKey);
            request.Headers.Add("Accept", "application/json");

            using var response = await client.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                int? ack = ExtraerAck(responseBody);
                return (true, ack, null);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                return (false, null, $"HTTP {(int)response.StatusCode}: {errorBody[..Math.Min(errorBody.Length, 300)]}");
            }
        }
        catch (TaskCanceledException)
        {
            return (false, null, "Timeout: WAHA no respondió en 30 segundos.");
        }
        catch (HttpRequestException ex)
        {
            return (false, null, $"HttpRequestException: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error inesperado: {ex.Message}");
        }
    }

    /// <summary>
    /// Intenta parsear el ACK desde la respuesta JSON de WAHA.
    /// WAHA devuelve un objeto con campo "ack": 0 (sin ack), 1 (enviado), 2 (recibido), 3 (leído).
    /// </summary>
    private static int? ExtraerAck(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ack", out var ackProp) &&
                ackProp.ValueKind == JsonValueKind.Number)
            {
                return ackProp.GetInt32();
            }
        }
        catch { /* JSON malformado — ignorar */ }

        return null;
    }

    /// <summary>
    /// Evalúa si todos los DetalleEnvio de un lote están terminados
    /// (Procesado o Error) y actualiza el estado del LoteEnvio a Completado.
    /// </summary>
    private static async Task ActualizarEstadoLoteAsync(
        AppDbContext db, Guid loteId, CancellationToken ct)
    {
        var hayPendientes = await db.DetallesEnvios
            .AnyAsync(d => d.LoteId == loteId && d.Estado == EstadoDetalle.Pendiente, ct);

        if (!hayPendientes)
        {
            var lote = await db.LotesEnvios.FindAsync(new object[] { loteId }, ct);
            if (lote is not null && lote.Estado != EstadoLote.Completado)
            {
                lote.Estado = EstadoLote.Completado;
                await db.SaveChangesAsync(ct);
            }
        }
    }

    /// <summary>
    /// Espera el tiempo indicado respetando el CancellationToken de la app
    /// Y el WakeUpToken del state service (para despertar ante un Play).
    /// Si cualquiera de los dos cancela, continúa sin lanzar excepción.
    /// </summary>
    private async Task EsperarConCancelacion(TimeSpan duracion, CancellationToken stoppingToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            _stateService.WakeUpToken);

        try
        {
            await Task.Delay(duracion, linked.Token);
        }
        catch (OperationCanceledException)
        {
            // Si fue el stoppingToken, dejar que el loop principal lo detecte.
            // Si fue el WakeUpToken (usuario presionó Play), simplemente continuar.
        }
    }

    // ─── Payloads WAHA ────────────────────────────────────────────────────────────

    private sealed class WahaPayload
    {
        [JsonPropertyName("session")] public string Session { get; set; } = "default";
        [JsonPropertyName("chatId")]  public string ChatId  { get; set; } = string.Empty;
        [JsonPropertyName("text")]    public string Text    { get; set; } = string.Empty;
    }

    private sealed class WahaImagePayload
    {
        [JsonPropertyName("session")] public string Session { get; set; } = "default";
        [JsonPropertyName("chatId")]  public string ChatId  { get; set; } = string.Empty;
        [JsonPropertyName("caption")] public string Caption { get; set; } = string.Empty;
        [JsonPropertyName("file")]    public WahaFile File  { get; set; } = new();
    }

    private sealed class WahaFile
    {
        [JsonPropertyName("url")]     public string Url     { get; set; } = string.Empty;
    }
}
