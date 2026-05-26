namespace WahaSender.Api.Entities;

/// <summary>
/// Un registro individual por contacto dentro de un lote.
/// El BackgroundService actualiza esta entidad tras cada intento de envío.
/// </summary>
public class DetalleEnvio
{
    public int Id { get; set; }

    /// <summary>FK hacia el lote padre.</summary>
    public Guid LoteId { get; set; }

    /// <summary>
    /// Número de celular ya sanitizado y con código de país.
    /// Formato: solo dígitos, sin '+', sin espacios. Ej: "51995799743".
    /// El chatId de WAHA se construye como: NumeroCelular + "@c.us"
    /// </summary>
    public string NumeroCelular { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del cliente leído del Excel. Se usa para reemplazar {Nombre}.
    /// </summary>
    public string NombreCliente { get; set; } = string.Empty;

    /// <summary>
    /// Texto final del mensaje DESPUÉS de la selección aleatoria de plantilla
    /// y la interpolación de {Nombre}. Se guarda para trazabilidad completa.
    /// Null hasta que el BackgroundService asigna la plantilla.
    /// </summary>
    public string? MensajeAsignado { get; set; }

    /// <summary>
    /// Estado del registro.
    /// Valores posibles: 'Pendiente', 'Procesado', 'Error'.
    /// </summary>
    public string Estado { get; set; } = EstadoDetalle.Pendiente;

    /// <summary>Fecha/hora UTC en que se creó este registro (importación).</summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha/hora UTC en que el BackgroundService procesó este envío (exitoso o fallido).
    /// Null si aún no ha sido procesado.
    /// </summary>
    public DateTime? FechaProcesado { get; set; }

    /// <summary>
    /// Código ACK devuelto por WAHA tras el envío exitoso.
    /// Significado: 0=Sin ACK, 1=Enviado, 2=Recibido, 3=Leído.
    /// Null si no se ha procesado o si hubo error antes de llegar a WAHA.
    /// </summary>
    public int? WahaAckCode { get; set; }

    /// <summary>
    /// Mensaje de error o status code HTTP en caso de fallo.
    /// Ej: "HttpRequestException: 401 Unauthorized", "Timeout", etc.
    /// Null si el envío fue exitoso.
    /// </summary>
    public string? MensajeError { get; set; }

    // ─── Navegación ────────────────────────────────────────────────────────────
    public LoteEnvio? Lote { get; set; }
}

/// <summary>
/// Constantes para los estados del DetalleEnvio. Evita strings mágicos dispersos.
/// </summary>
public static class EstadoDetalle
{
    public const string Pendiente  = "Pendiente";
    public const string Procesado  = "Procesado";
    public const string Error      = "Error";
}
