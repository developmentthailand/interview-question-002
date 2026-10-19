using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Example.Com.InterviewQuestion002.Entities;
using Example.Com.InterviewQuestion002.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Example.Com.InterviewQuestion002.Services;

public sealed class AccessTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private readonly JwtOptions _options = options.Value;
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Convert.FromBase64String(options.Value.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public (string Token, DateTimeOffset ExpiresAtUtc) Create(User user)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.Add(TokenLifetime);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim("sub", user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
            ],
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
