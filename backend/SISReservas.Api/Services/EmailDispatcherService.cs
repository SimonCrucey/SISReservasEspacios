namespace SISReservas.Api.Services;

// El servicio hospedado es singleton; cada ciclo obtiene su propio DbContext scoped.
public sealed class EmailDispatcherService(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger<EmailDispatcherService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>("EmailDispatcher:Enabled"))
        {
            logger.LogInformation("Despacho de correo deshabilitado. Los mensajes permanecen en cola.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<EmailSenderService>().SendPendingAsync();
            }
            catch (Exception ex)
            {
                // Un fallo de SMTP o base de datos no debe detener la API.
                logger.LogError(ex, "Falló el despacho de correo; se reintentará en el próximo ciclo.");
            }

            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
