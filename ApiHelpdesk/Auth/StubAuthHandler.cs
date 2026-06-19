using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace ApiHelpdesk.Auth;

public class StubAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public StubAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Devolvemos NoResult para que no interfiera. 
        // El StubAuthMiddleware se encargará de asignar el contexto del usuario.
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}