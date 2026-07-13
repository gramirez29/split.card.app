using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;

namespace SplitCard.Infrastructure.Security;

/// <summary>
/// Issues the same kind of JWT that Split.Card.Api's JwtBearerOptions validates —
/// symmetric key, HS256, no issuer/audience (matches ValidateIssuer=false /
/// ValidateAudience=false in Program.cs). Claim types are the short JwtRegisteredClaimNames
/// form (e.g. "sub", not the long ClaimTypes URI) — Program.cs sets MapInboundClaims=false
/// so they arrive unchanged on the validation side; don't remove that flag without
/// updating ClaimsPrincipalExtensions.GetUserId() to match.
/// </summary>
public sealed class JwtTokenGenerator(JwtSettings settings) : ITokenGenerator
{
    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("householdId", user.HouseholdId),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
