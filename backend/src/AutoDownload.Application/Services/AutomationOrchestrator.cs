using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;
using AutoDownload.Domain.Common;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using System.Security.Cryptography;

namespace AutoDownload.Application.Services;

public sealed class AutomationOrchestrator
{
    private readonly IUserAccountRepository accounts;
    private readonly IOperatorRepository operators;
    private readonly IBillRepository bills;
    private readonly IAutomationRunRepository runs;
    private readonly INotificationRepository notifications;
    private readonly IOperatorAutomationStrategyResolver strategyResolver;
    private readonly ICredentialProtector credentialProtector;
    private readonly IMonthlyScheduleCalculator scheduleCalculator;
    private readonly IClock clock;
    private readonly AutomationExecutionOptions executionOptions;
    private readonly IUnitOfWork unitOfWork;

    public AutomationOrchestrator(
        IUserAccountRepository accounts,
        IOperatorRepository operators,
        IBillRepository bills,
        IAutomationRunRepository runs,
        INotificationRepository notifications,
        IOperatorAutomationStrategyResolver strategyResolver,
        ICredentialProtector credentialProtector,
        IMonthlyScheduleCalculator scheduleCalculator,
        IClock clock,
        AutomationExecutionOptions executionOptions,
        IUnitOfWork unitOfWork)
    {
        this.accounts = accounts;
        this.operators = operators;
        this.bills = bills;
        this.runs = runs;
        this.notifications = notifications;
        this.strategyResolver = strategyResolver;
        this.credentialProtector = credentialProtector;
        this.scheduleCalculator = scheduleCalculator;
        this.clock = clock;
        this.executionOptions = executionOptions;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<HistoryResponse>> RunAccountAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByIdForUserAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return Result<HistoryResponse>.Failure(Error.NotFound("account.not_found", "Account not found."));
        }

        return await RunAccountAsync(account, cancellationToken);
    }

    public async Task<Result<HistoryResponse>> RunAccountAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        var operatorCompany = await operators.FindByIdAsync(account.OperatorId, cancellationToken);
        if (operatorCompany is null)
        {
            return Result<HistoryResponse>.Failure(Error.NotFound("operator.not_found", "Operator not found."));
        }

        var startedAt = clock.Now;
        AutomationDownloadResult result;

        try
        {
            var strategy = strategyResolver.Resolve(operatorCompany);
            var credential = new PortalCredential(
                account.PortalLogin,
                credentialProtector.Unprotect(account.EncryptedPortalPassword),
                account.CustomerIdentifier);

            result = await DownloadWithTimeoutAsync(
                strategy,
                new AutomationExecutionContext(account.UserId, account, operatorCompany, credential, clock.Today),
                executionOptions.Timeout);
        }
        catch (DomainException ex)
        {
            result = new AutomationDownloadResult(AutomationRunStatus.Failed, ex.Message, null);
        }
        catch (CryptographicException)
        {
            result = new AutomationDownloadResult(
                AutomationRunStatus.LoginFailed,
                "Nao foi possivel ler a senha do portal. Edite a conta, informe a senha novamente e salve.",
                null);
        }
        catch (OperationCanceledException)
        {
            result = new AutomationDownloadResult(
                AutomationRunStatus.ConnectionError,
                "Execucao da automacao interrompida antes da conclusao.",
                null);
        }
        catch (Exception ex)
        {
            result = new AutomationDownloadResult(
                AutomationRunStatus.ConnectionError,
                BuildUnexpectedFailureMessage(ex),
                null);
        }

        result = NormalizeResult(result);

        var finishedAt = clock.Now;
        Bill? createdBill = null;

        if (result.Succeeded)
        {
            var draft = result.Bill!;
            createdBill = await bills.FindByAccountAndReferenceAsync(account.Id, draft.Reference, cancellationToken);
            if (createdBill is null)
            {
                createdBill = Bill.Create(
                    account.UserId,
                    account.Id,
                    operatorCompany.Id,
                    draft.Reference,
                    draft.DueDate,
                    draft.Amount,
                    draft.FileName,
                    draft.StoragePath,
                    finishedAt);
                await bills.AddAsync(createdBill, cancellationToken);
            }
            else
            {
                createdBill.RefreshDownload(
                    draft.DueDate,
                    draft.Amount,
                    draft.FileName,
                    draft.StoragePath,
                    finishedAt);
            }
        }

        var run = AutomationRun.Create(
            account.UserId,
            account.Id,
            operatorCompany.Id,
            startedAt,
            finishedAt,
            result.Status,
            result.Message,
            result.Bill?.FileName ?? createdBill?.FileName);

        await runs.AddAsync(run, cancellationToken);
        DateTimeOffset? nextRunAt = account.IsScheduleEnabled
            ? scheduleCalculator.CalculateNext(
                finishedAt,
                finishedAt,
                account.ScheduleDayOfMonth,
                account.ScheduleTime)
            : null;
        account.MarkAutomationRun(finishedAt, nextRunAt);

        await notifications.AddAsync(
            Notification.Create(
                account.UserId,
                BuildDetailedNotificationText(operatorCompany.Name, result),
                result.Status == AutomationRunStatus.Success ? NotificationType.Success : NotificationType.Warning,
                finishedAt),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HistoryResponse>.Success(run.ToResponse(operatorCompany));
    }

    private static string BuildDetailedNotificationText(string operatorName, AutomationDownloadResult result)
    {
        if (result.Status == AutomationRunStatus.Success)
        {
            return $"Boleto {operatorName} baixado com sucesso.";
        }

        var notificationText = string.IsNullOrWhiteSpace(result.Message)
            ? $"Falha ao executar automacao para {operatorName}."
            : $"{operatorName}: {result.Message}";

        return LimitText(notificationText, 300);
    }

    private static string BuildUnexpectedFailureMessage(Exception exception)
    {
        var baseException = exception.GetBaseException();
        var message = string.IsNullOrWhiteSpace(baseException.Message)
            ? exception.Message
            : baseException.Message;

        return LimitText(
            $"Falha inesperada ao executar a automacao: {NormalizeMessage(message)}",
            500);
    }

    private static AutomationDownloadResult NormalizeResult(AutomationDownloadResult result)
        => result with
        {
            Message = LimitText(NormalizeMessage(result.Message), 500)
        };

    private static string NormalizeMessage(string message)
        => string.Join(' ', message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string LimitText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..Math.Max(0, maxLength - 3)] + "...";
    }

    private static string BuildNotificationText(string operatorName, AutomationDownloadResult result)
        => result.Status switch
        {
            AutomationRunStatus.Success => $"Boleto {operatorName} baixado com sucesso.",
            AutomationRunStatus.BillUnavailable => $"Boleto {operatorName} ainda não disponível no portal.",
            AutomationRunStatus.LoginFailed => $"Falha de login ao acessar {operatorName}.",
            AutomationRunStatus.ConnectionError => $"Falha de conexao ao acessar {operatorName}.",
            _ => $"Falha ao executar automacao para {operatorName}."
        };

    private static async Task<AutomationDownloadResult> DownloadWithTimeoutAsync(
        IOperatorAutomationStrategy strategy,
        AutomationExecutionContext context,
        TimeSpan timeout)
    {
        var executionTimeout = new CancellationTokenSource();
        var automationTask = strategy.DownloadCurrentBillAsync(context, executionTimeout.Token);
        var completedTask = await Task.WhenAny(automationTask, Task.Delay(timeout));

        if (completedTask == automationTask)
        {
            executionTimeout.Dispose();
            return await automationTask;
        }

        await executionTimeout.CancelAsync();
        ObserveTimedOutAutomation(automationTask, executionTimeout);

        return new AutomationDownloadResult(
            AutomationRunStatus.ConnectionError,
            $"Tempo limite de {timeout.TotalSeconds:0} segundos atingido ao executar a automacao. Tente novamente mais tarde ou use o operador demo para demonstracao.",
            null);
    }

    private static void ObserveTimedOutAutomation(
        Task<AutomationDownloadResult> automationTask,
        CancellationTokenSource executionTimeout)
        => _ = automationTask.ContinueWith(
            completedTask =>
            {
                _ = completedTask.Exception;
                executionTimeout.Dispose();
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
}
