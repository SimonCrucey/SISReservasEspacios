using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SISReservas.Api.Models;
using SISReservas.Api.Services;

namespace SISReservas.Api.Tests;

public class LecturaConcurrenteTests
{
    [Fact]
    public async Task H01_DosLecturasConcurrentes_ConservanUnaSolaFechaPersistida()
    {
        // Arrange: detener el primer guardado mientras entra una segunda lectura real.
        using var datos = new TestDatabase();
        var seed = datos.CreateContext();
        var usuario = await TestData.AgregarUsuarioAsync(seed);
        var notificacion = new Notificacion { UsuarioId = usuario.Id, Asunto = "Prueba H-01", Mensaje = "Ficticio" };
        seed.Add(notificacion);
        await seed.SaveChangesAsync();
        var puerta = new GuardadoCoordinado();
        var primero = new NotificacionService(datos.CreateContext(puerta));
        var segundo = new NotificacionService(datos.CreateContext(puerta));

        // Act
        var lectura1 = primero.LeerAsync(usuario.Id, notificacion.Id);
        await puerta.Primero.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var lectura2 = segundo.LeerAsync(usuario.Id, notificacion.Id);
        try { await Task.WhenAny(lectura2, Task.Delay(100)); }
        finally { puerta.Continuar.TrySetResult(); }
        var resultados = await Task.WhenAll(lectura1, lectura2);

        // Assert: una sola escritura; la otra operación reconoce que ya estaba leída.
        Assert.All(resultados, Assert.True);
        Assert.Equal(1, puerta.Guardados);
        Assert.NotNull((await datos.CreateContext().Notificaciones.SingleAsync()).LeidaEn);
    }

    private sealed class GuardadoCoordinado : SaveChangesInterceptor
    {
        private int guardados;
        public int Guardados => guardados;
        public TaskCompletionSource Primero { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref guardados) == 1)
            {
                Primero.TrySetResult();
                await Continuar.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }
}
