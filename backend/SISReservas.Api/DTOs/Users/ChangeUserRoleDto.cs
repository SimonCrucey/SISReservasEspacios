using SISReservas.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace SISReservas.Api.DTOs.Users;

public class ChangeUserRoleDto
{
    [Required]
    public Rol Rol { get; set; }
}