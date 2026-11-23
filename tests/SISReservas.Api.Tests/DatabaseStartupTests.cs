using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SISReservas.Api.Data;

namespace SISReservas.Api.Tests;

public class DatabaseStartupTests
{
    [Theory]
    [InlineData(1801)]
    [InlineData(4060)]
    [InlineData(1205)]
    [InlineData(53)]
    [InlineData(-2)]
    public void ErrorTransitorio_AdmiteReintento(int number)
    {
        // Arrange / Act
        var retry = DatabaseStartup.IsTransient(number);
        // Assert
        Assert.True(retry);
    }

    [Theory]
    [InlineData(18456)]
    [InlineData(229)]
    [InlineData(262)]
    [InlineData(916)]
    [InlineData(2714)]
    [InlineData(3906)]
    public void CredencialesPermisosOEsquema_NoSeReintentan(int number)
    {
        // Arrange / Act
        var retry = DatabaseStartup.IsTransient(number);
        // Assert
        Assert.False(retry);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(301)]
    public async Task LimiteInvalido_FallaAntesDeAbrirConexion(int seconds)
    {
        // Arrange
        using var db = new SISReservasDbContext(new DbContextOptionsBuilder<SISReservasDbContext>()
            .UseSqlServer("Server=localhost;Database=Prueba;").Options);
        // Act / Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => DatabaseStartup.MigrateAsync(
            db, NullLogger.Instance, CancellationToken.None, seconds));
    }

    [Fact]
    public async Task CatalogoOmitido_FallaAntesDeAbrirConexion()
    {
        // Arrange
        using var db = new SISReservasDbContext(new DbContextOptionsBuilder<SISReservasDbContext>()
            .UseSqlServer("Server=localhost;").Options);
        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseStartup.MigrateAsync(
            db, NullLogger.Instance, CancellationToken.None));
    }

    [Fact]
    public async Task CancelacionDelHost_SeConservaSinConvertirlaEnErrorSql()
    {
        // Arrange
        using var db = new SISReservasDbContext(new DbContextOptionsBuilder<SISReservasDbContext>()
            .UseSqlServer("Server=localhost;Database=Prueba;").Options);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DatabaseStartup.MigrateAsync(
            db, NullLogger.Instance, cancelled.Token));
    }
}
