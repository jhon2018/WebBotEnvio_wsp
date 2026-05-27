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
                        "✨ ¡Hola, {Nombre}! Qué alegría saludarte. Te escribe Betty Farroñan, tu asesora de Banco Santander.\n\n" +
                        "Te comparto una excelente noticia: con una única evaluación de tu DNI, tienes aprobada una *Tarjeta de Crédito* llena de beneficios exclusivos y un *Préstamo Personal* de libre disponibilidad. 🚀\n\n" +
                        "Todo el trámite es 100% digital, rápido y seguro.\n\n" +
                        "¿Te gustaría conocer el límite de tu tarjeta y el monto de tu préstamo?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Confirma aquí: https://wa.me/51995799743?text=Hola%20Betty,%20deseo%20conocer%20mis%20ofertas%20aprobadas"
                },
                new PlantillaMensaje
                {
                    Id         = 2,
                    Indice     = 2,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "🌟 Estimado/a {Nombre}, espero que tengas un día excelente. Soy Betty Farroñan.\n\n" +
                        "Queremos premiar tu buen historial. Solo con tu DNI, Banco Santander te ha pre-aprobado un *Préstamo Personal* para lo que necesites, además de una *Tarjeta de Crédito* con descuentos preferenciales. 💳💰\n\n" +
                        "Ambos productos están listos para ser activados sin papeleos.\n\n" +
                        "¿Me permites brindarte los detalles para coordinar la entrega?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Escríbeme aquí: https://wa.me/51995799743?text=Hola%20Betty,%20quiero%20activar%20mi%20tarjeta%20y%20pr%C3%A9stamo"
                },
                new PlantillaMensaje
                {
                    Id         = 3,
                    Indice     = 3,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "💎 ¡Hola, {Nombre}! Soy Betty Farroñan de Banco Santander.\n\n" +
                        "¡Tu evaluación fue un éxito! Solo presentando tu DNI, has calificado para nuestra campaña VIP: una *Tarjeta de Crédito Santander* y un *Préstamo en Efectivo* inmediato. 🏆\n\n" +
                        "Es la oportunidad perfecta para ordenar tus finanzas y disfrutar de promociones únicas.\n\n" +
                        "¿Te gustaría validar tus montos aprobados en línea?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Solicítalo aquí: https://wa.me/51995799743?text=Hola%20Betty,%20quiero%20validar%20mis%20montos%20aprobados"
                },
                new PlantillaMensaje
                {
                    Id         = 4,
                    Indice     = 4,
                    Tipo       = "prestamo",
                    Activo     = true,
                    CuerpoTexto =
                        "🎉 Buen día, {Nombre}. Qué gusto saludarte, soy Betty Farroñan.\n\n" +
                        "Hoy tienes acceso a una oferta doble e irrepetible: *Préstamo Personal* con tasa preferencial + *Tarjeta de Crédito*, ¡ambos aprobados con una única evaluación de tu DNI! 💼💳\n\n" +
                        "Cero trámites engorrosos, todo es digital y seguro.\n\n" +
                        "¿Prefieres que te envíe la información por este medio o agendamos una llamada corta?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Contáctame aquí: https://wa.me/51995799743?text=Hola%20Betty,%20me%20interesa%20la%20oferta%20doble"
                },
                new PlantillaMensaje
                {
                    Id         = 5,
                    Indice     = 5,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "🥇 ¡Hola, {Nombre}! Te saluda Betty Farroñan, tu ejecutiva Santander.\n\n" +
                        "Tengo grandes noticias para ti: gracias a nuestra evaluación ágil (solo con tu DNI), has desbloqueado dos productos exclusivos:\n" +
                        "✅ *Tarjeta de Crédito* (con exoneración de membresía sujeta a uso).\n" +
                        "✅ *Préstamo Personal* (liquidez inmediata a tu cuenta).\n\n" +
                        "Están listos para ti. ¿Coordinamos la entrega de tu tarjeta a domicilio sin costo?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Escríbeme aquí: https://wa.me/51995799743?text=Hola%20Betty,%20quiero%20coordinar%20la%20entrega%20de%20mi%20tarjeta"
                },
                new PlantillaMensaje
                {
                    Id         = 6,
                    Indice     = 6,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "🌟 {Nombre}, ¡tienes una oportunidad exclusiva esperándote! Soy Betty Farroñan.\n\n" +
                        "Tu perfil ha sido seleccionado por Banco Santander. Con una única validación de tu DNI, te otorgamos una *Tarjeta de Crédito* para tus compras diarias y un *Préstamo Efectivo* para concretar tus proyectos. 🚀💳\n\n" +
                        "¡Sin trámites extras!\n\n" +
                        "¿Te interesa que revisemos el simulador de cuotas y los beneficios de tu tarjeta?\n\n" +
                        "👩‍💼 Betty Farroñan\n" +
                        "👉 Confirma aquí: https://wa.me/51995799743?text=Hola%20Betty,%20quiero%20revisar%20las%20cuotas%20y%20beneficios"
                },
                new PlantillaMensaje
                {
                    Id         = 7,
                    Indice     = 7,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💳 ¡Hola, {Nombre}! Espero que estés genial. Te escribe Betty Farroñan.\n\n" +
                        "Queremos facilitarte la vida. Por eso, con solo verificar tu DNI, Santander te ha aprobado un *Préstamo Personal* y una nueva *Tarjeta de Crédito*. 💰✨\n\n" +
                        "Ambos productos te brindarán el respaldo financiero que mereces.\n\n" +
                        "¿A qué hora te viene bien que te explique cómo activar tus beneficios hoy mismo?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Escríbeme aquí: https://wa.me/51995799743?text=Hola%20Betty,%20quiero%20activar%20mis%20beneficios%20hoy"
                },
                new PlantillaMensaje
                {
                    Id         = 8,
                    Indice     = 8,
                    Tipo       = "tarjeta",
                    Activo     = true,
                    CuerpoTexto =
                        "💎 Buen día, {Nombre}. Es un placer saludarte. Soy Betty Farroñan, de Santander.\n\n" +
                        "Nos complace invitarte a nuestra campaña Premium. Con una sola evaluación de tu DNI, tienes acceso inmediato a una *Tarjeta de Crédito* con bonos de bienvenida y a un *Préstamo Personal* con desembolso ágil. 🌟\n\n" +
                        "Disfruta de la tranquilidad de un respaldo total.\n\n" +
                        "¿Te gustaría que validemos tus datos de forma segura para gestionar la entrega?\n\n" +
                        "👩‍💼 Ejecutiva: Betty Farroñan\n" +
                        "👉 Escríbeme aquí: https://wa.me/51995799743?text=Hola%20Betty,%20quiero%20gestionar%20mis%20productos%20Premium"
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
