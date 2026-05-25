using AutoDownload.Domain.Common;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Domain.Entities;

public sealed class Bill : Entity
{
    public Bill(
        Guid id,
        Guid userId,
        Guid accountId,
        Guid operatorId,
        string reference,
        DateOnly dueDate,
        decimal amount,
        string fileName,
        string storagePath,
        DateTimeOffset downloadedAt,
        BillStatus status)
        : base(id)
    {
        UserId = EnsureGuid(userId, nameof(userId));
        AccountId = EnsureGuid(accountId, nameof(accountId));
        OperatorId = EnsureGuid(operatorId, nameof(operatorId));
        Reference = EnsureReference(reference);
        DueDate = dueDate;
        Amount = EnsureAmount(amount);
        FileName = EnsureFileName(fileName);
        StoragePath = EnsureStoragePath(storagePath);
        DownloadedAt = downloadedAt;
        Status = status;
    }

    public Guid UserId { get; }

    public Guid AccountId { get; }

    public Guid OperatorId { get; }

    public string Reference { get; }

    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public string FileName { get; private set; }

    public string StoragePath { get; private set; }

    public DateTimeOffset DownloadedAt { get; private set; }

    public BillStatus Status { get; private set; }

    public static Bill Create(
        Guid userId,
        Guid accountId,
        Guid operatorId,
        string reference,
        DateOnly dueDate,
        decimal amount,
        string fileName,
        string storagePath,
        DateTimeOffset downloadedAt)
        => new(
            Guid.NewGuid(),
            userId,
            accountId,
            operatorId,
            reference,
            dueDate,
            amount,
            fileName,
            storagePath,
            downloadedAt,
            BillStatus.Available);

    public void RefreshDownload(
        DateOnly dueDate,
        decimal amount,
        string fileName,
        string storagePath,
        DateTimeOffset downloadedAt)
    {
        DueDate = dueDate;
        Amount = EnsureAmount(amount);
        FileName = EnsureFileName(fileName);
        StoragePath = EnsureStoragePath(storagePath);
        DownloadedAt = downloadedAt;
        Status = BillStatus.Available;
    }

    private static Guid EnsureGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        return value;
    }

    private static string EnsureReference(string reference)
    {
        var normalized = (reference ?? string.Empty).Trim();
        if (normalized.Length is < 4 or > 30)
        {
            throw new DomainException("Bill reference is invalid.");
        }

        return normalized;
    }

    private static decimal EnsureAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new DomainException("Bill amount cannot be negative.");
        }

        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static string EnsureFileName(string fileName)
    {
        var normalized = (fileName ?? string.Empty).Trim();
        if (normalized.Length is < 5 or > 180)
        {
            throw new DomainException("Bill file name is invalid.");
        }

        return normalized;
    }

    private static string EnsureStoragePath(string storagePath)
    {
        var normalized = (storagePath ?? string.Empty).Trim();
        if (normalized.Length is < 5 or > 260)
        {
            throw new DomainException("Bill storage path is invalid.");
        }

        return normalized;
    }
}
