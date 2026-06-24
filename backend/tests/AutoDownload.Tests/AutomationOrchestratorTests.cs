using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Services;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using System.Security.Cryptography;

namespace AutoDownload.Tests;

public sealed class AutomationOrchestratorTests
{
    [Fact]
    public async Task RunAccountAsync_WhenAutomationThrowsLongException_StoresLimitedFailure()
    {
        var fixture = CreateFixture();
        var longMessage = new string('x', 650);
        var service = fixture.CreateService(
            new ThrowingAutomationStrategy(new InvalidOperationException(longMessage)),
            new FakeCredentialProtector());

        var result = await service.RunAccountAsync(fixture.Account);

        Assert.True(result.IsSuccess);
        var run = Assert.Single(fixture.Runs.Items);
        var notification = Assert.Single(fixture.Notifications.Items);
        Assert.Equal(AutomationRunStatus.ConnectionError, run.Status);
        Assert.Equal(500, run.Message.Length);
        Assert.StartsWith("Falha inesperada ao executar a automacao:", run.Message);
        Assert.InRange(notification.Text.Length, 2, 300);
        Assert.Equal(NotificationType.Warning, notification.Type);
        Assert.Equal(1, fixture.UnitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task RunAccountAsync_WhenCredentialCannotBeRead_AsksUserToUpdatePassword()
    {
        var fixture = CreateFixture();
        var service = fixture.CreateService(
            new SuccessfulAutomationStrategy(),
            new ThrowingCredentialProtector());

        var result = await service.RunAccountAsync(fixture.Account);

        Assert.True(result.IsSuccess);
        var run = Assert.Single(fixture.Runs.Items);
        Assert.Equal(AutomationRunStatus.LoginFailed, run.Status);
        Assert.Equal(
            "Nao foi possivel ler a senha do portal. Edite a conta, informe a senha novamente e salve.",
            run.Message);
        Assert.Contains("Edite a conta", Assert.Single(fixture.Notifications.Items).Text);
    }

    private static AutomationFixture CreateFixture()
    {
        var userId = Guid.NewGuid();
        var operatorCompany = TestData.Operator();
        var account = TestData.Account(userId, operatorCompany.Id);
        var operators = new FakeOperatorRepository();
        var accounts = new FakeUserAccountRepository();

        operators.Items.Add(operatorCompany);
        accounts.Items.Add(account);

        return new AutomationFixture(
            account,
            accounts,
            operators,
            new FakeBillRepository(),
            new FakeAutomationRunRepository(),
            new FakeNotificationRepository(),
            new FakeUnitOfWork(),
            new FakeClock(),
            new FakeMonthlyScheduleCalculator());
    }

    private sealed record AutomationFixture(
        UserAccount Account,
        FakeUserAccountRepository Accounts,
        FakeOperatorRepository Operators,
        FakeBillRepository Bills,
        FakeAutomationRunRepository Runs,
        FakeNotificationRepository Notifications,
        FakeUnitOfWork UnitOfWork,
        FakeClock Clock,
        FakeMonthlyScheduleCalculator ScheduleCalculator)
    {
        public AutomationOrchestrator CreateService(
            IOperatorAutomationStrategy strategy,
            ICredentialProtector credentialProtector)
            => new(
                Accounts,
                Operators,
                Bills,
                Runs,
                Notifications,
                new FakeAutomationStrategyResolver(strategy),
                credentialProtector,
                ScheduleCalculator,
                Clock,
                new AutomationExecutionOptions(),
                UnitOfWork);
    }

    private sealed class FakeAutomationRunRepository : IAutomationRunRepository
    {
        public List<AutomationRun> Items { get; } = [];

        public Task AddAsync(AutomationRun run, CancellationToken cancellationToken = default)
        {
            Items.Add(run);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AutomationRun>> ListByUserAsync(
            Guid userId,
            AutomationRunStatus? status,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AutomationRun>>(
                Items.Where(item => item.UserId == userId && (status is null || item.Status == status)).ToList());

        public Task<IReadOnlyList<AutomationRun>> ListRecentByUserAsync(
            Guid userId,
            int count,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AutomationRun>>(
                Items.Where(item => item.UserId == userId).Take(count).ToList());

        public Task<int> CountByUserAndStatusAsync(
            Guid userId,
            AutomationRunStatus status,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Count(item => item.UserId == userId && item.Status == status));
    }

    private sealed class FakeAutomationStrategyResolver : IOperatorAutomationStrategyResolver
    {
        private readonly IOperatorAutomationStrategy strategy;

        public FakeAutomationStrategyResolver(IOperatorAutomationStrategy strategy)
        {
            this.strategy = strategy;
        }

        public IOperatorAutomationStrategy Resolve(OperatorCompany operatorCompany) => strategy;
    }

    private sealed class ThrowingAutomationStrategy : IOperatorAutomationStrategy
    {
        private readonly Exception exception;

        public ThrowingAutomationStrategy(Exception exception)
        {
            this.exception = exception;
        }

        public bool CanHandle(OperatorCompany operatorCompany) => true;

        public Task<AutomationDownloadResult> DownloadCurrentBillAsync(
            AutomationExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromException<AutomationDownloadResult>(exception);
    }

    private sealed class SuccessfulAutomationStrategy : IOperatorAutomationStrategy
    {
        public bool CanHandle(OperatorCompany operatorCompany) => true;

        public Task<AutomationDownloadResult> DownloadCurrentBillAsync(
            AutomationExecutionContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AutomationDownloadResult(
                AutomationRunStatus.Success,
                "Boleto baixado.",
                new BillDraft(
                    "Junho 2026",
                    new DateOnly(2026, 6, 30),
                    99.90m,
                    "boleto.pdf",
                    "C:/downloads/boleto.pdf")));
    }

    private sealed class ThrowingCredentialProtector : ICredentialProtector
    {
        public string Protect(string plainText) => plainText;

        public string Unprotect(string protectedText)
            => throw new CryptographicException("stored credential is invalid");
    }
}
