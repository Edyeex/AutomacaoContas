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
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public AccountService(
        IUserAccountRepository accounts,
        IOperatorRepository operators,
        INotificationRepository notifications,
        ICredentialProtector credentialProtector,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.accounts = accounts;
        this.operators = operators;
        this.notifications = notifications;
        this.credentialProtector = credentialProtector;
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
                clock.Now,
                clock.Now.AddDays(5));

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
