using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.DTOs.Auth;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Security;

namespace SISReservas.Api.Services;

public class AuthService
{
    private readonly SISReservasDbContext _db;
    private readonly PasswordService _passwordService;
    private readonly TokenService _tokenService;
    private readonly EmailQueueService _emailQueue;

    public AuthService(
        SISReservasDbContext db,
        PasswordService passwordService,
        TokenService tokenService,
        EmailQueueService emailQueue)
    {
        _db = db;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailQueue = emailQueue;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (!PasswordPolicy.IsValid(request.Password))
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 8 caracteres, incluyendo letras y números.");
        }

        var exists = await _db.Usuarios
            .AnyAsync(u => u.Email == email);

        if (exists)
        {
            throw new InvalidOperationException(
                "El correo electrónico ya está registrado.");
        }

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Email = email,
            Activo = false,
            Rol = Rol.Estandar
        };

        usuario.PasswordHash =
            _passwordService.HashPassword(
                usuario,
                request.Password);

        _db.Usuarios.Add(usuario);

        await _db.SaveChangesAsync();

        await InvalidateActivationTokensAsync(usuario.Id);

        var rawToken = _tokenService.GenerateToken();
        var tokenHash = _tokenService.HashToken(rawToken);

        var token = new TokenAcceso
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            Tipo = TipoToken.Activacion,
            ExpiraEn = DateTime.UtcNow.AddHours(24),
            Usado = false
        };

        _db.TokensAcceso.Add(token);

        var activationUrl =
            $"https://localhost:7014/api/auth/activate?email={Uri.EscapeDataString(usuario.Email)}&token={Uri.EscapeDataString(rawToken)}";

        await _db.SaveChangesAsync();

        await _emailQueue.QueueAsync(
            usuario.Id,
            usuario.Email,
            "Activa tu cuenta",
            $"Hola {usuario.Nombre}, activa tu cuenta usando este enlace: {activationUrl}");

        return new AuthResponse
        {
            Message = "Registro realizado correctamente. Revisa tu correo para activar la cuenta."
        };
    }

    public async Task<AuthResponse> ActivateAsync(
        string email,
        string rawToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var tokenHash = _tokenService.HashToken(rawToken);

        var token = await _db.TokensAcceso
            .Include(t => t.Usuario)
            .SingleOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.Tipo == TipoToken.Activacion);

        if (token is null ||
            token.Usuario.Email != normalizedEmail ||
            token.Usado ||
            token.ExpiraEn <= DateTime.UtcNow)
        {
            throw new InvalidOperationException(
                "El enlace de activación no es válido o ya expiró.");
        }

        token.Usuario.Activo = true;
        token.Usado = true;

        await _db.SaveChangesAsync();

        return new AuthResponse
        {
            Message = "Cuenta activada correctamente."
        };
    }

    public async Task<AuthResponse> ResendActivationAsync(
        string email)
    {
        var normalizedEmail = NormalizeEmail(email);

        var usuario = await _db.Usuarios
            .SingleOrDefaultAsync(u =>
                u.Email == normalizedEmail);

        // La respuesta será la misma exista o no el correo.
        if (usuario is null || usuario.Activo)
        {
            return new AuthResponse
            {
                Message = "Si la cuenta puede recibir una activación, se ha enviado un nuevo enlace."
            };
        }

        await InvalidateActivationTokensAsync(usuario.Id);

        var rawToken = _tokenService.GenerateToken();
        var tokenHash = _tokenService.HashToken(rawToken);

        var token = new TokenAcceso
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            Tipo = TipoToken.Activacion,
            ExpiraEn = DateTime.UtcNow.AddHours(24),
            Usado = false
        };

        _db.TokensAcceso.Add(token);

        var activationUrl =
            $"https://localhost:7014/api/auth/activate?email={Uri.EscapeDataString(usuario.Email)}&token={Uri.EscapeDataString(rawToken)}";

        await _db.SaveChangesAsync();

        await _emailQueue.QueueAsync(
            usuario.Id,
            usuario.Email,
            "Nuevo enlace de activación",
            $"Hola {usuario.Nombre}, utiliza este enlace para activar tu cuenta: {activationUrl}");

        return new AuthResponse
        {
            Message = "Si la cuenta puede recibir una activación, se ha enviado un nuevo enlace."
        };
    }

    private async Task InvalidateActivationTokensAsync(
        Guid usuarioId)
    {
        var tokens = await _db.TokensAcceso
            .Where(t =>
                t.UsuarioId == usuarioId &&
                t.Tipo == TipoToken.Activacion &&
                !t.Usado)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Usado = true;
        }

        await _db.SaveChangesAsync();
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}