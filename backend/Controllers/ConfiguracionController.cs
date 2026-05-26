using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Entities;
using WahaSender.Api.Services;

namespace WahaSender.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfiguracionController : ControllerBase
{
    private readonly AppDbContext       _db;
    private readonly IEnvioStateService _stateService;

    public ConfiguracionController(AppDbContext db, IEnvioStateService stateService)
    {
        _db           = db;
        _stateService = stateService;
    }

    // GET /api/configuracion
    // Devuelve la configuración actual (fila Id=1).
    [HttpGet]
    public async Task<ActionResult<ConfiguracionDto>> Get(CancellationToken ct)
    {
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync(ct);
        if (config is null) return NotFound("No se encontró la configuración.");

        return Ok(MapToDto(config));
    }

    // PUT /api/configuracion
    // Actualiza los parámetros editables (delays, límite, API key, URL, sesión).
    [HttpPut]
    public async Task<ActionResult<ConfiguracionDto>> Actualizar(
        [FromBody] ActualizarConfiguracionDto dto, CancellationToken ct)
    {
        var config = await _db.Configuraciones.FirstOrDefaultAsync(ct);
        if (config is null) return NotFound();

        if (dto.DelayMinSegundos < 1 || dto.DelayMaxSegundos < dto.DelayMinSegundos)
            return BadRequest("DelayMin debe ser >= 1 y DelayMax >= DelayMin.");

        config.DelayMinSegundos   = dto.DelayMinSegundos;
        config.DelayMaxSegundos   = dto.DelayMaxSegundos;
        config.FactorIncremento   = dto.FactorIncremento;
        config.WahaApiKey         = dto.WahaApiKey;
        config.WahaEndpointUrl    = dto.WahaEndpointUrl;
        config.WahaSession        = dto.WahaSession;

        await _db.SaveChangesAsync(ct);
        return Ok(MapToDto(config));
    }

    // PUT /api/configuracion/incrementar-limite
    // Suma FactorIncremento al LimiteDiarioActual (botón "Calentar número").
    [HttpPut("incrementar-limite")]
    public async Task<ActionResult<ConfiguracionDto>> IncrementarLimite(CancellationToken ct)
    {
        var config = await _db.Configuraciones.FirstOrDefaultAsync(ct);
        if (config is null) return NotFound();

        config.LimiteDiarioActual += config.FactorIncremento;
        await _db.SaveChangesAsync(ct);

        return Ok(MapToDto(config));
    }

    // PUT /api/configuracion/toggle-envio
    // Alterna entre Play y Paused. Sincroniza la BD y el EnvioStateService en memoria.
    [HttpPut("toggle-envio")]
    public async Task<ActionResult<ConfiguracionDto>> ToggleEnvio(CancellationToken ct)
    {
        var config = await _db.Configuraciones.FirstOrDefaultAsync(ct);
        if (config is null) return NotFound();

        config.ModoEnvioActivo = !config.ModoEnvioActivo;
        await _db.SaveChangesAsync(ct);

        // Sincronizar el estado en memoria para que el BackgroundService
        // reaccione de forma inmediata sin esperar el próximo ciclo de polling.
        if (config.ModoEnvioActivo)
            _stateService.Activar();
        else
            _stateService.Pausar();

        return Ok(MapToDto(config));
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static ConfiguracionDto MapToDto(Configuracion c) => new(
        c.Id,
        c.LimiteDiarioActual,
        c.FactorIncremento,
        c.DelayMinSegundos,
        c.DelayMaxSegundos,
        c.WahaApiKey,
        c.WahaEndpointUrl,
        c.WahaSession,
        c.ModoEnvioActivo
    );
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────

public record ConfiguracionDto(
    int    Id,
    int    LimiteDiarioActual,
    int    FactorIncremento,
    int    DelayMinSegundos,
    int    DelayMaxSegundos,
    string WahaApiKey,
    string WahaEndpointUrl,
    string WahaSession,
    bool   ModoEnvioActivo
);

public record ActualizarConfiguracionDto(
    int    DelayMinSegundos,
    int    DelayMaxSegundos,
    int    FactorIncremento,
    string WahaApiKey,
    string WahaEndpointUrl,
    string WahaSession
);
