namespace WahaSender.Api.Helpers;

/// <summary>
/// Utilidades para detectar errores de WAHA relacionados con números no registrados en WhatsApp.
/// </summary>
public static class WahaErrorHelper
{
    // Palabras clave que identifican un error de "número no registrado en WhatsApp".
    private static readonly string[] KeywordsNoRegistrado =
    [
        "not found", "chat not found", "number not on whatsapp", "invalid wid",
        "notfound", "404", "number does not exist", "no existe", "not registered"
    ];

    /// <summary>
    /// Devuelve true si el mensaje de error indica que el número no está registrado en WhatsApp.
    /// </summary>
    public static bool EsNumeroNoRegistrado(string? mensajeError)
    {
        if (string.IsNullOrWhiteSpace(mensajeError)) return false;
        var lower = mensajeError.ToLowerInvariant();
        return KeywordsNoRegistrado.Any(k => lower.Contains(k));
    }
}
