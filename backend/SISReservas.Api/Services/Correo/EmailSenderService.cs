using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public class EmailSenderService(
    SISReservasDbContext db, IConfiguration configuration, ILogger<EmailSenderService> logger)
{
    public const int MaximoIntentos = 3;
    public const int TamanoLote = 20;
    // EF InMemory no admite UPDATE condicional. Solo se usa esta exclusión en pruebas.
    private static readonly SemaphoreSlim ProcesamientoNoRelacional = new(1, 1);

    public async Task SendPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
        {
            await ProcesamientoNoRelacional.WaitAsync(cancellationToken);
            try { await ProcesarAsync(cancellationToken); }
            finally { ProcesamientoNoRelacional.Release(); }
        }
        else await ProcesarAsync(cancellationToken);
    }

    private async Task ProcesarAsync(CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        var agotados = db.CorreosEnCola.Where(c => c.Estado == EstadoCorreo.Pendiente
            && c.Intentos >= MaximoIntentos && (c.ReclamoHasta == null || c.ReclamoHasta <= ahora));
        if (db.Database.IsRelational())
            await agotados.ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, EstadoCorreo.Fallido)
                .SetProperty(c => c.UltimoError, "Se agotaron los intentos; el último procesamiento pudo interrumpirse.")
                .SetProperty(c => c.ReclamoId, (Guid?)null).SetProperty(c => c.ReclamoHasta, (DateTime?)null), ct);
        else
        {
            foreach (var correo in await agotados.ToListAsync(ct))
            {
                correo.Estado = EstadoCorreo.Fallido;
                correo.UltimoError = "Se agotaron los intentos; el último procesamiento pudo interrumpirse.";
                correo.ReclamoId = null;
                correo.ReclamoHasta = null;
            }
            await db.SaveChangesAsync(ct);
        }

        var ids = await db.CorreosEnCola.AsNoTracking()
            .Where(c => c.Estado == EstadoCorreo.Pendiente && c.Intentos < MaximoIntentos
                && (c.ReclamoHasta == null || c.ReclamoHasta <= ahora))
            .OrderBy(c => c.CreadoEn).ThenBy(c => c.Id).Select(c => c.Id).Take(TamanoLote).ToListAsync(ct);
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            var reclamo = Guid.NewGuid();
            ahora = DateTime.UtcNow;
            var disponibles = db.CorreosEnCola.Where(c => c.Id == id && c.Estado == EstadoCorreo.Pendiente
                && c.Intentos < MaximoIntentos && (c.ReclamoHasta == null || c.ReclamoHasta <= ahora));
            CorreoEnCola? correo;
            if (db.Database.IsRelational())
            {
                var afectados = await disponibles.ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.ReclamoId, reclamo)
                    .SetProperty(c => c.ReclamoHasta, ahora.AddMinutes(2))
                    .SetProperty(c => c.UltimoIntentoEn, ahora)
                    .SetProperty(c => c.Intentos, c => c.Intentos + 1), ct);
                if (afectados == 0) continue;
                correo = await db.CorreosEnCola.AsNoTracking().SingleAsync(c => c.Id == id, ct);
            }
            else
            {
                correo = await disponibles.SingleOrDefaultAsync(ct);
                if (correo is null) continue;
                correo.ReclamoId = reclamo;
                correo.ReclamoHasta = ahora.AddMinutes(2);
                correo.UltimoIntentoEn = ahora;
                correo.Intentos++;
                await db.SaveChangesAsync(ct);
            }

            string? error = null;
            try
            {
                using var smtp = CrearCliente();
                using var mensaje = new MailMessage
                {
                    From = new MailAddress(configuration["SMTP_FROM"]!),
                    Subject = correo.Asunto,
                    Body = correo.Cuerpo
                };
                mensaje.To.Add(correo.Destinatario);
                using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
                limite.CancelAfter(TimeSpan.FromSeconds(20));
                await smtp.SendMailAsync(mensaje, limite.Token);
            }
            catch (SmtpException) { error = "SMTP no aceptó el mensaje o no está disponible."; }
            catch (FormatException) { error = "Dirección o formato de correo inválido."; }
            catch (ArgumentException) { error = "Configuración o dirección SMTP inválida."; }
            catch (InvalidOperationException) { error = "Configuración SMTP incompleta."; }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                error = "Se agotó el tiempo de espera SMTP.";
            }
            // Cancelación del host conserva el reclamo: puede haber aceptación SMTP incierta.
            var estado = error is null ? EstadoCorreo.Enviado
                : correo.Intentos >= MaximoIntentos ? EstadoCorreo.Fallido : EstadoCorreo.Pendiente;
            DateTime? enviada = error is null ? DateTime.UtcNow : null;
            if (db.Database.IsRelational())
                await db.CorreosEnCola.Where(c => c.Id == id && c.ReclamoId == reclamo)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.Estado, estado)
                        .SetProperty(c => c.EnviadoEn, enviada).SetProperty(c => c.UltimoError, error)
                        .SetProperty(c => c.ReclamoId, (Guid?)null).SetProperty(c => c.ReclamoHasta, (DateTime?)null),
                        CancellationToken.None);
            else
            {
                correo.Estado = estado;
                correo.EnviadoEn = enviada;
                correo.UltimoError = error;
                correo.ReclamoId = null;
                correo.ReclamoHasta = null;
                await db.SaveChangesAsync(CancellationToken.None);
            }
            logger.LogInformation("Correo {CorreoId}: estado {Estado}, intento {Intentos}.", id, estado, correo.Intentos);
        }
    }

    private SmtpClient CrearCliente()
    {
        var host = configuration["SMTP_HOST"];
        var from = configuration["SMTP_FROM"];
        var auth = configuration["SMTP_USE_AUTH"] ?? "true";
        if (string.IsNullOrWhiteSpace(host) || !int.TryParse(configuration["SMTP_PORT"], out var port)
            || port is < 1 or > 65535 || !MailAddress.TryCreate(from, out _)
            || !bool.TryParse(configuration["SMTP_ENABLE_SSL"], out var ssl)
            || !bool.TryParse(auth, out var usarAuth))
            throw new InvalidOperationException("Configuración SMTP incompleta.");
        var username = configuration["SMTP_USERNAME"];
        var password = configuration["SMTP_PASSWORD"];
        if (usarAuth && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
            throw new InvalidOperationException("Credenciales SMTP ausentes.");
        return new SmtpClient(host, port)
        {
            EnableSsl = ssl, UseDefaultCredentials = false,
            Credentials = usarAuth ? new NetworkCredential(username, password) : null,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
    }
}
