using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;

namespace AutoDownload.Tests;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset Now { get; set; } = TestData.Now;

    public DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public PasswordCheckResult VerificationResult { get; set; } = PasswordCheckResult.Success;

    public int HashCalls { get; private set; }

    public string Hash(string password)
    {
        HashCalls++;
        return $"hash::{password}";
    }

    public PasswordCheckResult Verify(string passwordHash, string providedPassword)
        => VerificationResult;
}

internal sealed class FakeAccessTokenService : IAccessTokenService
{
    public int IssueCalls { get; private set; }

    public AccessToken Issue(AppUser user)
    {
        IssueCalls++;
        return new AccessToken("test-access-token", TestData.Now.AddHours(2));
    }

    public AccessTokenPrincipal? Validate(string token) => null;
}

internal sealed class FakeCredentialProtector : ICredentialProtector
{
    public string Protect(string plainText) => $"protected::{plainText}";

    public string Unprotect(string protectedText)
        => protectedText.Replace("protected::", string.Empty, StringComparison.Ordinal);
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<AppUser> Items { get; } = [];

    public Task AddAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }

    public Task<AppUser?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(user => user.Id == id));

    public Task<AppUser?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(user => user.Email.Value == email.Value));

    public Task<bool> EmailExistsAsync(
        EmailAddress email,
        Guid? exceptUserId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(user => user.Email.Value == email.Value && user.Id != exceptUserId));
}

internal sealed class FakeOperatorRepository : IOperatorRepository
{
    public List<OperatorCompany> Items { get; } = [];

    public Task<IReadOnlyList<OperatorCompany>> ListActiveAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OperatorCompany>>(Items.Where(item => item.IsActive).ToList());

    public Task<OperatorCompany?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(item => item.Id == id && item.IsActive));
}

internal sealed class FakeUserAccountRepository : IUserAccountRepository
{
    public List<UserAccount> Items { get; } = [];

    public Task AddAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        Items.Add(account);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        Items.Remove(account);
        return Task.CompletedTask;
    }

    public Task<UserAccount?> FindByIdForUserAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(account => account.UserId == userId && account.Id == accountId));

    public Task<IReadOnlyList<UserAccount>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserAccount>>(Items.Where(account => account.UserId == userId).ToList());

    public Task<IReadOnlyList<UserAccount>> ListDueAsync(
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserAccount>>(
            Items.Where(account => account.Status == AccountStatus.Active && account.NextRunAt <= dueAt).ToList());

    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Count(account => account.UserId == userId && account.Status == AccountStatus.Active));
}

internal sealed class FakeBillRepository : IBillRepository
{
    public List<Bill> Items { get; } = [];

    public Task AddAsync(Bill bill, CancellationToken cancellationToken = default)
    {
        Items.Add(bill);
        return Task.CompletedTask;
    }

    public Task<Bill?> FindByIdForUserAsync(Guid userId, Guid billId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(bill => bill.UserId == userId && bill.Id == billId));

    public Task<Bill?> FindByAccountAndReferenceAsync(
        Guid accountId,
        string reference,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(
            bill => bill.AccountId == accountId && bill.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsAsync(Guid accountId, string reference, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(
            bill => bill.AccountId == accountId && bill.Reference.Equals(reference, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Bill>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Bill>>(Items.Where(bill => bill.UserId == userId).ToList());

    public Task<IReadOnlyList<Bill>> ListRecentByUserAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Bill>>(
            Items.Where(bill => bill.UserId == userId).OrderByDescending(bill => bill.DownloadedAt).Take(count).ToList());

    public Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Count(bill => bill.UserId == userId));
}

internal sealed class FakeNotificationRepository : INotificationRepository
{
    public List<Notification> Items { get; } = [];

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        Items.Add(notification);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        Items.Remove(notification);
        return Task.CompletedTask;
    }

    public Task<Notification?> FindByIdForUserAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(item => item.UserId == userId && item.Id == notificationId));

    public Task<IReadOnlyList<Notification>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Notification>>(Items.Where(item => item.UserId == userId).ToList());

    public Task<int> CountUnreadByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Count(item => item.UserId == userId && !item.IsRead));
}

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 6, 18, 15, 0, 0, TimeSpan.Zero);

    public static AppUser User(Guid? id = null, string email = "maria@example.com")
        => new(
            id ?? Guid.NewGuid(),
            "Maria Silva",
            EmailAddress.Create(email),
            "hash::password",
            Now.AddDays(-10),
            Now.AddDays(-10));

    public static OperatorCompany Operator(Guid? id = null, bool active = true)
        => new(
            id ?? Guid.NewGuid(),
            $"operator-{Guid.NewGuid():N}"[..20],
            "Operadora Teste",
            ServiceType.Internet,
            new Uri("https://example.test/"),
            active);

    public static UserAccount Account(Guid userId, Guid operatorId)
        => UserAccount.Create(
            userId,
            operatorId,
            "portal.login",
            "protected::secret",
            "CUSTOMER-01",
            Now,
            Now.AddDays(5));

    public static Bill Bill(Guid userId, Guid accountId, Guid operatorId)
        => AutoDownload.Domain.Entities.Bill.Create(
            userId,
            accountId,
            operatorId,
            "Junho 2026",
            new DateOnly(2026, 7, 20),
            129.90m,
            "boleto_2026_06.pdf",
            "C:/downloads/boleto_2026_06.pdf",
            Now);
}
