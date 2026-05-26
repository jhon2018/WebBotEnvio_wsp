using Microsoft.AspNetCore.Mvc;

namespace WahaSender.Api.Controllers;

/// <summary>
/// Gestiona las imágenes que se adjuntan aleatoriamente a los mensajes de WhatsApp.
/// Las imágenes se almacenan en wwwroot/imagenes/ y son servidas como archivos estáticos.
/// WAHA Docker las descarga desde la URL pública configurada en ImagenesPublicBaseUrl.
///
/// Reglas de negocio:
/// - Máximo 5 imágenes activas (PNG, JPG, JPEG, WEBP).
/// - Subir una imagen con el mismo nombre sobreescribe la existente (no consume un slot extra).
/// - DELETE elimina la imagen permanentemente.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImagenesController : ControllerBase
{
    private static readonly string[] ExtensionesPermitidas = [".jpg", ".jpeg", ".png", ".webp"];
    private const int MaxImagenes = 5;

    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration      _config;
    private readonly ILogger<ImagenesController> _logger;

    public ImagenesController(
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<ImagenesController> logger)
    {
        _env    = env;
        _config = config;
        _logger = logger;
    }

    // ─── Propiedades auxiliares ─────────────────────────────────────────────

    private string CarpetaImagenes
    {
        get
        {
            // WebRootPath puede ser null si wwwroot no existía al arrancar.
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            return Path.Combine(webRoot, "imagenes");
        }
    }

    private string UrlBase =>
        _config["ImagenesPublicBaseUrl"]?.TrimEnd('/') ?? "http://host.docker.internal:5000/imagenes";

    private void EnsureCarpeta()
    {
        if (!Directory.Exists(CarpetaImagenes))
            Directory.CreateDirectory(CarpetaImagenes);
    }

    private bool EsExtensionValida(string nombreArchivo) =>
        ExtensionesPermitidas.Contains(Path.GetExtension(nombreArchivo).ToLowerInvariant());

    // ─── GET /api/imagenes ──────────────────────────────────────────────────
    /// <summary>
    /// Lista todas las imágenes almacenadas con su URL pública y tamaño.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<ImagenDto>> Listar()
    {
        EnsureCarpeta();

        var imagenes = Directory
            .GetFiles(CarpetaImagenes)
            .Where(f => EsExtensionValida(f))
            .OrderBy(f => f)
            .Select(f =>
            {
                var nombre = Path.GetFileName(f);
                var info   = new FileInfo(f);
                return new ImagenDto(nombre, $"{UrlBase}/{nombre}", info.Length, info.LastWriteTimeUtc);
            })
            .ToList();

        return Ok(imagenes);
    }

    // ─── POST /api/imagenes ─────────────────────────────────────────────────
    /// <summary>
    /// Sube una imagen. Reglas:
    /// - Solo JPG, JPEG, PNG, WEBP.
    /// - Máximo 5 imágenes en total. Si ya hay 5 y el archivo es nuevo, devuelve 400.
    /// - Si el nombre ya existe, sobreescribe sin consumir slot extra.
    /// - Tamaño máximo por archivo: 10 MB (configurado en Program.cs).
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImagenDto>> Subir(IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest("Debes enviar un archivo de imagen.");

        if (!EsExtensionValida(archivo.FileName))
            return BadRequest($"Extensión no permitida. Acepta: {string.Join(", ", ExtensionesPermitidas)}");

        EnsureCarpeta();

        // Sanitizar nombre para evitar path-traversal
        var nombreSeguro = Path.GetFileName(archivo.FileName);
        var rutaDestino  = Path.Combine(CarpetaImagenes, nombreSeguro);
        bool esNuevo     = !System.IO.File.Exists(rutaDestino);

        if (esNuevo)
        {
            var actuales = Directory
                .GetFiles(CarpetaImagenes)
                .Count(f => EsExtensionValida(f));

            if (actuales >= MaxImagenes)
                return BadRequest(
                    $"Límite de {MaxImagenes} imágenes alcanzado. Elimina alguna antes de subir otra.");
        }

        await using var stream = System.IO.File.Create(rutaDestino);
        await archivo.CopyToAsync(stream, ct);

        _logger.LogInformation("📸 Imagen {Nombre} {Accion} correctamente.", nombreSeguro, esNuevo ? "subida" : "reemplazada");

        var fileInfo = new FileInfo(rutaDestino);
        return Ok(new ImagenDto(nombreSeguro, $"{UrlBase}/{nombreSeguro}", fileInfo.Length, fileInfo.LastWriteTimeUtc));
    }

    // ─── DELETE /api/imagenes/{nombre} ──────────────────────────────────────
    /// <summary>
    /// Elimina una imagen por su nombre de archivo.
    /// </summary>
    [HttpDelete("{nombre}")]
    public ActionResult Eliminar(string nombre)
    {
        // Seguridad: extraer solo el nombre del archivo, sin directorios.
        var nombreSeguro = Path.GetFileName(nombre);

        if (!EsExtensionValida(nombreSeguro))
            return BadRequest("Nombre de archivo no válido.");

        var ruta = Path.Combine(CarpetaImagenes, nombreSeguro);

        if (!System.IO.File.Exists(ruta))
            return NotFound($"Imagen '{nombreSeguro}' no encontrada.");

        System.IO.File.Delete(ruta);
        _logger.LogInformation("🗑 Imagen {Nombre} eliminada.", nombreSeguro);

        return NoContent();
    }
}

// ─── DTO ─────────────────────────────────────────────────────────────────────

public record ImagenDto(
    string   Nombre,
    string   Url,
    long     TamanoBytes,
    DateTime FechaSubida
);
