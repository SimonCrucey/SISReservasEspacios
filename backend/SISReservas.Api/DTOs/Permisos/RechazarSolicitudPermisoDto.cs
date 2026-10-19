using System.ComponentModel.DataAnnotations;

namespace SISReservas.Api.DTOs.Permisos;

public class RechazarSolicitudPermisoDto
{
    [Required]
    [MaxLength(500)]
    public string MotivoRechazo { get; set; } = string.Empty;
}
