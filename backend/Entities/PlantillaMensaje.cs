namespace WahaSender.Api.Entities;

/// <summary>
/// Una de las 8 plantillas de mensaje configurables.
/// El BackgroundService selecciona una aleatoriamente por cada envío.
/// Soporta la variable {Nombre} para interpolación dinámica.
/// </summary>
public class PlantillaMensaje
{
    public int Id { get; set; }

    /// <summary>
    /// Índice visual del 1 al 8 para identificar la plantilla en la UI.
    /// </summary>
    public int Indice { get; set; }

    /// <summary>
    /// Texto completo del mensaje. Puede contener {Nombre} como variable
    /// que será reemplazada por el NombreCliente del contacto antes de enviar.
    /// Soporta emojis y saltos de línea (\n).
    /// </summary>
    public string CuerpoTexto { get; set; } = string.Empty;

    /// <summary>
    /// Categoría descriptiva (ej: "prestamo", "tarjeta", "bienvenida").
    /// No afecta la lógica, es solo informativo para el usuario.
    /// </summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>
    /// Si es false, el BackgroundService excluye esta plantilla del pool aleatorio.
    /// Permite deshabilitar temporalmente un mensaje sin borrarlo.
    /// </summary>
    public bool Activo { get; set; } = true;
}
