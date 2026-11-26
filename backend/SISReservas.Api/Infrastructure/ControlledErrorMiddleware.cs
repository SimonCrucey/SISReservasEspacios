using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace SISReservas.Api.Infrastructure;

// Frontera común: tampoco en Development una respuesta pública contiene la excepción.
public sealed class ControlledErrorMiddleware(RequestDelegate next, ILogger<ControlledErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // El cliente ya cerró la petición: no intentar escribir otra respuesta.
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            var indisponible = error is DbException or DbUpdateException;
            logger.LogError("Petición fallida ({TipoError}); identificador {Identificador}.",
                error.GetType().Name, context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = indisponible
                ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                message = indisponible
                    ? "Servicio temporalmente no disponible. Inténtalo de nuevo."
                    : "Se produjo un error inesperado."
            }, context.RequestAborted);
        }
    }
}
