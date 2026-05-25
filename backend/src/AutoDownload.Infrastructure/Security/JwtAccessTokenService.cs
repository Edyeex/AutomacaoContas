using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AutoDownload.Infrastructure.Security;

public sealed class JwtAccessTokenService : IAccessTokenService
{
    private readonly AccessTokenOptions options;
    private readonly IClock clock;

    public JwtAccessTokenService(IOptions<AccessTokenOptions> options, IClock clock)
    {
        this.options = options.Value;
        this.clock = clock;
    }

    public AccessToken Issue(AppUser user)
    {
        var expiresAt = clock.Now.AddMinutes(Math.Max(15, options.ExpirationMinutes));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email.Value),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: clock.Now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public AccessTokenPrincipal? Validate(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, BuildValidationParameters(options), out _);
            var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);
            var expiresAt = principal.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var expiration = long.TryParse(expiresAt, out var unixSeconds)
                ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                : DateTimeOffset.MinValue;

            return new AccessTokenPrincipal(userId, email, expiration);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static TokenValidationParameters BuildValidationParameters(AccessTokenOptions options)
        => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
}
