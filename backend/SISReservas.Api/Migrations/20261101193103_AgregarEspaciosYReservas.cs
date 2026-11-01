using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISReservas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEspaciosYReservas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Espacios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Espacios", x => x.Id);
                    table.CheckConstraint("CK_Espacios_Nombre", "LEN(LTRIM(RTRIM([Nombre]))) > 0");
                    table.CheckConstraint("CK_Espacios_Tipo", "[Tipo] IN ('Salon', 'Cancha', 'Equipo')");
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EspacioId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.CheckConstraint("CK_Reservas_Estado", "[Estado] IN ('Pendiente', 'Confirmada', 'Cancelada', 'Completada')");
                    table.CheckConstraint("CK_Reservas_Fechas", "[FechaFin] > [FechaInicio]");
                    table.ForeignKey(
                        name: "FK_Reservas_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_EspacioId_Estado_FechaInicio_FechaFin",
                table: "Reservas",
                columns: new[] { "EspacioId", "Estado", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_Estado",
                table: "Reservas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_FechaFin",
                table: "Reservas",
                column: "FechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_FechaInicio",
                table: "Reservas",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Espacios");
        }
    }
}
