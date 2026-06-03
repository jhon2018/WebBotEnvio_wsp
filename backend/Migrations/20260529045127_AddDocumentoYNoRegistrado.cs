using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WahaSender.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentoYNoRegistrado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Documento",
                table: "DetallesEnvios",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsNumeroNoRegistrado",
                table: "DetallesEnvios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CuerpoTexto",
                value: "✨ ¡Hola, {Nombre}! Qué alegría saludarte. Te escribe *Betty Farroñan*, tu asesora de Banco Santander.\n\nTe comparto una excelente noticia: con una única evaluación de tu DNI, tienes aprobada una *Tarjeta de Crédito* llena de beneficios exclusivos y un *Préstamo Personal* de libre disponibilidad. 🚀\n\nTodo el trámite es 100% digital, rápido y seguro.\n\n¿Te gustaría conocer el límite de tu tarjeta y el monto de tu préstamo?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Confirma aquí: https://wa.link/xe4ext  o 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CuerpoTexto",
                value: "🌟 Estimado/a {Nombre}, espero que tengas un día excelente. Soy *Betty Farroñan*.\n\nQueremos premiar tu buen historial. Solo con tu DNI, Banco Santander te ha pre-aprobado un *Préstamo Personal* para lo que necesites, además de una *Tarjeta de Crédito* con descuentos preferenciales. 💳💰\n\nAmbos productos están listos para ser activados sin papeleos.\n\n¿Me permites brindarte los detalles para coordinar la entrega?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Escríbeme aquí: https://wa.link/xe4ext o 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CuerpoTexto",
                value: "💎 ¡Hola, {Nombre}! Soy *Betty Farroñan* de Banco Santander.\n\n¡Tu evaluación fue un éxito! Solo presentando tu DNI, has calificado para nuestra campaña VIP: una *Tarjeta de Crédito Santander* y un *Préstamo en Efectivo* inmediato. 🏆\n\nEs la oportunidad perfecta para ordenar tus finanzas y disfrutar de promociones únicas.\n\n¿Te gustaría validar tus montos aprobados en línea?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Solicítalo aquí: https://wa.link/xe4ext o 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CuerpoTexto", "Tipo" },
                values: new object[] { "🎉 Buen día, {Nombre}. Qué gusto saludarte, soy *Betty Farroñan*.\n\nHoy tienes acceso a una oferta doble e irrepetible: *Préstamo Personal* con tasa preferencial + *Tarjeta de Crédito*, ¡ambos aprobados con una única evaluación de tu DNI! 💼💳\n\nCero trámites engorrosos, todo es digital y seguro.\n\n¿Prefieres que te envíe la información por este medio o agendamos una llamada corta?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Contáctame aquí: https://wa.link/xe4ext o 995799743", "prestamo" });

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 5,
                column: "CuerpoTexto",
                value: "🥇 ¡Hola, {Nombre}! Te saluda *Betty Farroñan*, tu ejecutiva Santander.\n\nTengo grandes noticias para ti: gracias a nuestra evaluación ágil (solo con tu DNI), has desbloqueado dos productos exclusivos:\n✅ *Tarjeta de Crédito* (con exoneración de membresía sujeta a uso).\n✅ *Préstamo Personal* (liquidez inmediata a tu cuenta).\n\nEstán listos para ti. ¿Coordinamos la entrega de tu tarjeta a domicilio sin costo?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Escríbeme aquí: https://wa.link/xe4ext o 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 6,
                column: "CuerpoTexto",
                value: "🌟 {Nombre}, ¡tienes una oportunidad exclusiva esperándote! Soy *Betty Farroñan*.\n\nTu perfil ha sido seleccionado por Banco Santander. Con una única validación de tu DNI, te otorgamos una *Tarjeta de Crédito* para tus compras diarias y un *Préstamo Efectivo* para concretar tus proyectos. 🚀💳\n\n¡Sin trámites extras!\n\n¿Te interesa que revisemos el simulador de cuotas y los beneficios de tu tarjeta?\n\n👩‍💼 *Betty Farroñan*\n👉 Confirma aquí: https://wa.link/xe4ext o 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CuerpoTexto", "Tipo" },
                values: new object[] { "💳 ¡Hola, {Nombre}! Espero que estés genial. Te escribe *Betty Farroñan*.\n\nQueremos facilitarte la vida. Por eso, con solo verificar tu DNI, Santander te ha aprobado un *Préstamo Personal* y una nueva *Tarjeta de Crédito*. 💰✨\n\nAmbos productos te brindarán el respaldo financiero que mereces.\n\n¿A qué hora te viene bien que te explique cómo activar tus beneficios hoy mismo?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Escríbeme aquí: https://wa.link/xe4ext o 995799743", "tarjeta" });

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CuerpoTexto", "Tipo" },
                values: new object[] { "💎 Buen día, {Nombre}. Es un placer saludarte. Soy *Betty Farroñan*, de Santander.\n\nNos complace invitarte a nuestra campaña Premium. Con una sola evaluación de tu DNI, tienes acceso inmediato a una *Tarjeta de Crédito* con bonos de bienvenida y a un *Préstamo Personal* con desembolso ágil. 🌟\n\nDisfruta de la tranquilidad de un respaldo total.\n\n¿Te gustaría que validemos tus datos de forma segura para gestionar la entrega?\n\n👩‍💼 Ejecutiva: *Betty Farroñan*\n👉 Escríbeme aquí: https://wa.link/xe4ext  o 995799743", "tarjeta" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEnvios_EsNumeroNoRegistrado",
                table: "DetallesEnvios",
                column: "EsNumeroNoRegistrado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DetallesEnvios_EsNumeroNoRegistrado",
                table: "DetallesEnvios");

            migrationBuilder.DropColumn(
                name: "Documento",
                table: "DetallesEnvios");

            migrationBuilder.DropColumn(
                name: "EsNumeroNoRegistrado",
                table: "DetallesEnvios");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 1,
                column: "CuerpoTexto",
                value: "💰 ¡Crédito aprobado a sola firma!\nHola {Nombre}, solo con tu DNI puedes acceder a tu préstamo inmediato.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 2,
                column: "CuerpoTexto",
                value: "💰 Banco Santander tiene un préstamo pre-aprobado para ti, {Nombre}.\nAccede rápido, sin papeleos y con tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 3,
                column: "CuerpoTexto",
                value: "💰 ¡Tu oportunidad está aquí, {Nombre}!\nPréstamo personal disponible con aprobación inmediata. Solo necesitas tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CuerpoTexto", "Tipo" },
                values: new object[] { "💳 ¡Ya tienes tu tarjeta Santander aprobada, {Nombre}!\nDisfruta beneficios exclusivos y compras sin intereses.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", "tarjeta" });

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 5,
                column: "CuerpoTexto",
                value: "💳 Banco Santander te ofrece tarjeta de crédito con aprobación inmediata, {Nombre}.\nEmpieza a disfrutar descuentos y facilidades hoy.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 6,
                column: "CuerpoTexto",
                value: "💳 ¡Activa y disfruta tu tarjeta Santander VISA, {Nombre}!\nAprovecha promociones y meses sin intereses.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743");

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CuerpoTexto", "Tipo" },
                values: new object[] { "👋 Hola {Nombre}, tienes beneficios disponibles en Banco Santander.\nPuedes acceder a préstamo o tarjeta con tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", "bienvenida" });

            migrationBuilder.UpdateData(
                table: "PlantillasMensajes",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CuerpoTexto", "Tipo" },
                values: new object[] { "👋 Banco Santander te da la bienvenida, {Nombre}.\nTienes opciones de crédito disponibles listas para ti.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", "bienvenida" });
        }
    }
}
