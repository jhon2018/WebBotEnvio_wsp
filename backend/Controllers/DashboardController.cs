using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Entities;

namespace WahaSender.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    // GET /api/dashboard/metricas
    // Endpoint principal del Dashboard: una sola llamada que devuelve todo lo necesario
    // para renderizar las tarjetas de métricas sin múltiples roundtrips.
    [HttpGet("metricas")]
    public async Task<ActionResult<MetricasDto>> GetMetricas(CancellationToken ct)
    {
        var hoyUtc    = DateTime.UtcNow.Date;
        var mananaUtc = hoyUtc.AddDays(1);

        // Consultas en paralelo para minimizar latencia.
        var configTask         = _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync(ct);
        var enviadosHoyTask    = _db.DetallesEnvios.CountAsync(d =>
            d.Estado == EstadoDetalle.Procesado &&
            d.FechaProcesado >= hoyUtc &&
            d.FechaProcesado < mananaUtc, ct);
        var enColaTask         = _db.DetallesEnvios.CountAsync(d => d.Estado == EstadoDetalle.Pendiente, ct);
        var totalErroresTask   = _db.DetallesEnvios.CountAsync(d => d.Estado == EstadoDetalle.Error, ct);

        // Lote más reciente que no esté completado (el "activo").
        var loteActivoTask = _db.LotesEnvios
            .AsNoTracking()
            .Where(l => l.Estado != EstadoLote.Completado)
            .OrderByDescending(l => l.FechaImportacion)
            .Select(l => new LoteActivoDto(
                l.Id,
                l.NombreArchivo,
                l.Estado,
                l.TotalRegistros,
                l.FechaImportacion
            ))
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(configTask, enviadosHoyTask, enColaTask, totalErroresTask, loteActivoTask);

        var config = await configTask;
        if (config is null) return NotFound("Configuración no encontrada.");

        return Ok(new MetricasDto(
            EnviadosHoy:      await enviadosHoyTask,
            LimiteMaximoDia:  config.LimiteDiarioActual,
            MensajesEnCola:   await enColaTask,
            MensajesConError: await totalErroresTask,
            ModoEnvioActivo:  config.ModoEnvioActivo,
            LoteActivo:       await loteActivoTask
        ));
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────

public record MetricasDto(
    int           EnviadosHoy,
    int           LimiteMaximoDia,
    int           MensajesEnCola,
    int           MensajesConError,
    bool          ModoEnvioActivo,
    LoteActivoDto? LoteActivo
);

public record LoteActivoDto(
    Guid     Id,
    string   NombreArchivo,
    string   Estado,
    int      TotalRegistros,
    DateTime FechaImportacion
);
