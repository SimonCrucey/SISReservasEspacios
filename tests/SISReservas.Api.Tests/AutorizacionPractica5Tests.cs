using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SISReservas.Api.Controllers;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class AutorizacionPractica5Tests
{
    [Theory]
    [InlineData(typeof(NotificacionesController))]
    [InlineData(typeof(ReportesController))]
    [InlineData(typeof(CorreosController))]
    public void Controladores_ExigenSesionYRol(Type controlador)
    {
        // Arrange
        var atributos = controlador.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>();
        // Act
        var regla = Assert.Single(atributos);
        // Assert
        Assert.Equal("Session", regla.AuthenticationSchemes);
        Assert.Contains("Administrador", regla.Roles);
        if (controlador == typeof(CorreosController)) Assert.DoesNotContain("Estandar", regla.Roles);
    }

    [Fact]
    public async Task Cola_RechazaEstandarInclusoInvocandoControladorDirectamente()
    {
        // Arrange
        using var datos = new TestDatabase();
        var db = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var controlador = new CorreosController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controlador.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())], "Session"));
        // Act
        var resultado = await controlador.Consultar();
        // Assert
        Assert.IsType<ForbidResult>(resultado);
    }
}
