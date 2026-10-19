using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Users;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Security;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class UsersControllerPermissionTests
{
    [Fact]
    public async Task GetUsers_EstandarConConsultarUsuarios_DevuelveSoloResumen()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(
            db,
            nombre: "Estandar");

        await TestData.AgregarUsuarioAsync(
            db,
            nombre: "Otro Usuario");

        await TestData.AgregarPermisoAsync(
            db,
            estandar,
            Permiso.ConsultarUsuarios);

        var controller = CrearController(
            db,
            estandar.Id,
            administrador: false);

        var resultado = await controller.GetUsers();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var usuarios = Assert.IsType<List<UserSummaryDto>>(
            ok.Value);

        Assert.Equal(2, usuarios.Count);
        Assert.All(
            usuarios,
            u => Assert.False(string.IsNullOrWhiteSpace(u.Nombre)));
    }

    [Fact]
    public async Task GetUsers_EstandarSinPermiso_DevuelveForbid()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var estandar = await TestData.AgregarUsuarioAsync(db);

        var controller = CrearController(
            db,
            estandar.Id,
            administrador: false);

        var resultado = await controller.GetUsers();

        Assert.IsType<ForbidResult>(resultado);
    }

    [Fact]
    public async Task GetUsers_Administrador_DevuelveDatosCompletos()
    {
        using var database = new TestDatabase();
        using var db = database.CreateContext();

        var administrador = await TestData.AgregarUsuarioAsync(
            db,
            Rol.Administrador,
            nombre: "Administrador");

        var controller = CrearController(
            db,
            administrador.Id,
            administrador: true);

        var resultado = await controller.GetUsers();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var usuarios = Assert.IsType<List<UserResponseDto>>(
            ok.Value);

        var usuario = Assert.Single(usuarios);

        Assert.Equal(administrador.Id, usuario.Id);
        Assert.Equal(administrador.Email, usuario.Email);
        Assert.Equal(Rol.Administrador, usuario.Rol);
        Assert.True(usuario.Activo);
    }

    private static UsersController CrearController(
        SISReservasDbContext db,
        Guid usuarioId,
        bool administrador)
    {
        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                usuarioId.ToString()),
            new(
                ClaimTypes.Role,
                administrador
                    ? nameof(Rol.Administrador)
                    : nameof(Rol.Estandar))
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var userAdministrationService =
            new UserAdministrationService(
                db,
                new SessionService(db, new TokenService()));

        var permisoService = new PermisoService(db);

        return new UsersController(
            userAdministrationService,
            permisoService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            }
        };
    }
}
