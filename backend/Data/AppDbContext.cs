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

            // Seed: las 8 plantillas con textos profesionales (4 préstamo y 4 tarjeta).
            e.HasData(
                new PlantillaMensaje
                {
                    Id         = 1,
                    Indice     = 1,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 ¡Hola, {Nombre}! Qué gusto saludarle. Le escribe Betty Farroñan, asesora de Banco Santander.\n\n" +
                        "Le comento que evaluamos su historial y cuenta con una gran propuesta de *Préstamo de Libre Disponibilidad pre-aprobado* para hoy. El trámite es ágil, seguro y 100% digital, sujeto únicamente a la validación de su DNI.\n\n" +
                        "¿Le interesaría conocer las opciones de plazos y cuotas a su medida?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 2,
                    Indice     = 2,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 Estimado/a {Nombre}, espero que se encuentre excelente. Le saluda Betty Farroñan.\n\n" +
                        "Queremos apoyarle a concretar sus planes e inversiones personales. Por ello, hemos habilitado un *financiamiento de desembolso inmediato a sola firma*. Podrá acceder de manera muy sencilla, sin papeleos ni trámites complejos.\n\n" +
                        "Con gusto le comparto el simulador de cuotas. ¿Le gustaría que lo revisemos juntos?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 3,
                    Indice     = 3,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 Hola, {Nombre}. Espero que esté teniendo una productiva semana. Le escribe Betty Farroñan de Banco Santander.\n\n" +
                        "Le informo que califica para acceder a un *Préstamo Personal con tasa preferencial y aprobación inmediata* gracias a su buen historial financiero. Es una excelente oportunidad para compra de deuda, capital de trabajo o proyectos personales.\n\n" +
                        "¿Desea que valide en línea el monto máximo disponible para su perfil el día de hoy?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 4,
                    Indice     = 4,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💰 Buen día, {Nombre}. Qué gusto saludarle. Le saluda Betty Farroñan, asesora comercial.\n\n" +
                        "Hoy tengo una muy buena noticia: dispone de una campaña especial con un *cupo de financiamiento pre-evaluado* con condiciones de tasa muy competitivas en el mercado actual. Todo el proceso es digital, inmediato y seguro.\n\n" +
                        "¿Le gustaría que coordinemos una breve llamada o prefiere que le envíe la información por este medio?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 5,
                    Indice     = 5,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 Estimado/a {Nombre}, es un verdadero placer saludarle. Le escribe Betty Farroñan.\n\n" +
                        "Me alegra informarle que ha sido seleccionado/a para recibir la exclusiva *Tarjeta de Crédito Santander*, con importantes beneficios, acumulación de millas para sus viajes y atractivos descuentos en los mejores establecimientos.\n\n" +
                        "¿Me permitiría brindarles los detalles para programar el envío sin costo adicional a su domicilio?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 6,
                    Indice     = 6,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 Hola, {Nombre}, ¿cómo está? Le saluda Betty Farroñan del Banco Santander.\n\n" +
                        "Le escribo para compartirle una gran alternativa para su flexibilidad financiera: tiene aprobada su nueva *Tarjeta de Crédito con costo de membresía exonerado* (sujeto a consumo) y la opción exclusiva de realizar compras en cuotas sin intereses en cientos de comercios a nivel nacional.\n\n" +
                        "Escríbame para confirmar su dirección de entrega y activar todos sus beneficios hoy mismo.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 7,
                    Indice     = 7,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 Hola, {Nombre}. Espero que se encuentre muy bien. Le saluda Betty Farroñan.\n\n" +
                        "Para facilitarle sus compras diarias y brindarle un respaldo ante cualquier eventualidad, Banco Santander ha seleccionado su perfil para la asignación directa de su nueva *Tarjeta de Crédito con un límite personalizado*. El proceso de activación es rápido y 100% digital.\n\n" +
                        "Por favor, coménteme en qué horario le convendría recibir una breve explicación de las promociones activas.\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743"
                },
                new PlantillaMensaje
                {
                    Id         = 8,
                    Indice     = 8,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 Buen día, {Nombre}. Le saluda Betty Farroñan, asesora de Banco Santander.\n\n" +
                        "Nos complace invitarle a nuestra campaña preferente de *Tarjetas de Crédito VISA/Mastercard con beneficios Premium*. Disfrute de bonos de bienvenida, acumulación rápida de puntos y la tranquilidad de un respaldo financiero inmediato para lo que necesite.\n\n" +
                        "¿Le gustaría que validemos sus datos para coordinar la entrega de su tarjeta de manera gratuita y segura?\n\n" +
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
