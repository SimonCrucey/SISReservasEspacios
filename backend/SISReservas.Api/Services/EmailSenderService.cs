using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public class EmailSenderService(
    SISReservasDbContext db, IConfiguration configuration, ILogger<EmailSenderService> logger)
{
    public async Task SendPendingAsync(CancellationToken cancellationToken = default)
    {
        var host = configuration["SMTP_HOST"];
        var portText = configuration["SMTP_PORT"];
        var username = configuration["SMTP_USERNAME"];
        var password = configuration["SMTP_PASSWORD"];
        var from = configuration["SMTP_FROM"];
        var sslText = configuration["SMTP_ENABLE_SSL"];
        var useAuthText = configuration["SMTP_USE_AUTH"] ?? "true";

        if (string.IsNullOrWhiteSpace(host) ||
            !int.TryParse(portText, out var port) ||
            port < 1 || port > 65535 ||
            !MailAddress.TryCreate(from, out var remitente) ||
            !bool.TryParse(sslText, out var enableSsl) ||
            !bool.TryParse(useAuthText, out var useAuth) ||
            (useAuth && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))))
        {
            throw new InvalidOperationException(
                "Configura SMTP_HOST, SMTP_PORT, SMTP_USERNAME, " +
                "SMTP_PASSWORD (si SMTP_USE_AUTH=true), SMTP_FROM y SMTP_ENABLE_SSL " +
                "como variables de entorno.");
        }

        var pendientes = await db.CorreosEnCola
            .Where(c => c.Estado == EstadoCorreo.Pendiente)
            .OrderBy(c => c.CreadoEn)
            .ToListAsync(cancellationToken);

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            UseDefaultCredentials = false,
            Credentials = useAuth ? new NetworkCredential(username, password) : null,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        foreach (var correo in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                using var mensaje = new MailMessage
                {
                    From = remitente,
                    Subject = correo.Asunto,
                    Body = correo.Cuerpo
                };
                mensaje.To.Add(correo.Destinatario);
                await smtp.SendMailAsync(mensaje, timeout.Token);
            }
            catch (SmtpException error)
            {
                logger.LogWarning("SMTP rechazó el correo {CorreoId} o no respondió. Código {CodigoSmtp}. Sigue pendiente.",
                    correo.Id, error.StatusCode);
                continue;
            }
            catch (FormatException)
            {
                logger.LogWarning("Formato SMTP inválido para el correo {CorreoId}. Sigue pendiente.", correo.Id);
                continue;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Se agotó el tiempo de espera SMTP para el correo {CorreoId}. Sigue pendiente.", correo.Id);
                continue;
            }

            correo.Estado = EstadoCorreo.Enviado;
            correo.EnviadoEn = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            logger.LogInformation("SMTP aceptó el correo {CorreoId}.", correo.Id);
        }
    }
}
