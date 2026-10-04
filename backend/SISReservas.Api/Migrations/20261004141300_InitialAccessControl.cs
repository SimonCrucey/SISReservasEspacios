using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISReservas.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    BloqueadoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorreosEnCola",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Destinatario = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnviadoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosEnCola", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorreosEnCola_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokensAcceso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExpiraEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usado = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensAcceso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensAcceso_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosEnCola_UsuarioId",
                table: "CorreosEnCola",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensAcceso_TokenHash",
                table: "TokensAcceso",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensAcceso_UsuarioId",
                table: "TokensAcceso",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorreosEnCola");

            migrationBuilder.DropTable(
                name: "TokensAcceso");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
