using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace SISReservas.Api.Data;

public static class DatabaseStartup
{
    public static async Task MigrateAsync(SISReservasDbContext db, ILogger logger,
        CancellationToken cancellationToken, int timeoutSeconds = 120)
    {
        if (timeoutSeconds is < 1 or > 300)
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "El límite de arranque debe estar entre 1 y 300 segundos.");
        var target = new SqlConnectionStringBuilder(db.Database.GetConnectionString())
        {
            Pooling = false,
            ConnectTimeout = 5
        };
        if (string.IsNullOrWhiteSpace(target.InitialCatalog))
            throw new InvalidOperationException("La conexión debe indicar el catálogo de la aplicación.");
        var master = new SqlConnectionStringBuilder(target.ConnectionString) { InitialCatalog = "master" };
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var attempt = 0;
        try
        {
            while (true)
            {
                limit.Token.ThrowIfCancellationRequested();
                attempt++;
                try
                {
                    // Sesión sin pool: un login temporalmente rechazado no queda cacheado.
                    await using var server = new SqlConnection(master.ConnectionString);
                    await server.OpenAsync(limit.Token);
                    await AcquireLockAsync(server, target.InitialCatalog, limit.Token);
                    if (!await CatalogReadyAsync(server, target, limit.Token))
                        throw new CatalogNotReadyException();

                    // El bloqueo en master cubre también la carrera de CREATE DATABASE.
                    // EF conserva su propio bloqueo de migraciones dentro del catálogo.
                    await db.Database.MigrateAsync(limit.Token);
                    return;
                }
                catch (SqlException error) when (IsTransient(error.Number))
                {
                    SqlConnection.ClearPool((SqlConnection)db.Database.GetDbConnection());
                    logger.LogWarning("Arranque SQL pendiente; intento {Attempt}, código {Code}.", attempt, error.Number);
                }
                catch (CatalogNotReadyException)
                {
                    logger.LogWarning("Arranque SQL pendiente; intento {Attempt}: catálogo recuperándose o bloqueo ocupado.", attempt);
                }
                catch (SqlException error)
                {
                    limit.Token.ThrowIfCancellationRequested();
                    throw new InvalidOperationException(
                        $"Error SQL permanente de arranque ({error.Number}). Comprueba credenciales, permisos y esquema; no se reintenta.", error);
                }
                await Task.Delay(TimeSpan.FromSeconds(2), limit.Token);
            }
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"SQL no completó el arranque en {timeoutSeconds} segundos. Comprueba recuperación y conectividad.", error);
        }
    }

    // Whitelist: errores no incluidos (credenciales, permisos o esquema) no se ocultan.
    internal static bool IsTransient(int number) => number is
        -2 or 20 or 53 or 64 or 121 or 233 or 258 or 1205 or 1222 or 1801 or 4060
        or 10053 or 10054 or 10060 or 40197 or 40501 or 40613;

    private static async Task AcquireLockAsync(SqlConnection server, string catalog, CancellationToken ct)
    {
        await using var command = server.CreateCommand();
        command.CommandTimeout = 6;
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=@resource,
                @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=5000;
            SELECT @result;
            """;
        command.Parameters.Add("@resource", SqlDbType.NVarChar, 255).Value = "SISReservas:Migrate:" + catalog.ToUpperInvariant();
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        if (result is -1 or -3) throw new CatalogNotReadyException();
        if (result == -2) { ct.ThrowIfCancellationRequested(); throw new CatalogNotReadyException(); }
        if (result < 0) throw new InvalidOperationException("No se pudo adquirir el bloqueo SQL de migraciones. Comprueba permisos.");
        // Se libera al cerrar esta conexión física (Pooling=false), incluso si ocurre una excepción.
    }

    private static async Task<bool> CatalogReadyAsync(SqlConnection server, SqlConnectionStringBuilder target, CancellationToken ct)
    {
        await using var command = server.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText = "SELECT state, is_read_only, HAS_DBACCESS(name) FROM sys.databases WHERE name=@catalog";
        command.Parameters.Add("@catalog", SqlDbType.NVarChar, 128).Value = target.InitialCatalog;
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return true; // EF crea únicamente catálogos inexistentes.
        var state = reader.GetByte(0);
        if (state is 1 or 2) return false; // RESTORING / RECOVERING.
        if (state != 0 || reader.GetBoolean(1))
            throw new InvalidOperationException($"El catálogo no admite migraciones: estado SQL {state} o solo lectura. Requiere intervención del operador.");
        var hasAccess = !reader.IsDBNull(2) && reader.GetInt32(2) == 1;
        if (!hasAccess) throw new InvalidOperationException("El login no tiene acceso al catálogo. Comprueba permisos y disponibilidad administrativa.");
        await reader.CloseAsync();
        // ONLINE en sys.databases no basta: el catálogo debe aceptar el login.
        // No reconstruir desde server.ConnectionString: después de Open oculta Password.
        await using var connection = new SqlConnection(target.ConnectionString);
        try { await connection.OpenAsync(ct); }
        catch (SqlException error) when (error.Number == 18456)
        {
            // El mismo login ya autenticó en master y HAS_DBACCESS=1. Durante la
            // recuperación aún puede fallar el acceso al catálogo (estado 38).
            throw new CatalogNotReadyException();
        }
        return true;
    }

    private sealed class CatalogNotReadyException : Exception;
}
