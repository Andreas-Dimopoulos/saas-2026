using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Portal.Tests;

/// <summary>
/// Stands in for the real "the browser went to Google and came back" round trip. Hitting this
/// endpoint sets the same IdentityConstants.ExternalScheme cookie that a real Google callback
/// would, with claims taken from the query string, so tests can drive AccountController's real
/// ExternalLoginCallback logic without a live OAuth handshake.
/// </summary>
public class FakeExternalLoginStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Path == "/test/fake-external-login")
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, context.Request.Query["providerKey"].ToString())
                };

                var email = context.Request.Query["email"].ToString();
                if (!string.IsNullOrEmpty(email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, email));
                }

                var name = context.Request.Query["name"].ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    claims.Add(new Claim(ClaimTypes.Name, name));
                }

                claims.Add(new Claim("email_verified", context.Request.Query["verified"] == "true" ? "true" : "false"));

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Google"));
                var properties = new AuthenticationProperties();
                properties.Items["LoginProvider"] = "Google";

                await context.SignInAsync(IdentityConstants.ExternalScheme, principal, properties);
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }

            await nextMiddleware();
        });

        next(app);
    };
}
