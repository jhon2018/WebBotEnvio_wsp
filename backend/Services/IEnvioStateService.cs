namespace WahaSender.Api.Services;

/// <summary>
/// Contrato del servicio que expone la señal de Play/Pause en memoria.
/// Complementa el campo ModoEnvioActivo de la BD con una señal reactiva
/// que el BackgroundService puede await sin hacer queries constantes.
/// </summary>
public interface IEnvioStateService
{
    /// <summary>true si el motor de envío está activo (Play).</summary>
    bool EstaActivo { get; }

    /// <summary>Activa el motor de envío y señala al BackgroundService para que despierte.</summary>
    void Activar();

    /// <summary>Pausa el motor de envío.</summary>
    void Pausar();

    /// <summary>
    /// Token de cancelación que se renueva cada vez que se llama a Activar().
    /// El BackgroundService lo usa como señal de "hay trabajo disponible, despierta".
    /// </summary>
    CancellationToken WakeUpToken { get; }
}
