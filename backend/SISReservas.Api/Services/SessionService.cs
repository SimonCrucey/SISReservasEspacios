using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Security;
using System.Security.Cryptography;

namespace SISReservas.Api.Services;

public class SessionService
{
    private readonly SISReservasDbContext _db;
    private readonly TokenService _tokenService;

    public SessionService(
        SISReservasDbContext db,
        TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<(string RawToken, Sesion Session)> CreateAsync(
        Usuario usuario)
    {
        var rawToken = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var session = new Sesion
        {
            UsuarioId = usuario.Id,
            TokenHash = _tokenService.HashToken(rawToken)
        };

        _db.Sesiones.Add(session);

        await _db.SaveChangesAsync();

        return (rawToken, session);
    }

    public async Task<Sesion?> GetValidSessionAsync(
        string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = _tokenService.HashToken(rawToken);

        return await _db.Sesiones
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s =>
                s.TokenHash == hash &&
                s.RevocadaEn == null);
    }

    public async Task RevokeAsync(string rawToken)
    {
        var hash = _tokenService.HashToken(rawToken);

        var session = await _db.Sesiones
            .FirstOrDefaultAsync(s =>
                s.TokenHash == hash &&
                s.RevocadaEn == null);

        if (session == null)
        {
            return;
        }

        session.RevocadaEn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(Guid usuarioId)
    {
        var sessions = await _db.Sesiones
            .Where(s =>
                s.UsuarioId == usuarioId &&
                s.RevocadaEn == null)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.RevocadaEn = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }
}