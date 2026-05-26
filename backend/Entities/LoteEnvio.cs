namespace WahaSender.Api.Entities;

/// <summary>
/// Representa un archivo Excel/CSV importado como un lote de envíos.
/// Relación 1:N con DetalleEnvio (un lote contiene muchos contactos).
/// </summary>
public class LoteEnvio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nombre original del archivo subido (ej: "contactos_mayo.xlsx").
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Código de país seleccionado en la UI al momento de la importación.
    /// Ejemplo: "51" para Perú. Se usa para armar el chatId de WAHA.
    /// </summary>
    public string CodigoPais { get; set; } = "51";

    /// <summary>
    /// Fecha y hora UTC en que se importó el archivo.
    /// </summary>
    public DateTime FechaImportacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total de registros válidos que se importaron del archivo.
    /// </summary>
    public int TotalRegistros { get; set; }

    /// <summary>
    /// Estado general del lote.
    /// Valores posibles: 'Pendiente', 'En Progreso', 'Completado'.
    /// </summary>
    public string Estado { get; set; } = EstadoLote.Pendiente;

    // ─── Navegación ────────────────────────────────────────────────────────────
    public ICollection<DetalleEnvio> Detalles { get; set; } = new List<DetalleEnvio>();
}

/// <summary>
/// Constantes para los estados del LoteEnvio. Evita strings mágicos dispersos.
/// </summary>
public static class EstadoLote
{
    public const string Pendiente   = "Pendiente";
    public const string EnProgreso  = "En Progreso";
    public const string Completado  = "Completado";
}
