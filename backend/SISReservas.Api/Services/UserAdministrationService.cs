using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Users;
using SISReservas.Api.Models.Enums;

namespace SISReservas.Api.Services;

public class UserAdministrationService
{
    private readonly SISReservasDbContext _context;
    private readonly SessionService _sessionService;

    public UserAdministrationService(
        SISReservasDbContext context,
        SessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<List<UserResponseDto>> GetUsersAsync()
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Select(u => new UserResponseDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Email = u.Email,
                Rol = u.Rol,
                Activo = u.Activo
            })
            .ToListAsync();
    }

    public async Task<bool> ChangeRoleAsync(Guid userId, Rol rol)
    {
        var usuario = await _context.Usuarios.FindAsync(userId);

        if (usuario is null)
            return false;

        usuario.Rol = rol;

        await _context.SaveChangesAsync();

        await _sessionService.RevokeAllForUserAsync(usuario.Id);

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid userId)
    {
        var usuario = await _context.Usuarios.FindAsync(userId);

        if (usuario is null)
            return false;

        usuario.Activo = false;

        await _context.SaveChangesAsync();

        await _sessionService.RevokeAllForUserAsync(usuario.Id);

        return true;
    }

    public async Task<bool> ActivateAsync(Guid userId)
    {
        var usuario = await _context.Usuarios.FindAsync(userId);

        if (usuario is null)
            return false;

        usuario.Activo = true;

        await _context.SaveChangesAsync();

        return true;
    }
}