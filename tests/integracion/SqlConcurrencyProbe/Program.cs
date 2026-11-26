using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Services;

// Solo se admiten catálogos nuevos identificados como auditoría dentro de la red Docker.
var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "";
var connection = new SqlConnectionStringBuilder(raw);
if (!Regex.IsMatch(connection.InitialCatalog, "^SISAsignacion3_[a-f0-9]{32}$")
    || connection.DataSource != "sqlserver,1433")
    throw new InvalidOperationException("Destino no autorizado para la prueba SQL aislada.");
var master = new SqlConnectionStringBuilder(raw) { InitialCatalog="master" };
await using (var server = new SqlConnection(master.ConnectionString))
{
    await server.OpenAsync();
    await using var check = server.CreateCommand();
    check.CommandText="SELECT DB_ID(@catalog)";
    check.Parameters.AddWithValue("@catalog",connection.InitialCatalog);
    if (await check.ExecuteScalarAsync() is not DBNull)
        throw new InvalidOperationException("La prueba exige un catálogo nuevo; no reutiliza datos.");
}
DbContextOptions<SISReservasDbContext> Options(UpdateGate? gate=null)
{
    var options = new DbContextOptionsBuilder<SISReservasDbContext>().UseSqlServer(raw);
    if (gate is not null) options.AddInterceptors(gate);
    return options.Options;
}
await using var seed = new SISReservasDbContext(Options());
await DatabaseStartup.MigrateAsync(seed,NullLogger.Instance,CancellationToken.None);
var user = new Usuario {Nombre="Auditoría H-01",Email=$"{Guid.NewGuid():N}@test.invalid",Activo=true,PasswordHash="fixture-sin-login"};
var notification = new Notificacion {UsuarioId=user.Id,Asunto="H-01 SQL",Mensaje="Datos sintéticos"};
seed.AddRange(user,notification);await seed.SaveChangesAsync();
var gate1=new UpdateGate();var gate2=new UpdateGate();
await using var context1=new SISReservasDbContext(Options(gate1));
await using var context2=new SISReservasDbContext(Options(gate2));
Task<bool>? operation1=null,operation2=null;
try
{
    // Arrange/Act: detener ambas escrituras antes de SQL, sin retrasos arbitrarios.
    operation1=new NotificacionService(context1).LeerAsync(user.Id,notification.Id);
    await gate1.Arrived.Task.WaitAsync(TimeSpan.FromSeconds(30));
    operation2=new NotificacionService(context2).LeerAsync(user.Id,notification.Id);
    await gate2.Arrived.Task.WaitAsync(TimeSpan.FromSeconds(30));
    gate1.Continue.TrySetResult();
    var firstResult=await operation1;
    var firstDate=await seed.Notificaciones.AsNoTracking().Where(n=>n.Id==notification.Id).Select(n=>n.LeidaEn).SingleAsync();
    gate2.Continue.TrySetResult();
    var secondResult=await operation2;
    var finalDate=await seed.Notificaciones.AsNoTracking().Where(n=>n.Id==notification.Id).Select(n=>n.LeidaEn).SingleAsync();
    // Assert: la fecha leída de SQL después del primer commit debe permanecer idéntica.
    var passed=firstResult&&secondResult&&firstDate.HasValue&&firstDate==finalDate;
    Console.WriteLine("A3_SQL_RESULT "+JsonSerializer.Serialize(new {
        Passed=passed,BothSucceeded=firstResult&&secondResult,FirstPersisted=firstDate,
        FinalPersisted=finalDate,FirstTicks=firstDate?.Ticks,FinalTicks=finalDate?.Ticks,
        Migrations=(await seed.Database.GetAppliedMigrationsAsync()).Count()
    }));
    return passed ? 0 : 1;
}
finally
{
    gate1.Continue.TrySetResult();gate2.Continue.TrySetResult();
    if(operation1 is not null) await operation1;
    if(operation2 is not null) await operation2;
}

sealed class UpdateGate : DbCommandInterceptor
{
    public TaskCompletionSource Arrived {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private async Task Before(DbCommand command,CancellationToken ct)
    {
        if (!command.CommandText.Contains("UPDATE",StringComparison.OrdinalIgnoreCase)
            || !command.CommandText.Contains("Notificaciones",StringComparison.Ordinal)) return;
        Arrived.TrySetResult();await Continue.Task.WaitAsync(ct);
    }
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand c,CommandEventData e,InterceptionResult<int> r,CancellationToken ct=default)
    {await Before(c,ct);return r;}
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand c,CommandEventData e,InterceptionResult<DbDataReader> r,CancellationToken ct=default)
    {await Before(c,ct);return r;}
}
