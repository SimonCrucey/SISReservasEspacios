using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISReservas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarColaCorreos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Intentos",
                table: "CorreosEnCola",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReclamoHasta",
                table: "CorreosEnCola",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReclamoId",
                table: "CorreosEnCola",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoError",
                table: "CorreosEnCola",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoIntentoEn",
                table: "CorreosEnCola",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CorreosEnCola_Estado_ReclamoHasta_CreadoEn",
                table: "CorreosEnCola",
                columns: new[] { "Estado", "ReclamoHasta", "CreadoEn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CorreosEnCola_Estado_ReclamoHasta_CreadoEn",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "Intentos",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "ReclamoHasta",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "ReclamoId",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "UltimoError",
                table: "CorreosEnCola");

            migrationBuilder.DropColumn(
                name: "UltimoIntentoEn",
                table: "CorreosEnCola");
        }
    }
}
