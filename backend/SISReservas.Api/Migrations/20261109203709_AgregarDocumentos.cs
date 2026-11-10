using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISReservas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDocumentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NombreInterno = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Mime = table.Column<string>(type: "nvarchar(127)", maxLength: 127, nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    SubidoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EliminadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_NombreInterno",
                table: "Documentos",
                column: "NombreInterno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_UsuarioId_EliminadoEn",
                table: "Documentos",
                columns: new[] { "UsuarioId", "EliminadoEn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documentos");
        }
    }
}
