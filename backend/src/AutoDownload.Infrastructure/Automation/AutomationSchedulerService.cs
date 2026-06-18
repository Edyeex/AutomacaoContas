using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class AutomationSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AutomationSchedulerService> logger;
    private readonly TimeSpan interval;

    public AutomationSchedulerService(
        IServiceScopeFactory scopeFactory,
        IOptions<MonthlyScheduleOptions> options,
        ILogger<AutomationSchedulerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        interval = TimeSpan.FromSeconds(Math.Clamp(options.Value.IntervalSeconds, 10, 3600));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ExecuteDueRunsAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ExecuteDueRunsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();
            var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountRepository>();
            var orchestrator = scope.ServiceProvider.GetRequiredService<AutomationOrchestrator>();

            var dueAccounts = await accounts.ListDueAsync(clock.Now, cancellationToken);
            foreach (var account in dueAccounts)
            {
                var result = await orchestrator.RunAccountAsync(account, cancellationToken);
                if (result.IsFailure)
                {
                    logger.LogWarning(
                        "Scheduled automation for account {AccountId} was not started: {Error}",
                        account.Id,
                        result.Error?.Message ?? "Unknown error");
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automation scheduler failed.");
        }
    }
}
