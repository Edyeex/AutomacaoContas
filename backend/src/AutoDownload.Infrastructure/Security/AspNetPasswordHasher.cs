using AutoDownload.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace AutoDownload.Infrastructure.Security;

internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private static readonly object User = new();
    private readonly PasswordHasher<object> hasher = new();

    public string Hash(string password) => hasher.HashPassword(User, password);

    public PasswordCheckResult Verify(string passwordHash, string providedPassword)
        => hasher.VerifyHashedPassword(User, passwordHash, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
}
