using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;
using AutoDownload.Domain.Common;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Application.Services;

public sealed class AccountService
{
    private readonly IUserAccountRepository accounts;
    private readonly IOperatorRepository operators;
    private readonly INotificationRepository notifications;
    private readonly ICredentialProtector credentialProtector;
    private readonly IMonthlyScheduleCalculator scheduleCalculator;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public AccountService(
        IUserAccountRepository accounts,
        IOperatorRepository operators,
        INotificationRepository notifications,
        ICredentialProtector credentialProtector,
        IMonthlyScheduleCalculator scheduleCalculator,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.accounts = accounts;
        this.operators = operators;
        this.notifications = notifications;
        this.credentialProtector = credentialProtector;
        this.scheduleCalculator = scheduleCalculator;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<OperatorResponse>>> ListOperatorsAsync(CancellationToken cancellationToken = default)
    {
        var list = await operators.ListActiveAsync(cancellationToken);
        return Result<IReadOnlyList<OperatorResponse>>.Success(list.Select(item => item.ToResponse()).ToList());
    }

    public async Task<Result<IReadOnlyList<AccountResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await accounts.ListByUserAsync(userId, cancellationToken);
        return Result<IReadOnlyList<AccountResponse>>.Success(await MapAccountsAsync(list, cancellationToken));
    }

    public async Task<Result<AccountPortalPasswordResponse>> GetPortalPasswordAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByIdForUserAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return Result<AccountPortalPasswordResponse>.Failure(Error.NotFound("account.not_found", "Account not found."));
        }

        try
        {
            var portalPassword = credentialProtector.Unprotect(account.EncryptedPortalPassword);
            return Result<AccountPortalPasswordResponse>.Success(new AccountPortalPasswordResponse(portalPassword));
        }
        catch (Exception)
        {
            return Result<AccountPortalPasswordResponse>.Failure(Error.Failure(
                "account.portal_password_unreadable",
                "Nao foi possivel ler a senha cadastrada. Edite a conta e informe a senha novamente."));
        }
    }

    public async Task<Result<AccountResponse>> CreateAsync(Guid userId, AccountCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (await accounts.CountActiveByUserAsync(userId, cancellationToken) >= UserAccount.MaxActiveAccountsPerUser)
        {
            return Result<AccountResponse>.Failure(Error.Conflict("account.limit_reached", "A user can register up to 3 active accounts."));
        }

        if (string.IsNullOrWhiteSpace(request.SenhaPortal))
        {
            return Result<AccountResponse>.Failure(Error.Validation("account.portal_password_required", "Portal password is required."));
        }

        var operatorCompany = await operators.FindByIdAsync(request.OperadoraId, cancellationToken);
        if (operatorCompany is null)
        {
            return Result<AccountResponse>.Failure(Error.NotFound("operator.not_found", "Operator not found."));
        }

        try
        {
            var account = UserAccount.Create(
                userId,
                operatorCompany.Id,
                request.LoginPortal,
                credentialProtector.Protect(request.SenhaPortal),
                request.UnidadeConsumidora,
                clock.Now);

            await accounts.AddAsync(account, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AccountResponse>.Success(account.ToResponse(operatorCompany));
        }
        catch (DomainException ex)
        {
            return Result<AccountResponse>.Failure(Error.Validation("account.invalid", ex.Message));
        }
    }

    public async Task<Result<AccountResponse>> UpdateAsync(Guid userId, Guid accountId, AccountUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByIdForUserAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return Result<AccountResponse>.Failure(Error.NotFound("account.not_found", "Account not found."));
        }

        var operatorCompany = await operators.FindByIdAsync(request.OperadoraId, cancellationToken);
        if (operatorCompany is null)
        {
            return Result<AccountResponse>.Failure(Error.NotFound("operator.not_found", "Operator not found."));
        }

        try
        {
            var encryptedPassword = string.IsNullOrWhiteSpace(request.SenhaPortal)
                ? null
                : credentialProtector.Protect(request.SenhaPortal);

            account.Update(
                operatorCompany.Id,
                request.LoginPortal,
                encryptedPassword,
                request.UnidadeConsumidora,
                clock.Now);

            await notifications.AddAsync(
                Notification.Create(
                    userId,
                    $"Conta {operatorCompany.Name} editada com sucesso.",
                    NotificationType.Info,
                    clock.Now),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AccountResponse>.Success(account.ToResponse(operatorCompany));
        }
        catch (DomainException ex)
        {
            return Result<AccountResponse>.Failure(Error.Validation("account.invalid", ex.Message));
        }
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByIdForUserAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure(Error.NotFound("account.not_found", "Account not found."));
        }

        var operatorCompany = await operators.FindByIdAsync(account.OperatorId, cancellationToken);
        var operatorName = operatorCompany?.Name ?? "selecionada";

        await accounts.RemoveAsync(account, cancellationToken);
        await notifications.AddAsync(
            Notification.Create(
                userId,
                $"Conta {operatorName} removida com sucesso.",
                NotificationType.Warning,
                clock.Now),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<AccountResponse>> ConfigureScheduleAsync(
        Guid userId,
        Guid accountId,
        AccountScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = await accounts.FindByIdForUserAsync(userId, accountId, cancellationToken);
        if (account is null)
        {
            return Result<AccountResponse>.Failure(Error.NotFound("account.not_found", "Account not found."));
        }

        var operatorCompany = await operators.FindByIdAsync(account.OperatorId, cancellationToken);
        if (operatorCompany is null)
        {
            return Result<AccountResponse>.Failure(Error.NotFound("operator.not_found", "Operator not found."));
        }

        try
        {
            if (!request.Enabled)
            {
                account.DisableMonthlySchedule(clock.Now);
                await AddScheduleNotificationAsync(userId, operatorCompany.Name, "desativado", cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<AccountResponse>.Success(account.ToResponse(operatorCompany));
            }

            if (request.LastDayOfMonth && request.DayOfMonth.HasValue)
            {
                return Result<AccountResponse>.Failure(Error.Validation(
                    "account.schedule_day_conflict",
                    "Choose a day of the month or the last day, not both."));
            }

            if (!request.LastDayOfMonth && request.DayOfMonth is null)
            {
                return Result<AccountResponse>.Failure(Error.Validation(
                    "account.schedule_day_required",
                    "Schedule day is required."));
            }

            var dayOfMonth = request.LastDayOfMonth ? null : request.DayOfMonth;
            var now = clock.Now;
            var nextRunAt = scheduleCalculator.CalculateNext(
                now,
                account.LastRunAt,
                dayOfMonth,
                request.Time);

            account.ConfigureMonthlySchedule(dayOfMonth, request.Time, nextRunAt, now);
            var scheduleDescription = request.LastDayOfMonth
                ? $"no último dia de cada mês às {request.Time:HH\\:mm}"
                : $"todo dia {dayOfMonth} às {request.Time:HH\\:mm}";
            await AddScheduleNotificationAsync(
                userId,
                operatorCompany.Name,
                $"ativado para {scheduleDescription}",
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<AccountResponse>.Success(account.ToResponse(operatorCompany));
        }
        catch (DomainException ex)
        {
            return Result<AccountResponse>.Failure(Error.Validation("account.schedule_invalid", ex.Message));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result<AccountResponse>.Failure(Error.Validation("account.schedule_invalid", ex.Message));
        }
    }

    private async Task AddScheduleNotificationAsync(
        Guid userId,
        string operatorName,
        string action,
        CancellationToken cancellationToken)
        => await notifications.AddAsync(
            Notification.Create(
                userId,
                $"Agendamento mensal da conta {operatorName} {action}.",
                NotificationType.Info,
                clock.Now),
            cancellationToken);

    private async Task<IReadOnlyList<AccountResponse>> MapAccountsAsync(IReadOnlyList<UserAccount> userAccounts, CancellationToken cancellationToken)
    {
        var operatorList = await operators.ListActiveAsync(cancellationToken);
        var operatorMap = operatorList.ToDictionary(item => item.Id);

        return userAccounts
            .Where(item => operatorMap.ContainsKey(item.OperatorId))
            .Select(item => item.ToResponse(operatorMap[item.OperatorId]))
            .ToList();
    }
}
