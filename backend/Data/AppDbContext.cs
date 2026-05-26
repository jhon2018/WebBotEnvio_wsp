using Microsoft.EntityFrameworkCore;
using WahaSender.Api.Entities;

namespace WahaSender.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Configuracion>    Configuraciones    { get; set; }
    public DbSet<PlantillaMensaje> PlantillasMensajes { get; set; }
    public DbSet<LoteEnvio>        LotesEnvios        { get; set; }
    public DbSet<DetalleEnvio>     DetallesEnvios     { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── Configuracion ─────────────────────────────────────────────────────
        modelBuilder.Entity<Configuracion>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.WahaApiKey).IsRequired(false);

            // Seed: registro único inicial (Id=1).
            // WahaApiKey se completa desde appsettings o desde la UI.
            e.HasData(new Configuracion
            {
                Id                 = 1,
                LimiteDiarioActual = 50,
                FactorIncremento   = 5,
                DelayMinSegundos   = 15,
                DelayMaxSegundos   = 45,
                WahaApiKey         = "119ce04a85dd41818809be61aba87066",
                WahaEndpointUrl    = "http://localhost:3000/api/sendText",
                WahaSession        = "default",
                ModoEnvioActivo    = false
            });
        });

        // ─── PlantillaMensaje ──────────────────────────────────────────────────
        modelBuilder.Entity<PlantillaMensaje>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.CuerpoTexto).IsRequired();
            e.Property(p => p.Tipo).HasMaxLength(50);

            // Seed: las 8 plantillas del spec original (JSON embebido).
            e.HasData(
                new PlantillaMensaje
                {
                    Id         = 1,
                    Indice     = 1,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 ¡Crédito aprobado a sola firma!\n" +
                        "Hola {Nombre}, solo con tu DNI puedes acceder a tu préstamo inmediato.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 2,
                    Indice     = 2,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 Banco Santander tiene un préstamo pre-aprobado para ti, {Nombre}.\n" +
                        "Accede rápido, sin papeleos y con tu DNI.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 3,
                    Indice     = 3,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 ¡Tu oportunidad está aquí, {Nombre}!\n" +
                        "Préstamo personal disponible con aprobación inmediata. Solo necesitas tu DNI.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 4,
                    Indice     = 4,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 ¡Ya tienes tu tarjeta Santander aprobada, {Nombre}!\n" +
                        "Disfruta beneficios exclusivos y compras sin intereses.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 5,
                    Indice     = 5,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 Banco Santander te ofrece tarjeta de crédito con aprobación inmediata, {Nombre}.\n" +
                        "Empieza a disfrutar descuentos y facilidades hoy.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 6,
                    Indice     = 6,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 ¡Activa y disfruta tu tarjeta Santander VISA, {Nombre}!\n" +
                        "Aprovecha promociones y meses sin intereses.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 7,
                    Indice     = 7,
                    Tipo       = "bienvenida",
                    Activo     = true,
                    CuerpoTexto =
                        "👋 Hola {Nombre}, tienes beneficios disponibles en Banco Santander.\n" +
                        "Puedes acceder a préstamo o tarjeta con tu DNI.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 8,
                    Indice     = 8,
                    Tipo       = "bienvenida",
                    Activo     = true,
                    CuerpoTexto =
                        "👋 Banco Santander te da la bienvenida, {Nombre}.\n" +
                        "Tienes opciones de crédito disponibles listas para ti.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                }
            );
        });

        // ─── LoteEnvio ─────────────────────────────────────────────────────────
        modelBuilder.Entity<LoteEnvio>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).ValueGeneratedNever(); // Guid generado desde C#
            e.Property(l => l.NombreArchivo).IsRequired().HasMaxLength(255);
            e.Property(l => l.CodigoPais).IsRequired().HasMaxLength(5);
            e.Property(l => l.Estado).IsRequired().HasMaxLength(20);

            e.HasMany(l => l.Detalles)
             .WithOne(d => d.Lote)
             .HasForeignKey(d => d.LoteId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── DetalleEnvio ──────────────────────────────────────────────────────
        modelBuilder.Entity<DetalleEnvio>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.NumeroCelular).IsRequired().HasMaxLength(20);
            e.Property(d => d.NombreCliente).IsRequired().HasMaxLength(200);
            e.Property(d => d.Estado).IsRequired().HasMaxLength(20);
            e.Property(d => d.MensajeAsignado).IsRequired(false);
            e.Property(d => d.MensajeError).IsRequired(false);

            // Índice para acelerar las queries del BackgroundService
            // que busca registros Pendientes frecuentemente.
            e.HasIndex(d => d.Estado);
            e.HasIndex(d => new { d.Estado, d.FechaProcesado });
        });
    }
}
