using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Services;

namespace AutoDownload.Tests;

public sealed class AccountServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_EncryptsPasswordAndPersistsAccount()
    {
        var userId = Guid.NewGuid();
        var operatorCompany = TestData.Operator();
        var accounts = new FakeUserAccountRepository();
        var operators = new FakeOperatorRepository();
        operators.Items.Add(operatorCompany);
        var unitOfWork = new FakeUnitOfWork();
        var service = new AccountService(
            accounts,
            operators,
            new FakeNotificationRepository(),
            new FakeCredentialProtector(),
            new FakeMonthlyScheduleCalculator(),
            new FakeClock(),
            unitOfWork);

        var result = await service.CreateAsync(
            userId,
            new AccountCreateRequest(operatorCompany.Id, "portal.login", "portal-secret", "CUSTOMER-01"));

        Assert.True(result.IsSuccess);
        var account = Assert.Single(accounts.Items);
        Assert.Equal("protected::portal-secret", account.EncryptedPortalPassword);
        Assert.Equal(operatorCompany.Id, account.OperatorId);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_WhenAccountLimitIsReached_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var operatorCompany = TestData.Operator();
        var accounts = new FakeUserAccountRepository();
        for (var index = 0; index < 3; index++)
        {
            accounts.Items.Add(TestData.Account(userId, operatorCompany.Id));
        }

        var operators = new FakeOperatorRepository();
        operators.Items.Add(operatorCompany);
        var unitOfWork = new FakeUnitOfWork();
        var service = new AccountService(
            accounts,
            operators,
            new FakeNotificationRepository(),
            new FakeCredentialProtector(),
            new FakeMonthlyScheduleCalculator(),
            new FakeClock(),
            unitOfWork);

        var result = await service.CreateAsync(
            userId,
            new AccountCreateRequest(operatorCompany.Id, "portal.login", "portal-secret", "CUSTOMER-04"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("account.limit_reached", result.Error?.Code);
        Assert.Equal(3, accounts.Items.Count);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccountAndCreatesNotification()
    {
        var userId = Guid.NewGuid();
        var operatorCompany = TestData.Operator();
        var account = TestData.Account(userId, operatorCompany.Id);
        var accounts = new FakeUserAccountRepository();
        accounts.Items.Add(account);
        var operators = new FakeOperatorRepository();
        operators.Items.Add(operatorCompany);
        var notifications = new FakeNotificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new AccountService(
            accounts,
            operators,
            notifications,
            new FakeCredentialProtector(),
            new FakeMonthlyScheduleCalculator(),
            new FakeClock(),
            unitOfWork);

        var result = await service.DeleteAsync(userId, account.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(accounts.Items);
        Assert.Contains("removida", Assert.Single(notifications.Items).Text, StringComparison.Ordinal);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }

    [Fact]
    public async Task ConfigureScheduleAsync_EnablesRecurringScheduleAndPersistsNextRun()
    {
        var userId = Guid.NewGuid();
        var operatorCompany = TestData.Operator();
        var account = TestData.Account(userId, operatorCompany.Id);
        var accounts = new FakeUserAccountRepository();
        accounts.Items.Add(account);
        var operators = new FakeOperatorRepository();
        operators.Items.Add(operatorCompany);
        var notifications = new FakeNotificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var calculator = new FakeMonthlyScheduleCalculator
        {
            Next = TestData.Now.AddMonths(1)
        };
        var service = new AccountService(
            accounts,
            operators,
            notifications,
            new FakeCredentialProtector(),
            calculator,
            new FakeClock(),
            unitOfWork);

        var result = await service.ConfigureScheduleAsync(
            userId,
            account.Id,
            new AccountScheduleRequest(true, 25, false, new TimeOnly(8, 30)));

        Assert.True(result.IsSuccess);
        Assert.True(account.IsScheduleEnabled);
        Assert.Equal(25, account.ScheduleDayOfMonth);
        Assert.Equal(new TimeOnly(8, 30), account.ScheduleTime);
        Assert.Equal(calculator.Next, account.NextRunAt);
        Assert.Contains("ativado", Assert.Single(notifications.Items).Text, StringComparison.Ordinal);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
    }
}
