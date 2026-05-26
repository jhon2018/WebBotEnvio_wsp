using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Data;
using WahaSender.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Servicios ────────────────────────────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "WAHA Sender API", Version = "v1" });
});

// EF Core + SQLite
// La base de datos se crea en la raíz del proyecto si no se especifica ruta absoluta.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=waha_sender.db"
    )
);

// Kestrel: permitir subida de imágenes hasta 10 MB.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// HttpClient dedicado para las llamadas al contenedor WAHA Docker.
// Timeout de 30 segundos por envío individual.
builder.Services.AddHttpClient("WahaClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// EnvioStateService: singleton en memoria para Play/Pause reactivo.
builder.Services.AddSingleton<IEnvioStateService, EnvioStateService>();

// BackgroundService: motor de envío principal.
builder.Services.AddHostedService<WahaSenderBackgroundService>();

// CORS: permite el origen del frontend React en desarrollo.
// En producción, cambiar por el dominio real.
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")   // Vite dev server
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ─── Pipeline ─────────────────────────────────────────────────────────────────

var app = builder.Build();

// Aplicar migraciones pendientes al arrancar automáticamente.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

// Servir archivos estáticos desde wwwroot/ (imágenes adjuntas a mensajes).
app.UseStaticFiles();

app.UseCors("DevPolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
