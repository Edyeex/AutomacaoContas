using AutoDownload.Domain.Common;
using AutoDownload.Domain.ValueObjects;

namespace AutoDownload.Domain.Entities;

public sealed class AppUser : Entity
{
    public const int MinPasswordLength = 6;

    public AppUser(
        Guid id,
        string name,
        EmailAddress email,
        string passwordHash,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        Name = EnsureName(name);
        Email = email;
        PasswordHash = EnsurePasswordHash(passwordHash);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Name { get; private set; }

    public EmailAddress Email { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static AppUser Register(string name, EmailAddress email, string passwordHash, DateTimeOffset now)
        => new(Guid.NewGuid(), name, email, passwordHash, now, now);

    public void UpdateProfile(string name, EmailAddress email, DateTimeOffset now)
    {
        Name = EnsureName(name);
        Email = email;
        UpdatedAt = now;
    }

    public void ChangePasswordHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = EnsurePasswordHash(passwordHash);
        UpdatedAt = now;
    }

    private static string EnsureName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 120)
        {
            throw new DomainException("User name must contain between 2 and 120 characters.");
        }

        return normalized;
    }

    private static string EnsurePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        return passwordHash;
    }
}
