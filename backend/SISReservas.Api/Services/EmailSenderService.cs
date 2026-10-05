using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public class EmailSenderService
{
    private readonly SISReservasDbContext _db;

    public EmailSenderService(SISReservasDbContext db)
    {
        _db = db;
    }

    public async Task SendPendingAsync()
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST");
        var portText = Environment.GetEnvironmentVariable("SMTP_PORT");
        var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
        var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
        var from = Environment.GetEnvironmentVariable("SMTP_FROM");
        var sslText = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL");

        if (string.IsNullOrWhiteSpace(host) ||
            !int.TryParse(portText, out var port) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(from) ||
            !bool.TryParse(sslText, out var enableSsl))
        {
            throw new InvalidOperationException(
                "Configura SMTP_HOST, SMTP_PORT, SMTP_USERNAME, " +
                "SMTP_PASSWORD, SMTP_FROM y SMTP_ENABLE_SSL " +
                "como variables de entorno.");
        }

        var pendientes = await _db.CorreosEnCola
            .Where(c => c.Estado == EstadoCorreo.Pendiente)
            .OrderBy(c => c.CreadoEn)
            .ToListAsync();

        if (pendientes.Count == 0)
        {
            Console.WriteLine("No hay correos pendientes.");
            return;
        }

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        foreach (var correo in pendientes)
        {
            try
            {
                using var mensaje = new MailMessage(
                    from,
                    correo.Destinatario,
                    correo.Asunto,
                    correo.Cuerpo);

                await smtp.SendMailAsync(mensaje);

                correo.Estado = EstadoCorreo.Enviado;
                correo.EnviadoEn = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                Console.WriteLine(
                    $"Correo enviado: {correo.Id} -> {correo.Destinatario}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"No se pudo enviar el correo {correo.Id}. " +
                    $"Permanece pendiente. Error: {ex.Message}");
            }
        }
    }
}