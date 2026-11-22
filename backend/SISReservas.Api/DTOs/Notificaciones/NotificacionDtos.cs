namespace SISReservas.Api.DTOs.Notificaciones;

public record NotificacionDto(Guid Id, string Asunto, string Mensaje, DateTime CreadaEn, DateTime? LeidaEn);
public record BandejaDto(int NoLeidas, List<NotificacionDto> Notificaciones);
public record PreferenciasDto(bool CorreoHabilitado);
public sealed class CambiarPreferenciasDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public bool? CorreoHabilitado { get; set; }
}
