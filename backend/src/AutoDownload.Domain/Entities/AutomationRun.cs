using AutoDownload.Domain.Common;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Domain.Entities;

public sealed class AutomationRun : Entity
{
    public AutomationRun(
        Guid id,
        Guid userId,
        Guid accountId,
        Guid operatorId,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        AutomationRunStatus status,
        string message,
        string? fileName)
        : base(id)
    {
        UserId = EnsureGuid(userId, nameof(userId));
        AccountId = EnsureGuid(accountId, nameof(accountId));
        OperatorId = EnsureGuid(operatorId, nameof(operatorId));
        StartedAt = startedAt;
        FinishedAt = finishedAt;
        Status = status;
        Message = EnsureMessage(message);
        FileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
    }

    public Guid UserId { get; }

    public Guid AccountId { get; }

    public Guid OperatorId { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset FinishedAt { get; }

    public AutomationRunStatus Status { get; }

    public string Message { get; }

    public string? FileName { get; }

    public int DurationSeconds => Math.Max(0, (int)Math.Round((FinishedAt - StartedAt).TotalSeconds));

    public static AutomationRun Create(
        Guid userId,
        Guid accountId,
        Guid operatorId,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        AutomationRunStatus status,
        string message,
        string? fileName)
        => new(Guid.NewGuid(), userId, accountId, operatorId, startedAt, finishedAt, status, message, fileName);

    private static Guid EnsureGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        return value;
    }

    private static string EnsureMessage(string message)
    {
        var normalized = (message ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 500)
        {
            throw new DomainException("Automation message is invalid.");
        }

        return normalized;
    }
}
