using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Entities;
using WahaSender.Api.Helpers;

namespace WahaSender.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<LotesController> _logger;

    private const int MaxFileSizeMb  = 10;
    private const int MaxFileSizeBytes = MaxFileSizeMb * 1024 * 1024;

    public LotesController(AppDbContext db, ILogger<LotesController> logger)
    {
        _db     = db;
        _logger = logger;
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
                LoteId          = lote.Id,
                NumeroCelular   = numeroLimpio,
                NombreCliente   = c.Nombre.Trim(),
                Estado          = EstadoDetalle.Pendiente,
                FechaRegistro   = DateTime.UtcNow
            });
        }

        if (detalles.Count == 0)
            return BadRequest($"Todos los {contactos.Count} registros fueron saltados por tener números inválidos.");

        lote.TotalRegistros = detalles.Count;

        // ── Persistir en BD ─────────────────────────────────────────────────────
        _db.LotesEnvios.Add(lote);
        _db.DetallesEnvios.AddRange(detalles);
        await _db.SaveChangesAsync(ct);

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
        [FromQuery] int pagina  = 1,
        [FromQuery] int tamano  = 50,
        [FromQuery] string? estado = null,
        CancellationToken ct    = default)
    {
        var lote = await _db.LotesEnvios.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lote is null) return NotFound($"Lote {id} no encontrado.");

        var query = _db.DetallesEnvios
            .AsNoTracking()
            .Where(d => d.LoteId == id);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(d => d.Estado == estado);

        var total = await query.CountAsync(ct);

        var detalles = await query
            .OrderBy(d => d.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(d => new DetalleDto(
                d.Id, d.NumeroCelular, d.NombreCliente,
                d.MensajeAsignado, d.Estado,
                d.FechaRegistro, d.FechaProcesado,
                d.WahaAckCode, d.MensajeError))
            .ToListAsync(ct);

        return Ok(new DetallesPageDto(total, pagina, tamano, detalles));
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
            d.Estado         = EstadoDetalle.Pendiente;
            d.MensajeError   = null;
            d.FechaProcesado = null;
            d.WahaAckCode    = null;
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
    /// Busca las columnas "Numero" y "Nombre" por nombre (case-insensitive),
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
        int colNumero = -1, colNombre = -1;
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim().ToLowerInvariant();
            if (header is "numero" or "número" or "phone" or "celular" or "telefono" or "teléfono")
                colNumero = cell.Address.ColumnNumber;
            else if (header is "nombre" or "name" or "cliente" or "contacto")
                colNombre = cell.Address.ColumnNumber;
        }

        if (colNumero == -1 || colNombre == -1)
            return Task.FromResult(new List<ContactoRaw>());

        var contactos = new List<ContactoRaw>();
        foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
        {
            var numero = row.Cell(colNumero).GetString().Trim();
            var nombre = row.Cell(colNombre).GetString().Trim();

            if (!string.IsNullOrWhiteSpace(numero))
                contactos.Add(new ContactoRaw(numero, nombre));
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

        if (idxNumero == -1 || idxNombre == -1)
            return new List<ContactoRaw>();

        var contactos = new List<ContactoRaw>();
        while (await csv.ReadAsync())
        {
            var numero = csv.GetField(idxNumero)?.Trim() ?? string.Empty;
            var nombre = csv.GetField(idxNombre)?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(numero))
                contactos.Add(new ContactoRaw(numero, nombre));
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

    private record ContactoRaw(string Numero, string Nombre);
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
    string?   MensajeAsignado,
    string    Estado,
    DateTime  FechaRegistro,
    DateTime? FechaProcesado,
    int?      WahaAckCode,
    string?   MensajeError
);

public record ReintentarDto(
    int    CantidadReencolada,
    string Mensaje
);
