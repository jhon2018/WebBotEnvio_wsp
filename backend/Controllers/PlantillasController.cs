using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Entities;

namespace WahaSender.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlantillasController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlantillasController(AppDbContext db) => _db = db;

    // GET /api/plantillas
    // Devuelve las 8 plantillas ordenadas por Indice.
    [HttpGet]
    public async Task<ActionResult<List<PlantillaDto>>> GetAll(CancellationToken ct)
    {
        var plantillas = await _db.PlantillasMensajes
            .AsNoTracking()
            .OrderBy(p => p.Indice)
            .Select(p => new PlantillaDto(p.Id, p.Indice, p.CuerpoTexto, p.Tipo, p.Activo))
            .ToListAsync(ct);

        return Ok(plantillas);
    }

    // GET /api/plantillas/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlantillaDto>> GetById(int id, CancellationToken ct)
    {
        var p = await _db.PlantillasMensajes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return NotFound($"Plantilla {id} no encontrada.");

        return Ok(new PlantillaDto(p.Id, p.Indice, p.CuerpoTexto, p.Tipo, p.Activo));
    }

    // PUT /api/plantillas/{id}
    // Actualiza el texto y el estado Activo de una plantilla.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlantillaDto>> Actualizar(
        int id,
        [FromBody] ActualizarPlantillaDto dto,
        CancellationToken ct)
    {
        var plantilla = await _db.PlantillasMensajes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (plantilla is null) return NotFound($"Plantilla {id} no encontrada.");

        if (string.IsNullOrWhiteSpace(dto.CuerpoTexto))
            return BadRequest("El texto de la plantilla no puede estar vacío.");

        plantilla.CuerpoTexto = dto.CuerpoTexto.Trim();
        plantilla.Activo      = dto.Activo;

        await _db.SaveChangesAsync(ct);

        return Ok(new PlantillaDto(plantilla.Id, plantilla.Indice, plantilla.CuerpoTexto, plantilla.Tipo, plantilla.Activo));
    }

    // PUT /api/plantillas/batch
    // Guarda todas las plantillas de una vez (botón "Guardar Cambios" del Dashboard).
    [HttpPut("batch")]
    public async Task<ActionResult<List<PlantillaDto>>> ActualizarBatch(
        [FromBody] List<ActualizarPlantillaConIdDto> dtos,
        CancellationToken ct)
    {
        if (dtos is null || dtos.Count == 0)
            return BadRequest("La lista de plantillas no puede estar vacía.");

        var ids       = dtos.Select(d => d.Id).ToList();
        var plantillas = await _db.PlantillasMensajes
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var dto in dtos)
        {
            var p = plantillas.FirstOrDefault(x => x.Id == dto.Id);
            if (p is null) continue;
            if (!string.IsNullOrWhiteSpace(dto.CuerpoTexto))
                p.CuerpoTexto = dto.CuerpoTexto.Trim();
            p.Activo = dto.Activo;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(plantillas
            .OrderBy(p => p.Indice)
            .Select(p => new PlantillaDto(p.Id, p.Indice, p.CuerpoTexto, p.Tipo, p.Activo))
            .ToList());
    }
}

// ─── DTOs ──────────────────────────────────────────────────────────────────────

public record PlantillaDto(
    int    Id,
    int    Indice,
    string CuerpoTexto,
    string Tipo,
    bool   Activo
);

public record ActualizarPlantillaDto(
    string CuerpoTexto,
    bool   Activo
);

public record ActualizarPlantillaConIdDto(
    int    Id,
    string CuerpoTexto,
    bool   Activo
);
