using System.Data.Common;
using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SISReservas.Api.Controllers;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class ErroresControladosTests
{
    [Theory]
    [InlineData(true, 503)]
    [InlineData(false, 500)]
    public async Task H02_Reportes_ErrorInterno_NoExponeTrazaRutaConsultaNiCabecera(bool baseDeDatos, int esperado)
    {
        // Arrange: controlador y servicio reales; la consulta falla sin levantar la API.
        using var datos = new TestDatabase();
        var usuario = await TestData.AgregarUsuarioAsync(datos.CreateContext());
        var controlador = new ReportesController(new ReporteService(datos.CreateContext(new ConsultaFallida())));
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();
        contexto.Request.Headers.Authorization = "Bearer sesion-sintetica-H02";
        contexto.Request.Headers["X-Dato-Sensible"] = "dato-sintetico-H02";
        contexto.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())], "Session"));
        controlador.ControllerContext = new ControllerContext { HttpContext = contexto };
        RequestDelegate endpoint = async _ =>
        {
            if (!baseDeDatos) throw new InvalidOperationException("Detalle interno ficticio /src/privado SELECT dato");
            await controlador.Reservas(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        };
        using var servicios = new ServiceCollection().AddLogging().BuildServiceProvider();
        contexto.RequestServices = servicios;
        // Resolución por nombre permite ejecutar exactamente esta prueba en el padre sin la corrección.
        var tipo = typeof(ReportesController).Assembly.GetType("SISReservas.Api.Infrastructure.ControlledErrorMiddleware");

        // Act
        if (tipo is null) await endpoint(contexto);
        else
        {
            var middleware = ActivatorUtilities.CreateInstance(servicios, tipo, endpoint);
            await (Task)tipo.GetMethod("InvokeAsync")!.Invoke(middleware, [contexto])!;
        }
        contexto.Response.Body.Position = 0;
        var cuerpo = await new StreamReader(contexto.Response.Body).ReadToEndAsync();

        // Assert
        Assert.Equal(esperado, contexto.Response.StatusCode);
        Assert.Contains("message", cuerpo);
        Assert.DoesNotContain("/src/", cuerpo);
        Assert.DoesNotContain("SELECT", cuerpo);
        Assert.DoesNotContain("Exception", cuerpo);
        Assert.DoesNotContain("Authorization", cuerpo);
        Assert.DoesNotContain("sesion-sintetica-H02", cuerpo);
        Assert.DoesNotContain("dato-sintetico-H02", cuerpo);
    }

    private sealed class ConsultaFallida : IQueryExpressionInterceptor
    {
        public Expression QueryCompilationStarting(Expression expression, QueryExpressionEventData eventData)
            => throw new FalloSqlFicticio();
    }
    private sealed class FalloSqlFicticio : DbException
    {
        public FalloSqlFicticio() : base("SQL ficticio /src/privado SELECT dato") { }
    }
}
