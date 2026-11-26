using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
namespace SISReservas.Api.Data;

// El esquema histórico es compartido; CoreOnly no es una migración para borrar Reservas.
public static class CoreSchemaMigration
{
    public static async Task MigrateAsync(SISReservasDbContext db, ILogger logger,
        CancellationToken ct, int timeoutSeconds = 120)
    {
#if CORE_ONLY
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot
            ?? throw new InvalidOperationException("Falta el snapshot histórico.");
        var historical = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot.Model, designTime: true);
        var current = db.GetService<IDesignTimeModel>().Model;
        var differences = db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(historical.GetRelationalModel(), current.GetRelationalModel());
        // Solo se permite que la vista de Core omita estas dos tablas. Nunca se aplican esos drops.
        ValidateCoreView(differences);
        var options = new DbContextOptionsBuilder<SISReservasDbContext>()
            .UseSqlServer(db.Database.GetConnectionString()).UseModel(historical).Options;
        await using var schema = new SISReservasDbContext(options);
        await DatabaseStartup.MigrateAsync(schema, logger, ct, timeoutSeconds);
#else
        await DatabaseStartup.MigrateAsync(db, logger, ct, timeoutSeconds);
#endif
    }
    public static void ValidateCoreView(IReadOnlyList<MigrationOperation> differences)
    {
        var removed = differences.OfType<DropTableOperation>().Select(x => x.Name).Order().ToArray();
        if (differences.Any(x => x is not DropTableOperation) || !removed.SequenceEqual(new[]{"Espacios", "Reservas"}))
            throw new InvalidOperationException("El modelo Core difiere del esquema histórico fuera del módulo permitido.");
    }

}
