using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public class EmailQueueService
{
    private readonly SISReservasDbContext _db;

    public EmailQueueService(SISReservasDbContext db)
    {
        _db = db;
    }

    public async Task QueueAsync(
        Guid usuarioId,
        string destinatario,
        string asunto,
        string cuerpo)
    {
        var correo = new CorreoEnCola
        {
            UsuarioId = usuarioId,
            Destinatario = destinatario,
            Asunto = asunto,
            Cuerpo = cuerpo,
            Estado = EstadoCorreo.Pendiente
        };

        _db.CorreosEnCola.Add(correo);

        await _db.SaveChangesAsync();
    }
}