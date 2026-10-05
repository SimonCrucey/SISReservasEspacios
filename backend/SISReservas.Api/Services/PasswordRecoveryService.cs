using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SISReservas.Api.Data;
using SISReservas.Api.Models;
using SISReservas.Api.Models.Enums;
using SISReservas.Api.Security;

namespace SISReservas.Api.Services;

public class PasswordRecoveryService
{
    private const int TokenExpirationMinutes = 30;

    private readonly SISReservasDbContext _db;
    private readonly PasswordService _passwordService;
    private readonly EmailQueueService _emailQueueService;
    private readonly SessionService _sessionService;
    private readonly PasswordPolicyService _passwordPolicyService;

    public PasswordRecoveryService(
        SISReservasDbContext db,
        PasswordService passwordService,
        EmailQueueService emailQueueService,
        SessionService sessionService,
        PasswordPolicyService passwordPolicyService)
    {
        _db = db;
        _passwordService = passwordService;
        _emailQueueService = emailQueueService;
        _sessionService = sessionService;
        _passwordPolicyService = passwordPolicyService;
    }

    public async Task RequestRecoveryAsync(string email)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);

        // No revelamos si el correo existe o no.
        if (usuario is null)
        {
            return;
        }

        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var token = new TokenAcceso
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            Tipo = TipoToken.RecuperacionPassword,
            ExpiraEn = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            Usado = false
        };

        _db.TokensAcceso.Add(token);

        await _db.SaveChangesAsync();

        var asunto = "Recuperación de contraseña";

        var cuerpo =
            $"Hola {usuario.Nombre},\n\n" +
            "Se ha solicitado una recuperación de contraseña para tu cuenta.\n\n" +
            $"Código de recuperación: {rawToken}\n\n" +
            $"Este código expira en {TokenExpirationMinutes} minutos " +
            "y solo puede utilizarse una vez.\n\n" +
            "Si no solicitaste este cambio, puedes ignorar este correo.";

        await _emailQueueService.QueueAsync(
            usuario.Id,
            usuario.Email,
            asunto,
            cuerpo);
    }

    public async Task<bool> ResetPasswordAsync(
        string rawToken,
        string nuevaPassword)
    {
        if (!_passwordPolicyService.IsValid(nuevaPassword))
        {
            return false;
        }

        var tokenHash = HashToken(rawToken);

        var token = await _db.TokensAcceso
            .Include(t => t.Usuario)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.Tipo == TipoToken.RecuperacionPassword &&
                !t.Usado);

        if (token is null)
        {
            return false;
        }

        if (token.ExpiraEn <= DateTime.UtcNow)
        {
            return false;
        }

        var usuario = token.Usuario;

        usuario.PasswordHash = _passwordService.HashPassword(
            usuario,
            nuevaPassword);

        token.Usado = true;

        await _db.SaveChangesAsync();

        await _sessionService.RevokeAllForUserAsync(usuario.Id);

        return true;
    }

    public async Task<bool> ChangePasswordAsync(
        Guid usuarioId,
        string passwordActual,
        string nuevaPassword)
    {
        if (!_passwordPolicyService.IsValid(nuevaPassword))
        {
            return false;
        }

        if (passwordActual == nuevaPassword)
        {
            return false;
        }

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return false;
        }

        var passwordCorrecta = _passwordService.VerifyPassword(
            usuario,
            usuario.PasswordHash,
            passwordActual);

        if (!passwordCorrecta)
        {
            return false;
        }

        usuario.PasswordHash = _passwordService.HashPassword(
            usuario,
            nuevaPassword);

        await _db.SaveChangesAsync();

        await _sessionService.RevokeAllForUserAsync(usuario.Id);

        return true;
    }

    public async Task<bool> ForcePasswordResetAsync(Guid usuarioId)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return false;
        }

        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var token = new TokenAcceso
        {
            UsuarioId = usuario.Id,
            TokenHash = tokenHash,
            Tipo = TipoToken.RecuperacionPassword,
            ExpiraEn = DateTime.UtcNow.AddMinutes(TokenExpirationMinutes),
            Usado = false
        };

        _db.TokensAcceso.Add(token);

        await _db.SaveChangesAsync();

        var asunto = "Restablecimiento de contraseña";

        var cuerpo =
            $"Hola {usuario.Nombre},\n\n" +
            "Un administrador ha solicitado un restablecimiento " +
            "de contraseña para tu cuenta.\n\n" +
            $"Código de recuperación: {rawToken}\n\n" +
            $"Este código expira en {TokenExpirationMinutes} minutos " +
            "y solo puede utilizarse una vez.";

        await _emailQueueService.QueueAsync(
            usuario.Id,
            usuario.Email,
            asunto,
            cuerpo);

        return true;
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}