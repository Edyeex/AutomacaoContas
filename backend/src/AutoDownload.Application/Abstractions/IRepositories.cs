using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;

namespace AutoDownload.Application.Abstractions;

public interface IUserRepository
{
    Task AddAsync(AppUser user, CancellationToken cancellationToken = default);

    Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AppUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(EmailAddress email, Guid? exceptUserId = null, CancellationToken cancellationToken = default);
}

public interface IOperatorRepository
{
    Task<IReadOnlyList<OperatorCompany>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<OperatorCompany?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IUserAccountRepository
{
    Task AddAsync(UserAccount account, CancellationToken cancellationToken = default);

    Task RemoveAsync(UserAccount account, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByIdForUserAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccount>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccount>> ListDueAsync(DateTimeOffset dueAt, CancellationToken cancellationToken = default);

    Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IBillRepository
{
    Task AddAsync(Bill bill, CancellationToken cancellationToken = default);

    Task<Bill?> FindByIdForUserAsync(Guid userId, Guid billId, CancellationToken cancellationToken = default);

    Task<Bill?> FindByAccountAndReferenceAsync(Guid accountId, string reference, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid accountId, string reference, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Bill>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Bill>> ListRecentByUserAsync(Guid userId, int count, CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IAutomationRunRepository
{
    Task AddAsync(AutomationRun run, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutomationRun>> ListByUserAsync(Guid userId, AutomationRunStatus? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutomationRun>> ListRecentByUserAsync(Guid userId, int count, CancellationToken cancellationToken = default);

    Task<int> CountByUserAndStatusAsync(Guid userId, AutomationRunStatus status, CancellationToken cancellationToken = default);
}

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task RemoveAsync(Notification notification, CancellationToken cancellationToken = default);

    Task<Notification?> FindByIdForUserAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountUnreadByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
