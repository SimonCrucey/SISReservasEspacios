using SISReservas.Api.Data;
namespace SISReservas.Api.Hosting;

// Punto de composición opcional; no referencia tipos del módulo de negocio.
internal static partial class BusinessModule
{
    public static void Register(IServiceCollection services) => RegisterServices(services);
    static partial void RegisterServices(IServiceCollection services);
    public static async Task InitializeAsync(SISReservasDbContext db)
    {
        var initializers = new List<Func<SISReservasDbContext, Task>>();
        CollectInitializers(initializers);
        foreach (var initialize in initializers) await initialize(db);
    }
    static partial void CollectInitializers(List<Func<SISReservasDbContext, Task>> initializers);
}
