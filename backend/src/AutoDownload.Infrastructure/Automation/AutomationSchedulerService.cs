using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class AutomationSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AutomationSchedulerService> logger;

    public AutomationSchedulerService(IServiceScopeFactory scopeFactory, ILogger<AutomationSchedulerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(12));

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
                await orchestrator.RunAccountAsync(account, cancellationToken);
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
