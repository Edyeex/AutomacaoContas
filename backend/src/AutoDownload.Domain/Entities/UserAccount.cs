using AutoDownload.Domain.Common;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Domain.Entities;

public sealed class UserAccount : Entity
{
    public const int MaxActiveAccountsPerUser = 3;

    public UserAccount(
        Guid id,
        Guid userId,
        Guid operatorId,
        string portalLogin,
        string encryptedPortalPassword,
        string customerIdentifier,
        AccountStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? lastRunAt,
        DateTimeOffset? nextRunAt,
        bool isScheduleEnabled,
        int? scheduleDayOfMonth,
        TimeOnly scheduleTime)
        : base(id)
    {
        UserId = EnsureGuid(userId, nameof(userId));
        OperatorId = EnsureGuid(operatorId, nameof(operatorId));
        PortalLogin = EnsurePortalLogin(portalLogin);
        EncryptedPortalPassword = EnsureEncryptedSecret(encryptedPortalPassword);
        CustomerIdentifier = EnsureCustomerIdentifier(customerIdentifier);
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LastRunAt = lastRunAt;
        NextRunAt = nextRunAt;
        IsScheduleEnabled = isScheduleEnabled;
        ScheduleDayOfMonth = scheduleDayOfMonth;
        ScheduleTime = scheduleTime;
    }

    public Guid UserId { get; }

    public Guid OperatorId { get; private set; }

    public string PortalLogin { get; private set; }

    public string EncryptedPortalPassword { get; private set; }

    public string CustomerIdentifier { get; private set; }

    public AccountStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? LastRunAt { get; private set; }

    public DateTimeOffset? NextRunAt { get; private set; }

    public bool IsScheduleEnabled { get; private set; }

    public int? ScheduleDayOfMonth { get; private set; }

    public TimeOnly ScheduleTime { get; private set; }

    public static UserAccount Create(
        Guid userId,
        Guid operatorId,
        string portalLogin,
        string encryptedPortalPassword,
        string customerIdentifier,
        DateTimeOffset now)
        => new(
            Guid.NewGuid(),
            userId,
            operatorId,
            portalLogin,
            encryptedPortalPassword,
            customerIdentifier,
            AccountStatus.Active,
            now,
            now,
            null,
            null,
            false,
            null,
            new TimeOnly(9, 0));

    public void Update(
        Guid operatorId,
        string portalLogin,
        string? encryptedPortalPassword,
        string customerIdentifier,
        DateTimeOffset now)
    {
        OperatorId = EnsureGuid(operatorId, nameof(operatorId));
        PortalLogin = EnsurePortalLogin(portalLogin);
        CustomerIdentifier = EnsureCustomerIdentifier(customerIdentifier);

        if (!string.IsNullOrWhiteSpace(encryptedPortalPassword))
        {
            EncryptedPortalPassword = EnsureEncryptedSecret(encryptedPortalPassword);
        }

        UpdatedAt = now;
    }

    public void ConfigureMonthlySchedule(
        int? dayOfMonth,
        TimeOnly scheduleTime,
        DateTimeOffset nextRunAt,
        DateTimeOffset now)
    {
        if (dayOfMonth is < 1 or > 31)
        {
            throw new DomainException("Schedule day must be between 1 and 31.");
        }

        IsScheduleEnabled = true;
        ScheduleDayOfMonth = dayOfMonth;
        ScheduleTime = scheduleTime;
        NextRunAt = nextRunAt;
        UpdatedAt = now;
    }

    public void DisableMonthlySchedule(DateTimeOffset now)
    {
        IsScheduleEnabled = false;
        NextRunAt = null;
        UpdatedAt = now;
    }

    public void MarkAutomationRun(DateTimeOffset ranAt, DateTimeOffset? nextRunAt)
    {
        LastRunAt = ranAt;
        NextRunAt = IsScheduleEnabled ? nextRunAt : null;
        UpdatedAt = ranAt;
    }

    public void Disable(DateTimeOffset now)
    {
        Status = AccountStatus.Inactive;
        UpdatedAt = now;
    }

    private static Guid EnsureGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        return value;
    }

    private static string EnsurePortalLogin(string portalLogin)
    {
        var normalized = (portalLogin ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 160)
        {
            throw new DomainException("Portal login must contain between 2 and 160 characters.");
        }

        return normalized;
    }

    private static string EnsureEncryptedSecret(string encryptedPortalPassword)
    {
        if (string.IsNullOrWhiteSpace(encryptedPortalPassword))
        {
            throw new DomainException("Encrypted portal password is required.");
        }

        return encryptedPortalPassword;
    }

    private static string EnsureCustomerIdentifier(string customerIdentifier)
    {
        var normalized = (customerIdentifier ?? string.Empty).Trim();
        if (normalized.Length is < 2 or > 80)
        {
            throw new DomainException("Customer identifier must contain between 2 and 80 characters.");
        }

        return normalized;
    }
}
