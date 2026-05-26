using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WahaSender.Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuraciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LimiteDiarioActual = table.Column<int>(type: "INTEGER", nullable: false),
                    FactorIncremento = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayMinSegundos = table.Column<int>(type: "INTEGER", nullable: false),
                    DelayMaxSegundos = table.Column<int>(type: "INTEGER", nullable: false),
                    WahaApiKey = table.Column<string>(type: "TEXT", nullable: true),
                    WahaEndpointUrl = table.Column<string>(type: "TEXT", nullable: false),
                    WahaSession = table.Column<string>(type: "TEXT", nullable: false),
                    ModoEnvioActivo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuraciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LotesEnvios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NombreArchivo = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CodigoPais = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    FechaImportacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalRegistros = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesEnvios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantillasMensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Indice = table.Column<int>(type: "INTEGER", nullable: false),
                    CuerpoTexto = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasMensajes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetallesEnvios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroCelular = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NombreCliente = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MensajeAsignado = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WahaAckCode = table.Column<int>(type: "INTEGER", nullable: true),
                    MensajeError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesEnvios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesEnvios_LotesEnvios_LoteId",
                        column: x => x.LoteId,
                        principalTable: "LotesEnvios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Configuraciones",
                columns: new[] { "Id", "DelayMaxSegundos", "DelayMinSegundos", "FactorIncremento", "LimiteDiarioActual", "ModoEnvioActivo", "WahaApiKey", "WahaEndpointUrl", "WahaSession" },
                values: new object[] { 1, 45, 15, 5, 50, false, "119ce04a85dd41818809be61aba87066", "http://localhost:3000/api/sendText", "default" });

            migrationBuilder.InsertData(
                table: "PlantillasMensajes",
                columns: new[] { "Id", "Activo", "CuerpoTexto", "Indice", "Tipo" },
                values: new object[,]
                {
                    { 1, true, "💰 ¡Crédito aprobado a sola firma!\nHola {Nombre}, solo con tu DNI puedes acceder a tu préstamo inmediato.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 1, "prestamo" },
                    { 2, true, "💰 Banco Santander tiene un préstamo pre-aprobado para ti, {Nombre}.\nAccede rápido, sin papeleos y con tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 2, "prestamo" },
                    { 3, true, "💰 ¡Tu oportunidad está aquí, {Nombre}!\nPréstamo personal disponible con aprobación inmediata. Solo necesitas tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 3, "prestamo" },
                    { 4, true, "💳 ¡Ya tienes tu tarjeta Santander aprobada, {Nombre}!\nDisfruta beneficios exclusivos y compras sin intereses.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 4, "tarjeta" },
                    { 5, true, "💳 Banco Santander te ofrece tarjeta de crédito con aprobación inmediata, {Nombre}.\nEmpieza a disfrutar descuentos y facilidades hoy.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 5, "tarjeta" },
                    { 6, true, "💳 ¡Activa y disfruta tu tarjeta Santander VISA, {Nombre}!\nAprovecha promociones y meses sin intereses.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 6, "tarjeta" },
                    { 7, true, "👋 Hola {Nombre}, tienes beneficios disponibles en Banco Santander.\nPuedes acceder a préstamo o tarjeta con tu DNI.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 7, "bienvenida" },
                    { 8, true, "👋 Banco Santander te da la bienvenida, {Nombre}.\nTienes opciones de crédito disponibles listas para ti.\n\n👩‍💼 Ejecutiva: Betty Farroñan\n📲 995799743", 8, "bienvenida" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEnvios_Estado",
                table: "DetallesEnvios",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEnvios_Estado_FechaProcesado",
                table: "DetallesEnvios",
                columns: new[] { "Estado", "FechaProcesado" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesEnvios_LoteId",
                table: "DetallesEnvios",
                column: "LoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuraciones");

            migrationBuilder.DropTable(
                name: "DetallesEnvios");

            migrationBuilder.DropTable(
                name: "PlantillasMensajes");

            migrationBuilder.DropTable(
                name: "LotesEnvios");
        }
    }
}
