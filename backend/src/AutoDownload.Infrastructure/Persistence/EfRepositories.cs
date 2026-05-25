using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace AutoDownload.Infrastructure.Persistence;

internal sealed class EfUserRepository : IUserRepository
{
    private readonly AutoDownloadDbContext dbContext;

    public EfUserRepository(AutoDownloadDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
        => await dbContext.Users.AddAsync(user, cancellationToken);

    public async Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<AppUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
        => await dbContext.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public async Task<bool> EmailExistsAsync(EmailAddress email, Guid? exceptUserId = null, CancellationToken cancellationToken = default)
        => await dbContext.Users.AnyAsync(user => user.Email == email && user.Id != exceptUserId, cancellationToken);
}

internal sealed class EfOperatorRepository : IOperatorRepository
{
    private readonly AutoDownloadDbContext dbContext;

    public EfOperatorRepository(AutoDownloadDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OperatorCompany>> ListActiveAsync(CancellationToken cancellationToken = default)
        => await dbContext.Operators
            .Where(operatorCompany => operatorCompany.IsActive)
            .OrderBy(operatorCompany => operatorCompany.Name)
            .ToListAsync(cancellationToken);

    public async Task<OperatorCompany?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Operators.FirstOrDefaultAsync(
            operatorCompany => operatorCompany.Id == id && operatorCompany.IsActive,
            cancellationToken);
}

internal sealed class EfUserAccountRepository : IUserAccountRepository
{
    private readonly AutoDownloadDbContext dbContext;

    public EfUserAccountRepository(AutoDownloadDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(UserAccount account, CancellationToken cancellationToken = default)
        => await dbContext.Accounts.AddAsync(account, cancellationToken);

    public Task RemoveAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        dbContext.Accounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task<UserAccount?> FindByIdForUserAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
        => await dbContext.Accounts.FirstOrDefaultAsync(
            account => account.UserId == userId && account.Id == accountId,
            cancellationToken);

    public async Task<IReadOnlyList<UserAccount>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Accounts
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserAccount>> ListDueAsync(DateTimeOffset dueAt, CancellationToken cancellationToken = default)
        => await dbContext.Accounts
            .Where(account => account.Status == AccountStatus.Active && account.NextRunAt <= dueAt)
            .OrderBy(account => account.NextRunAt)
            .ToListAsync(cancellationToken);

    public async Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Accounts.CountAsync(
            account => account.UserId == userId && account.Status == AccountStatus.Active,
            cancellationToken);
}

internal sealed class EfBillRepository : IBillRepository
{
    private readonly AutoDownloadDbContext dbContext;

    public EfBillRepository(AutoDownloadDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(Bill bill, CancellationToken cancellationToken = default)
        => await dbContext.Bills.AddAsync(bill, cancellationToken);

    public async Task<Bill?> FindByIdForUserAsync(Guid userId, Guid billId, CancellationToken cancellationToken = default)
        => await dbContext.Bills.FirstOrDefaultAsync(
            bill => bill.UserId == userId && bill.Id == billId,
            cancellationToken);

    public async Task<Bill?> FindByAccountAndReferenceAsync(Guid accountId, string reference, CancellationToken cancellationToken = default)
        => await dbContext.Bills.FirstOrDefaultAsync(
            bill => bill.AccountId == accountId && bill.Reference.ToLower() == reference.ToLower(),
            cancellationToken);

    public async Task<bool> ExistsAsync(Guid accountId, string reference, CancellationToken cancellationToken = default)
        => await dbContext.Bills.AnyAsync(
            bill => bill.AccountId == accountId && bill.Reference.ToLower() == reference.ToLower(),
            cancellationToken);

    public async Task<IReadOnlyList<Bill>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Bills
            .Where(bill => bill.UserId == userId)
            .OrderByDescending(bill => bill.DownloadedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Bill>> ListRecentByUserAsync(Guid userId, int count, CancellationToken cancellationToken = default)
        => await dbContext.Bills
            .Where(bill => bill.UserId == userId)
            .OrderByDescending(bill => bill.DownloadedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Bills.CountAsync(bill => bill.UserId == userId, cancellationToken);
}

internal sealed class EfAutomationRunRepository : IAutomationRunRepository
{
    private readonly AutoDownloadDbContext dbContext;

    public EfAutomationRunRepository(AutoDownloadDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(AutomationRun run, CancellationToken cancellationToken = default)
        => await dbContext.AutomationRuns.AddAsync(run, cancellationToken);

    public async Task<IReadOnlyList<AutomationRun>> ListByUserAsync(Guid userId, AutomationRunStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.AutomationRuns.Where(run => run.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(run => run.Status == status);
        }

        return await query
            .OrderByDescending(run => run.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AutomationRun>> ListRecentByUserAsync(Guid userId, int count, CancellationToken cancellationToken = default)
        => await dbContext.AutomationRuns
            .Where(run => run.UserId == userId)
            .OrderByDescending(run => run.StartedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByUserAndStatusAsync(Guid userId, AutomationRunStatus status, CancellationToken cancellationToken = default)
        => await dbContext.AutomationRuns.CountAsync(run => run.UserId == userId && run.Status == status, cancellationToken);
}

internal sealed class EfNotificationRepository : INotificationRepository
{
    private readonly AutoDownloadDbContext dbContext;

    public EfNotificationRepository(AutoDownloadDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await dbContext.Notifications.AddAsync(notification, cancellationToken);

    public Task RemoveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Remove(notification);
        return Task.CompletedTask;
    }

    public async Task<Notification?> FindByIdForUserAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
        => await dbContext.Notifications.FirstOrDefaultAsync(
            notification => notification.UserId == userId && notification.Id == notificationId,
            cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Notifications
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<int> CountUnreadByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Notifications.CountAsync(
            notification => notification.UserId == userId && notification.ReadAt == null,
            cancellationToken);
}
