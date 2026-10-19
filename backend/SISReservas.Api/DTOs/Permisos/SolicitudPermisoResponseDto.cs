using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Permisos;

public class SolicitudPermisoResponseDto
{
    public Guid Id { get; set; }

    public Guid SolicitanteId { get; set; }

    public string SolicitanteNombre { get; set; } = string.Empty;

    public Permiso Permiso { get; set; }

    public EstadoSolicitudPermiso Estado { get; set; }

    public DateTime CreadaEn { get; set; }

    public Guid? ResueltaPorId { get; set; }

    public string? ResueltaPorNombre { get; set; }

    public DateTime? ResueltaEn { get; set; }

    public DateTime? AplicadaEn { get; set; }

    public string? MotivoRechazo { get; set; }
}
