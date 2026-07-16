using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace EventGrok.Events.ApiTests.Fixtures;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Context.Request.Headers.TryGetValue("X-Test-UserId", out var userIdStr)
        || string.IsNullOrEmpty(userIdStr))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Context.Request.Headers.TryGetValue("X-Test-Role", out StringValues roleStr);
        string role = string.IsNullOrEmpty(roleStr) ? "User" : roleStr.ToString();

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userIdStr.ToString()),
            new(ClaimTypes.Role, role),
        ];

        ClaimsIdentity identity = new(claims, "TestScheme");
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}