namespace SISReservas.Api.DTOs.Notificaciones;

public record NotificacionDto(Guid Id, string Asunto, string Mensaje, DateTime CreadaEn, DateTime? LeidaEn);
public record BandejaDto(int NoLeidas, List<NotificacionDto> Notificaciones);
public record PreferenciasDto(bool CorreoHabilitado);
public record CambiarPreferenciasDto(
    [property: System.ComponentModel.DataAnnotations.Required] bool? CorreoHabilitado);
