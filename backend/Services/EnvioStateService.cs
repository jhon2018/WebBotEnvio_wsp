namespace WahaSender.Api.Services;

/// <summary>
/// Singleton en memoria que gestiona el estado Play/Pause del motor de envío.
/// Usa un CancellationTokenSource para despertar al BackgroundService de forma
/// reactiva cuando el usuario presiona "Play", evitando polling ciego.
/// </summary>
public sealed class EnvioStateService : IEnvioStateService, IDisposable
{
    private readonly object _lock = new();
    private CancellationTokenSource _wakeUpCts = new();
    private bool _estaActivo = false;

    public bool EstaActivo
    {
        get { lock (_lock) return _estaActivo; }
    }

    public CancellationToken WakeUpToken
    {
        get { lock (_lock) return _wakeUpCts.Token; }
    }

    /// <summary>
    /// Pone el motor en Play y cancela el token de espera para que el
    /// BackgroundService despierte inmediatamente si estaba en delay.
    /// </summary>
    public void Activar()
    {
        CancellationTokenSource? oldCts = null;

        lock (_lock)
        {
            if (_estaActivo) return; // Ya estaba activo, no hacer nada
            _estaActivo = true;

            // Reemplazamos el CTS para emitir la señal de despertar.
            oldCts = _wakeUpCts;
            _wakeUpCts = new CancellationTokenSource();
        }

        // Cancelamos fuera del lock para evitar deadlocks.
        oldCts?.Cancel();
        oldCts?.Dispose();
    }

    /// <summary>
    /// Pone el motor en Pause. El BackgroundService terminará el envío
    /// actual (si lo hay) y luego quedará en espera hasta el próximo Play.
    /// </summary>
    public void Pausar()
    {
        lock (_lock)
        {
            _estaActivo = false;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _wakeUpCts.Dispose();
        }
    }
}
