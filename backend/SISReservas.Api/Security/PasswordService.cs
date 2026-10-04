using Microsoft.AspNetCore.Identity;
using SISReservas.Api.Models;

namespace SISReservas.Api.Security;

public class PasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string HashPassword(Usuario usuario, string password)
    {
        return _hasher.HashPassword(usuario, password);
    }

    public bool VerifyPassword(
        Usuario usuario,
        string passwordHash,
        string password)
    {
        var result = _hasher.VerifyHashedPassword(
            usuario,
            passwordHash,
            password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}