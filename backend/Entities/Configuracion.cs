namespace WahaSender.Api.Entities;

/// <summary>
/// Tabla singleton (siempre 1 fila con Id=1).
/// Almacena todos los parámetros de control del motor de envío.
/// </summary>
public class Configuracion
{
    public int Id { get; set; }

    /// <summary>
    /// Cantidad máxima de mensajes a enviar en el día calendar actual.
    /// Se incrementa manualmente desde el Dashboard (+FactorIncremento).
    /// </summary>
    public int LimiteDiarioActual { get; set; } = 50;

    /// <summary>
    /// Cuántas unidades suma el botón "Incrementar Límite Diario".
    /// Por defecto: +5 (calentamiento progresivo del número).
    /// </summary>
    public int FactorIncremento { get; set; } = 5;

    /// <summary>
    /// Pausa mínima en segundos entre envíos consecutivos.
    /// Obligatorio para anti-spam. No puede ser 0.
    /// </summary>
    public int DelayMinSegundos { get; set; } = 15;

    /// <summary>
    /// Pausa máxima en segundos entre envíos consecutivos.
    /// El BackgroundService elegirá un valor aleatorio en [DelayMin, DelayMax].
    /// </summary>
    public int DelayMaxSegundos { get; set; } = 45;

    /// <summary>
    /// Token de autenticación para el header X-Api-Key de WAHA Docker.
    /// </summary>
    public string WahaApiKey { get; set; } = string.Empty;

    /// <summary>
    /// URL base del endpoint de WAHA. Configurable para facilitar cambio de puerto.
    /// </summary>
    public string WahaEndpointUrl { get; set; } = "http://localhost:3000/api/sendText";

    /// <summary>
    /// Nombre de la sesión de WAHA. Por defecto "default".
    /// </summary>
    public string WahaSession { get; set; } = "default";

    /// <summary>
    /// Controla si el BackgroundService debe procesar la cola o estar en pausa.
    /// true = Play (procesando), false = Paused (detenido).
    /// </summary>
    public bool ModoEnvioActivo { get; set; } = false;
}
