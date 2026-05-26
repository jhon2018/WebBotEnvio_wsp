using System.Text.RegularExpressions;

namespace WahaSender.Api.Helpers;

/// <summary>
/// Utilidades de sanitización y formateo de números de teléfono.
/// Centraliza toda la lógica de limpieza para que nunca llegue
/// un número malformado al payload de WAHA.
/// </summary>
public static class TelefonoHelper
{
    // Elimina cualquier caracter que no sea dígito.
    private static readonly Regex SoloDigitos = new(@"\D", RegexOptions.Compiled);

    /// <summary>
    /// Sanitiza un número de teléfono y lo formatea con el código de país.
    /// Pasos:
    ///   1. Elimina espacios, guiones, paréntesis, '+' y cualquier no-dígito.
    ///   2. Detecta si el número ya incluye el código de país (evita duplicación).
    ///   3. Devuelve el número listo para armar el chatId: "{numero}@c.us".
    /// </summary>
    /// <param name="numeroRaw">Número tal como viene del Excel (puede ser sucio).</param>
    /// <param name="codigoPais">Código de país sin '+'. Ej: "51" para Perú.</param>
    /// <returns>
    /// Número limpio CON código de país. Ej: "51995799743".
    /// Retorna string.Empty si el resultado tiene menos de 7 dígitos (inválido).
    /// </returns>
    public static string Sanitizar(string numeroRaw, string codigoPais)
    {
        if (string.IsNullOrWhiteSpace(numeroRaw))
            return string.Empty;

        // Paso 1: dejar solo dígitos.
        var soloNumeros = SoloDigitos.Replace(numeroRaw.Trim(), string.Empty);

        if (soloNumeros.Length < 7)
            return string.Empty; // Número claramente inválido

        // Paso 2: si ya comienza con el código de país, no duplicar.
        // Comparamos el inicio del número limpio con el código de país.
        // Ejemplo: codigoPais="51", soloNumeros="51995799743" → ya tiene prefijo.
        // Ejemplo: codigoPais="51", soloNumeros="995799743"   → falta el prefijo.
        if (!soloNumeros.StartsWith(codigoPais))
        {
            soloNumeros = codigoPais + soloNumeros;
        }

        // Paso 3: validación de longitud mínima con código de país incluido.
        // La longitud mínima razonable es codigoPais + 7 dígitos locales.
        if (soloNumeros.Length < codigoPais.Length + 7)
            return string.Empty;

        return soloNumeros;
    }

    /// <summary>
    /// Construye el chatId completo para WAHA a partir del número sanitizado.
    /// Formato WAHA: "{numeroCelular}@c.us"
    /// El número debe venir ya sanitizado (usar Sanitizar() primero).
    /// </summary>
    public static string BuildChatId(string numeroCelularSanitizado)
        => $"{numeroCelularSanitizado}@c.us";
}
