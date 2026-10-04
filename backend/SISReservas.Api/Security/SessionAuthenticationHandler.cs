using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SISReservas.Api.Services;

namespace SISReservas.Api.Security;

public class SessionAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly SessionService _sessionService;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SessionService sessionService)
        : base(options, logger, encoder)
    {
        _sessionService = sessionService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            return AuthenticateResult.NoResult();
        }

        var value = authorization.ToString();

        if (!value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Sesión inválida.");
        }

        var rawToken = value["Bearer ".Length..].Trim();

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return AuthenticateResult.Fail("Sesión inválida.");
        }

        var session = await _sessionService.GetValidSessionAsync(rawToken);

        if (session == null)
        {
            return AuthenticateResult.Fail("Sesión inválida.");
        }

        if (!session.Usuario.Activo)
        {
            return AuthenticateResult.Fail("Usuario inactivo.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.Usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, session.Usuario.Nombre),
            new Claim(ClaimTypes.Email, session.Usuario.Email),
            new Claim(ClaimTypes.Role, session.Usuario.Rol.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}