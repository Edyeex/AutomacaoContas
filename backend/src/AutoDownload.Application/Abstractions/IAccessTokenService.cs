using AutoDownload.Domain.Entities;

namespace AutoDownload.Application.Abstractions;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record AccessTokenPrincipal(Guid UserId, string Email, DateTimeOffset ExpiresAt);

public interface IAccessTokenService
{
    AccessToken Issue(AppUser user);

    AccessTokenPrincipal? Validate(string token);
}
