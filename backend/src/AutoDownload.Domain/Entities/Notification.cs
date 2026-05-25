using AutoDownload.Domain.Common;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Domain.Entities;

public sealed class Notification : Entity
{
    public Notification(
        Guid id,
        Guid userId,
        string text,
        NotificationType type,
        DateTimeOffset createdAt,
        DateTimeOffset? readAt)
        : base(id)
    {
        UserId = EnsureGuid(userId, nameof(userId));
        Text = EnsureText(text);
        Type = type;
        CreatedAt = createdAt;
        ReadAt = readAt;
    }

    public Guid UserId { get; }

    public string Text { get; }

    public NotificationType Type { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    public static Notification Create(Guid userId, string text, NotificationType type, DateTimeOffset now)
        => new(Guid.NewGuid(), userId, text, type, now, null);

    public void MarkAsRead(DateTimeOffset now)
    {
        ReadAt ??= now;
    }

    public void MarkAsUnread()
    {
        ReadAt = null;
    }

    private static Guid EnsureGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        return value;
    }

    private static string EnsureText(string text)
    {
        var normalized = (text ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 300)
        {
            throw new DomainException("Notification text is invalid.");
        }

        return normalized;
    }
}
