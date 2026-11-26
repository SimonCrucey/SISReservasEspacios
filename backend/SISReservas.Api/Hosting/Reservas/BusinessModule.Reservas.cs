using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Services;
namespace SISReservas.Api.Hosting;

internal static partial class BusinessModule
{
    static partial void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ReservaService>();
        services.AddSingleton(new ClimaOptions(Environment.GetEnvironmentVariable("WEATHER_API_KEY") ?? "",
            Environment.GetEnvironmentVariable("WEATHER_LOCATION") ?? "18.4861,-69.9312"));
        services.AddHttpClient<PlanificacionService>(client =>
        {
            client.BaseAddress = new Uri("https://api.weatherapi.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).RemoveAllLoggers();
    }
    static partial void CollectInitializers(List<Func<SISReservasDbContext, Task>> initializers)
        => initializers.Add(async db =>
        {
            if (!await db.Espacios.AnyAsync())
                db.Espacios.Add(new Espacio { Nombre="Cancha de evaluación", Tipo=TipoEspacio.Cancha, Activo=true });
        });
}
