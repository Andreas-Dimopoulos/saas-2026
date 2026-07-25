using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TodoApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? throw new InvalidOperationException("Authenticated principal is missing an email claim.");
}
