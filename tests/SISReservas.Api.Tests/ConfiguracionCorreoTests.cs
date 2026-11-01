using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SISReservas.Api.DTOs.Auth;
using SISReservas.Api.Security;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class ConfiguracionCorreoTests
{
    [Theory]
    [InlineData("http://localhost:5054")]
    [InlineData("https://reservas.example")]
    public async Task ActivacionYReenvio_UsanOrigenConfigurado(string origen)
    {
        // Arrange
        using var database = new TestDatabase(); var db = database.CreateContext();
        var tokens = new TokenService(); var sesion = new SessionService(db, tokens);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppBaseUrl"] = origen }).Build();
        var auth = new AuthService(db, new PasswordService(), tokens, new EmailQueueService(db), config, sesion);
        // Act
        await auth.RegisterAsync(new RegisterRequest { Nombre = "Prueba", Email = "correo@test.invalid", Password = "Prueba1234" });
        await auth.ResendActivationAsync("correo@test.invalid");
        // Assert
        Assert.Equal(2, await db.CorreosEnCola.CountAsync());
        Assert.All(await db.CorreosEnCola.ToListAsync(), correo =>
            Assert.Contains(origen + "/api/auth/activate?email=correo%40test.invalid&token=", correo.Cuerpo));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("http://public.example")]
    [InlineData("https://user:password@example.com")]
    [InlineData("https://example.com?redirect=otro")]
    public async Task Activacion_OrigenInvalido_RechazaAntesDePersistir(string? origen)
    {
        // Arrange
        using var database = new TestDatabase(); var db = database.CreateContext();
        var tokens = new TokenService();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppBaseUrl"] = origen }).Build();
        var auth = new AuthService(db, new PasswordService(), tokens, new EmailQueueService(db), config, new SessionService(db, tokens));
        // Act
        var error = await Record.ExceptionAsync(() => auth.RegisterAsync(new RegisterRequest { Nombre = "Prueba", Email = "correo@test.invalid", Password = "Prueba1234" }));
        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Empty(db.Usuarios);
        Assert.Empty(db.CorreosEnCola);
    }

    [Fact]
    public async Task Dispatcher_Deshabilitado_NoResuelveServiciosNiTocaBaseDeDatos()
    {
        // Arrange: no registrar EmailSenderService hace fallar cualquier resolución accidental.
        using var provider = new ServiceCollection().BuildServiceProvider();
        var config = new ConfigurationBuilder().Build();
        using var dispatcher = new EmailDispatcherService(provider.GetRequiredService<IServiceScopeFactory>(), config, NullLogger<EmailDispatcherService>.Instance);
        // Act
        await dispatcher.StartAsync(CancellationToken.None);
        await dispatcher.StopAsync(CancellationToken.None);
        // Assert
        Assert.True(dispatcher.ExecuteTask!.IsCompletedSuccessfully);
    }
}
