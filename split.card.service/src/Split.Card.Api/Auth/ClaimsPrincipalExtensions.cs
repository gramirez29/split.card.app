using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SplitCard.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the "sub" claim as-issued by JwtTokenGenerator. Requires
    /// options.MapInboundClaims = false in Program.cs's JwtBearerOptions — otherwise
    /// ASP.NET Core remaps "sub" to the long ClaimTypes.NameIdentifier URI and this
    /// lookup silently returns null.
    /// </summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Authenticated request is missing a 'sub' claim.");
}
