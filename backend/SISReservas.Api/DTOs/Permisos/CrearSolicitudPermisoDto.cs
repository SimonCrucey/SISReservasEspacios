using System.ComponentModel.DataAnnotations;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.DTOs.Permisos;

public class CrearSolicitudPermisoDto
{
    [Required]
    public Permiso Permiso { get; set; }
}
