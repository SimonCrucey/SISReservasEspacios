using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Models;

public class SolicitudPermiso
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SolicitanteId { get; set; }

    public Usuario Solicitante { get; set; } = null!;

    public Permiso Permiso { get; set; }

    public EstadoSolicitudPermiso Estado { get; set; } =
        EstadoSolicitudPermiso.Pendiente;

    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;

    public Guid? ResueltaPorId { get; set; }

    public Usuario? ResueltaPor { get; set; }

    public DateTime? ResueltaEn { get; set; }

    public DateTime? AplicadaEn { get; set; }

    public string? MotivoRechazo { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
