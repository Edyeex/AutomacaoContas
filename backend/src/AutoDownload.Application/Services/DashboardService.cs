using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Application.Services;

public sealed class DashboardService
{
    private readonly IUserAccountRepository accounts;
    private readonly IBillRepository bills;
    private readonly IAutomationRunRepository runs;
    private readonly INotificationRepository notifications;
    private readonly IOperatorRepository operators;

    public DashboardService(
        IUserAccountRepository accounts,
        IBillRepository bills,
        IAutomationRunRepository runs,
        INotificationRepository notifications,
        IOperatorRepository operators)
    {
        this.accounts = accounts;
        this.bills = bills;
        this.runs = runs;
        this.notifications = notifications;
        this.operators = operators;
    }

    public async Task<Result<DashboardResponse>> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userAccounts = await accounts.ListByUserAsync(userId, cancellationToken);
        var userBills = await bills.ListByUserAsync(userId, cancellationToken);
        var userRuns = await runs.ListByUserAsync(userId, null, cancellationToken);
        var operatorMap = (await operators.ListActiveAsync(cancellationToken)).ToDictionary(item => item.Id);
        var visibleAccounts = userAccounts
            .Where(item => operatorMap.ContainsKey(item.OperatorId))
            .ToList();
        var visibleBills = userBills
            .Where(item => operatorMap.ContainsKey(item.OperatorId))
            .ToList();
        var visibleRuns = userRuns
            .Where(item => operatorMap.ContainsKey(item.OperatorId))
            .ToList();

        var response = new DashboardResponse(
            visibleAccounts.Count,
            UserAccount.MaxActiveAccountsPerUser,
            visibleBills.Count,
            visibleRuns.Count(item => item.Status == AutomationRunStatus.Success),
            visibleRuns.Count(item => item.Status != AutomationRunStatus.Success),
            visibleAccounts
                .Where(item => item.NextRunAt.HasValue)
                .OrderBy(item => item.NextRunAt)
                .Select(item => item.NextRunAt)
                .FirstOrDefault(),
            await notifications.CountUnreadByUserAsync(userId, cancellationToken),
            visibleBills
                .OrderByDescending(item => item.DownloadedAt)
                .Take(3)
                .Select(item => item.ToResponse(operatorMap[item.OperatorId]))
                .ToList(),
            visibleRuns
                .OrderByDescending(item => item.StartedAt)
                .Take(5)
                .Select(item => item.ToResponse(operatorMap[item.OperatorId]))
                .ToList());

        return Result<DashboardResponse>.Success(response);
    }
}
