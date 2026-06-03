using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Entities;
using WahaSender.Api.Helpers;
using WahaSender.Api.Services;

namespace WahaSender.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<LotesController> _logger;
    private readonly IEnvioStateService _stateService;

    private const int MaxFileSizeMb    = 10;
    private const int MaxFileSizeBytes = MaxFileSizeMb * 1024 * 1024;

    public LotesController(AppDbContext db, ILogger<LotesController> logger, IEnvioStateService stateService)
    {
        _db           = db;
        _logger       = logger;
        _stateService = stateService;
    }

    // POST /api/lotes/importar
    // Recibe el archivo Excel/CSV + código de país. Parsea, sanitiza y guarda en BD.
    [HttpPost("importar")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<ActionResult<LoteResumenDto>> Importar(
        IFormFile archivo,
        [FromForm] string codigoPais,
        CancellationToken ct)
    {
        // ── Validaciones de entrada ─────────────────────────────────────────────
        if (archivo is null || archivo.Length == 0)
            return BadRequest("Debe adjuntar un archivo.");

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".csv")
            return BadRequest("Solo se aceptan archivos .xlsx o .csv.");

        if (string.IsNullOrWhiteSpace(codigoPais))
            codigoPais = "51"; // Default: Perú

        // Limpiar el código de país: solo dígitos, sin '+'.
        codigoPais = new string(codigoPais.Where(char.IsDigit).ToArray());

        // ── Parsear el archivo ──────────────────────────────────────────────────
        List<ContactoRaw> contactos;
        try
        {
            contactos = extension == ".xlsx"
                ? await ParseXlsxAsync(archivo, ct)
                : await ParseCsvAsync(archivo, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al parsear el archivo {Archivo}", archivo.FileName);
            return BadRequest($"Error al leer el archivo: {ex.Message}");
        }

        if (contactos.Count == 0)
            return BadRequest("El archivo no contiene registros válidos o las columnas 'Numero' y 'Nombre' no fueron encontradas.");

        // ── Crear el Lote ───────────────────────────────────────────────────────
        var lote = new LoteEnvio
        {
            Id               = Guid.NewGuid(),
            NombreArchivo    = archivo.FileName,
            CodigoPais       = codigoPais,
            FechaImportacion = DateTime.UtcNow,
            Estado           = EstadoLote.Pendiente
        };

        // ── Convertir contactos a DetalleEnvio ──────────────────────────────────
        var detalles = new List<DetalleEnvio>();
        int saltados = 0;

        foreach (var c in contactos)
        {
            var numeroLimpio = TelefonoHelper.Sanitizar(c.Numero, codigoPais);

            if (string.IsNullOrEmpty(numeroLimpio))
            {
                _logger.LogWarning("Número inválido saltado: '{Numero}' (Nombre: {Nombre})", c.Numero, c.Nombre);
                saltados++;
                continue;
            }

            detalles.Add(new DetalleEnvio
            {
                LoteId        = lote.Id,
                NumeroCelular = numeroLimpio,
                NombreCliente = FormatearNombreCompleto(c.Nombre),
                Documento     = string.IsNullOrWhiteSpace(c.Documento) ? null : c.Documento.Trim(),
                Estado        = EstadoDetalle.Pendiente,
                FechaRegistro = DateTime.UtcNow
            });
        }

        if (detalles.Count == 0)
            return BadRequest($"Todos los {contactos.Count} registros fueron saltados por tener números inválidos.");

        lote.TotalRegistros = detalles.Count;

        // ── Persistir en BD ─────────────────────────────────────────────────────
        _db.LotesEnvios.Add(lote);
        _db.DetallesEnvios.AddRange(detalles);
        await _db.SaveChangesAsync(ct);

        // Si el motor ya está en modo "Activo", lo despertamos para que procese de inmediato
        // sin tener que esperar el loop de 10 segundos o requerir un toggle manual de Play/Pause.
        if (_stateService.EstaActivo)
        {
            _stateService.Activar();
        }

        _logger.LogInformation(
            "Lote {LoteId} importado: {Total} registros válidos, {Saltados} saltados.",
            lote.Id, detalles.Count, saltados);

        return Ok(new LoteResumenDto(
            lote.Id,
            lote.NombreArchivo,
            lote.CodigoPais,
            lote.TotalRegistros,
            saltados,
            lote.Estado,
            lote.FechaImportacion
        ));
    }

    // GET /api/lotes
    // Lista todos los lotes ordenados por fecha descendente.
    [HttpGet]
    public async Task<ActionResult<List<LoteResumenDto>>> GetAll(CancellationToken ct)
    {
        var lotes = await _db.LotesEnvios
            .AsNoTracking()
            .OrderByDescending(l => l.FechaImportacion)
            .Select(l => new LoteResumenDto(
                l.Id, l.NombreArchivo, l.CodigoPais, l.TotalRegistros,
                0, l.Estado, l.FechaImportacion))
            .ToListAsync(ct);

        return Ok(lotes);
    }

    // GET /api/lotes/{id}/detalles
    // Devuelve los contactos de un lote con paginación.
    [HttpGet("{id:guid}/detalles")]
    public async Task<ActionResult<DetallesPageDto>> GetDetalles(
        Guid id,
        [FromQuery] int pagina     = 1,
        [FromQuery] int tamano     = 50,
        [FromQuery] string? estado = null,
        [FromQuery] string? busqueda = null,
        CancellationToken ct       = default)
    {
        var lote = await _db.LotesEnvios.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lote is null) return NotFound($"Lote {id} no encontrado.");

        var query = _db.DetallesEnvios
            .AsNoTracking()
            .Where(d => d.LoteId == id);

        // Filtro especial: "No Registrado" no es un Estado en BD sino el flag booleano.
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado == "No Registrado")
                query = query.Where(d => d.EsNumeroNoRegistrado);
            else
                query = query.Where(d => d.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.ToLower();
            bool isDate = DateTime.TryParse(busqueda, out var parsedDate);

            // UTC-5 para Perú.
            DateTime? inicioUtc = null;
            DateTime? finUtc    = null;

            if (isDate)
            {
                inicioUtc = parsedDate.Date.AddHours(5);
                finUtc    = inicioUtc.Value.AddDays(1);
            }

            query = query.Where(d =>
                d.NombreCliente.ToLower().Contains(term) ||
                d.NumeroCelular.Contains(term) ||
                (d.Documento != null && d.Documento.Contains(term)) ||
                (isDate && d.FechaProcesado >= inicioUtc && d.FechaProcesado < finUtc));
        }

        var total = await query.CountAsync(ct);

        var detalles = await query
            .OrderBy(d => d.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(d => new DetalleDto(
                d.Id, d.NumeroCelular, d.NombreCliente, d.Documento,
                d.MensajeAsignado, d.Estado,
                d.FechaRegistro, d.FechaProcesado,
                d.WahaAckCode, d.MensajeError, d.EsNumeroNoRegistrado))
            .ToListAsync(ct);

        return Ok(new DetallesPageDto(total, pagina, tamano, detalles));
    }

    // GET /api/lotes/no-registrados
    // Lista paginada de todos los números marcados como no registrados en WhatsApp.
    // Sirve para el panel de auditoría en Historial.
    [HttpGet("no-registrados")]
    public async Task<ActionResult<NoRegistradosPageDto>> GetNoRegistrados(
        [FromQuery] int pagina          = 1,
        [FromQuery] int tamano          = 50,
        [FromQuery] Guid? loteId        = null,
        [FromQuery] string? busqueda    = null,
        CancellationToken ct            = default)
    {
        var query = _db.DetallesEnvios
            .AsNoTracking()
            .Include(d => d.Lote)
            .Where(d => d.EsNumeroNoRegistrado);

        if (loteId.HasValue)
            query = query.Where(d => d.LoteId == loteId.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.ToLower();
            query = query.Where(d =>
                d.NombreCliente.ToLower().Contains(term) ||
                d.NumeroCelular.Contains(term) ||
                (d.Documento != null && d.Documento.Contains(term)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => d.FechaProcesado)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(d => new NoRegistradoDto(
                d.Id,
                d.NumeroCelular,
                d.NombreCliente,
                d.Documento,
                d.LoteId,
                d.Lote != null ? d.Lote.NombreArchivo : string.Empty,
                d.MensajeError,
                d.FechaProcesado))
            .ToListAsync(ct);

        return Ok(new NoRegistradosPageDto(total, pagina, tamano, items));
    }

    // GET /api/lotes/no-registrados/exportar
    // Devuelve un archivo CSV con todos los números no registrados para descarga/auditoría.
    [HttpGet("no-registrados/exportar")]
    public async Task<IActionResult> ExportarNoRegistrados(
        [FromQuery] Guid? loteId     = null,
        [FromQuery] string? busqueda = null,
        CancellationToken ct         = default)
    {
        var query = _db.DetallesEnvios
            .AsNoTracking()
            .Include(d => d.Lote)
            .Where(d => d.EsNumeroNoRegistrado);

        if (loteId.HasValue)
            query = query.Where(d => d.LoteId == loteId.Value);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var term = busqueda.ToLower();
            query = query.Where(d =>
                d.NombreCliente.ToLower().Contains(term) ||
                d.NumeroCelular.Contains(term) ||
                (d.Documento != null && d.Documento.Contains(term)));
        }

        var datos = await query
            .OrderByDescending(d => d.FechaProcesado)
            .Select(d => new
            {
                Numero    = d.NumeroCelular,
                Nombre    = d.NombreCliente,
                Documento = d.Documento ?? string.Empty,
                Lote      = d.Lote != null ? d.Lote.NombreArchivo : string.Empty,
                Error     = d.MensajeError ?? string.Empty,
                Fecha     = d.FechaProcesado.HasValue
                    ? d.FechaProcesado.Value.AddHours(-5).ToString("yyyy-MM-dd HH:mm:ss")
                    : string.Empty
            })
            .ToListAsync(ct);

        // Generar CSV manualmente (sin dependencias extra).
        var sb = new StringBuilder();
        sb.AppendLine("Numero,Nombre,Documento,Lote,Error,Fecha_Procesado");
        foreach (var r in datos)
        {
            sb.AppendLine(
                $"\"{EscapeCsv(r.Numero)}\",\"{EscapeCsv(r.Nombre)}\",\"{EscapeCsv(r.Documento)}\"," +
                $"\"{EscapeCsv(r.Lote)}\",\"{EscapeCsv(r.Error)}\",\"{r.Fecha}\"");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var nombreArchivo = loteId.HasValue
            ? $"no_registrados_{loteId}.csv"
            : $"no_registrados_todos_{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
    }

    // POST /api/lotes/reintentar-fallidos
    // Cambia todos los registros en 'Error' a 'Pendiente' para reintento.
    // (Decisión de arquitectura: no hay auto-reintento, solo manual desde UI.)
    [HttpPost("reintentar-fallidos")]
    public async Task<ActionResult<ReintentarDto>> ReintentarFallidos(CancellationToken ct)
    {
        var fallidos = await _db.DetallesEnvios
            .Where(d => d.Estado == EstadoDetalle.Error)
            .ToListAsync(ct);

        if (fallidos.Count == 0)
            return Ok(new ReintentarDto(0, "No hay registros en estado Error."));

        foreach (var d in fallidos)
        {
            d.Estado              = EstadoDetalle.Pendiente;
            d.MensajeError        = null;
            d.FechaProcesado      = null;
            d.WahaAckCode         = null;
            d.EsNumeroNoRegistrado = false;
        }

        // Si el lote padre estaba Completado pero tiene fallidos reencolados,
        // volver a estado Pendiente para que el BackgroundService lo retome.
        var loteIds = fallidos.Select(d => d.LoteId).Distinct().ToList();
        var lotes   = await _db.LotesEnvios.Where(l => loteIds.Contains(l.Id)).ToListAsync(ct);
        foreach (var l in lotes.Where(l => l.Estado == EstadoLote.Completado))
            l.Estado = EstadoLote.Pendiente;

        await _db.SaveChangesAsync(ct);

        return Ok(new ReintentarDto(fallidos.Count, $"{fallidos.Count} registro(s) reencolados para reintento."));
    }

    // ─── Parsers privados ──────────────────────────────────────────────────────

    /// <summary>
    /// Parsea un archivo .xlsx con ClosedXML.
    /// Busca las columnas "Numero", "Nombre" y opcionalmente "Documento" por nombre (case-insensitive),
    /// ignorando columnas extra que pueda tener el archivo.
    /// </summary>
    private static Task<List<ContactoRaw>> ParseXlsxAsync(IFormFile archivo, CancellationToken _)
    {
        using var stream    = archivo.OpenReadStream();
        using var workbook  = new XLWorkbook(stream);
        var worksheet       = workbook.Worksheets.First();

        // Detectar fila de encabezados (primera fila no vacía).
        var headerRow = worksheet.RowsUsed().FirstOrDefault();
        if (headerRow is null) return Task.FromResult(new List<ContactoRaw>());

        // Mapear columna por nombre (case-insensitive + trim).
        int colNumero = -1, colNombre = -1, colDocumento = -1;
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim().ToLowerInvariant();
            if (header is "numero" or "número" or "phone" or "celular" or "telefono" or "teléfono")
                colNumero = cell.Address.ColumnNumber;
            else if (header is "nombre" or "name" or "cliente" or "contacto")
                colNombre = cell.Address.ColumnNumber;
            else if (header is "documento" or "dni" or "ruc" or "cedula" or "cédula" or "id")
                colDocumento = cell.Address.ColumnNumber;
        }

        if (colNumero == -1 || colNombre == -1)
            return Task.FromResult(new List<ContactoRaw>());

        var contactos = new List<ContactoRaw>();
        foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
        {
            var numero    = row.Cell(colNumero).GetString().Trim();
            var nombre    = row.Cell(colNombre).GetString().Trim();
            var documento = colDocumento > 0 ? row.Cell(colDocumento).GetString().Trim() : string.Empty;

            if (!string.IsNullOrWhiteSpace(numero))
                contactos.Add(new ContactoRaw(numero, nombre, documento));
        }

        return Task.FromResult(contactos);
    }

    /// <summary>
    /// Parsea un archivo .csv con CsvHelper.
    /// Acepta separadores por coma o punto y coma.
    /// </summary>
    private static async Task<List<ContactoRaw>> ParseCsvAsync(IFormFile archivo, CancellationToken ct)
    {
        using var stream = archivo.OpenReadStream();
        using var reader = new StreamReader(stream);

        // Intentar detectar el delimitador leyendo la primera línea.
        var firstLine = await reader.ReadLineAsync(ct) ?? string.Empty;
        var delimiter = firstLine.Contains(';') ? ";" : ",";
        stream.Position = 0;

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter          = delimiter,
            HeaderValidated    = null,   // No lanzar error si faltan columnas
            MissingFieldFound  = null,   // Ignorar campos faltantes
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()
        };

        using var csv = new CsvReader(new StreamReader(archivo.OpenReadStream()), config);
        await csv.ReadAsync();
        csv.ReadHeader();

        // Buscar los índices de columna por nombre flexible.
        var headers      = csv.HeaderRecord ?? Array.Empty<string>();
        int idxNumero    = EncontrarIndiceHeader(headers, "numero", "número", "phone", "celular", "telefono", "teléfono");
        int idxNombre    = EncontrarIndiceHeader(headers, "nombre", "name", "cliente", "contacto");
        int idxDocumento = EncontrarIndiceHeader(headers, "documento", "dni", "ruc", "cedula", "cédula", "id");

        if (idxNumero == -1 || idxNombre == -1)
            return new List<ContactoRaw>();

        var contactos = new List<ContactoRaw>();
        while (await csv.ReadAsync())
        {
            var numero    = csv.GetField(idxNumero)?.Trim() ?? string.Empty;
            var nombre    = csv.GetField(idxNombre)?.Trim() ?? string.Empty;
            var documento = idxDocumento >= 0 ? csv.GetField(idxDocumento)?.Trim() ?? string.Empty : string.Empty;
            if (!string.IsNullOrWhiteSpace(numero))
                contactos.Add(new ContactoRaw(numero, nombre, documento));
        }

        return contactos;
    }

    private static int EncontrarIndiceHeader(string[] headers, params string[] candidatos)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var h = headers[i].Trim().ToLowerInvariant();
            if (candidatos.Contains(h)) return i;
        }
        return -1;
    }

    private static string FormatearNombreCompleto(string nombreRaw)
    {
        if (string.IsNullOrWhiteSpace(nombreRaw)) return string.Empty;
        var textInfo = new CultureInfo("es-ES", false).TextInfo;
        return textInfo.ToTitleCase(nombreRaw.Trim().ToLowerInvariant());
    }

    private static string EscapeCsv(string value) => value.Replace("\"", "\"\"");

    private record ContactoRaw(string Numero, string Nombre, string Documento = "");
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────

public record LoteResumenDto(
    Guid     Id,
    string   NombreArchivo,
    string   CodigoPais,
    int      TotalRegistros,
    int      RegistrosSaltados,
    string   Estado,
    DateTime FechaImportacion
);

public record DetallesPageDto(
    int             Total,
    int             Pagina,
    int             Tamano,
    List<DetalleDto> Items
);

public record DetalleDto(
    int       Id,
    string    NumeroCelular,
    string    NombreCliente,
    string?   Documento,
    string?   MensajeAsignado,
    string    Estado,
    DateTime  FechaRegistro,
    DateTime? FechaProcesado,
    int?      WahaAckCode,
    string?   MensajeError,
    bool      EsNumeroNoRegistrado
);

public record NoRegistradosPageDto(
    int                   Total,
    int                   Pagina,
    int                   Tamano,
    List<NoRegistradoDto> Items
);

public record NoRegistradoDto(
    int       Id,
    string    NumeroCelular,
    string    NombreCliente,
    string?   Documento,
    Guid      LoteId,
    string    NombreArchivo,
    string?   MensajeError,
    DateTime? FechaProcesado
);

public record ReintentarDto(
    int    CantidadReencolada,
    string Mensaje
);
