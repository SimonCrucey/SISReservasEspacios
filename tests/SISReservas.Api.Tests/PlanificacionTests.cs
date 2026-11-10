using System.Net;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class PlanificacionTests
{
    [Theory]
    [InlineData("ok", true, true)]
    [InlineData("lluvia", true, false)]
    [InlineData("sin-clave", false, false)]
    [InlineData("401", false, false)]
    [InlineData("503", false, false)]
    [InlineData("timeout", false, false)]
    [InlineData("json", false, false)]
    public async Task Planificacion_CombinaDisponibilidadConProveedor(string escenario, bool exito, bool recomendable)
    {
        // Arrange
        using var database = new TestDatabase();
        var db = database.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(db);
        var espacio = new Espacio { Nombre = "Cancha", Tipo = TipoEspacio.Cancha, Activo = true };
        db.Espacios.Add(espacio);
        await db.SaveChangesAsync();
        var inicio = DateTime.UtcNow.Date.AddDays(1);
        var epoch = new DateTimeOffset(inicio).ToUnixTimeSeconds();
        using var http = new HttpClient(new Proveedor(escenario, epoch)) { BaseAddress = new Uri("https://example.invalid/") };
        var reservas = new ReservaService(db, new PermisoService(db), TimeProvider.System);
        var servicio = new PlanificacionService(http, new(escenario == "sin-clave" ? "" : "ficticia", "18,-69"), reservas, db);
        // Act
        var resultado = await servicio.EvaluarAsync(usuario.Id, espacio.Id, inicio, inicio.AddHours(1), default);
        // Assert
        Assert.Equal(exito, resultado.Exito);
        if (exito) Assert.Equal(recomendable, resultado.Valor!.Recomendado);
        Assert.Empty(db.Reservas);
    }

    private sealed class Proveedor(string escenario, long epoch) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (escenario == "timeout") throw new TaskCanceledException();
            var lluvia = escenario == "lluvia" ? 90 : 10;
            return Task.FromResult(new HttpResponseMessage(escenario switch { "401" => HttpStatusCode.Unauthorized, "503" => HttpStatusCode.ServiceUnavailable, _ => HttpStatusCode.OK })
            {
                Content = new StringContent(escenario == "json" ? "{}" :
                    System.Text.Json.JsonSerializer.Serialize(new { forecast = new { forecastday = new[] {
                        new { hour = new[] { new { time_epoch = epoch, chance_of_rain = lluvia, wind_kph = 10 } } }
                    } } }))
            });
        }
    }
}
