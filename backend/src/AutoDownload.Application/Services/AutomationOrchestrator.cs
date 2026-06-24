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

            result = await strategy.DownloadCurrentBillAsync(
                new AutomationExecutionContext(account.UserId, account, operatorCompany, credential, clock.Today),
                cancellationToken);
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
            throw;
        }
        catch (Exception)
        {
            result = new AutomationDownloadResult(AutomationRunStatus.ConnectionError, "Falha inesperada ao executar a automacao.", null);
        }

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
                BuildNotificationText(operatorCompany.Name, result),
                result.Status == AutomationRunStatus.Success ? NotificationType.Success : NotificationType.Warning,
                finishedAt),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HistoryResponse>.Success(run.ToResponse(operatorCompany));
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
}
