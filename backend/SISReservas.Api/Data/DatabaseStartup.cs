using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SISReservas.Api.Data;

public static class DatabaseStartup
{
    public static async Task MigrateAsync(SISReservasDbContext db, ILogger logger, CancellationToken ct)
    {
        var target = new SqlConnectionStringBuilder(db.Database.GetConnectionString())
        {
            Pooling = false,
            ConnectTimeout = 5
        };
        if (string.IsNullOrWhiteSpace(target.InitialCatalog))
            throw new InvalidOperationException("La conexión debe indicar la base de datos de la aplicación.");
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };

        // El healthcheck de master no garantiza que un catálogo existente haya terminado de recuperarse.
        // Las conexiones de sondeo no usan pool: un rechazo temporal no debe quedar almacenado.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var server = new SqlConnection(master.ConnectionString);
                await server.OpenAsync(ct);
                await using var command = server.CreateCommand();
                command.CommandText = "SELECT state FROM sys.databases WHERE name = @database";
                command.Parameters.AddWithValue("@database", target.InitialCatalog);
                var state = await command.ExecuteScalarAsync(ct);
                if (state is not null)
                {
                    if (Convert.ToInt32(state) != 0)
                        throw new InvalidOperationException("El catálogo configurado todavía no está ONLINE.");
                    await using var connection = new SqlConnection(target.ConnectionString);
                    await connection.OpenAsync(ct);
                }
                // Solo EF crea catálogos nuevos y aplica el historial; nunca EnsureCreated.
                await db.Database.MigrateAsync(ct);
                return;
            }
            catch (Exception error) when (attempt < 30 &&
                (error is SqlException || error is InvalidOperationException { Message: "El catálogo configurado todavía no está ONLINE." }))
            {
                // También cubre la carrera 1801 si otro proceso crea el catálogo.
                SqlConnection.ClearPool((SqlConnection)db.Database.GetDbConnection());
                logger.LogWarning("Base no disponible para migraciones; intento {Attempt}/30. Código SQL: {Code}.",
                    attempt, (error as SqlException)?.Number);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }
}
