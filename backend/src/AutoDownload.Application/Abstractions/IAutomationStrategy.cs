using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Application.Abstractions;

public sealed record PortalCredential(string Login, string Password, string CustomerIdentifier);

public sealed record AutomationExecutionContext(
    Guid UserId,
    UserAccount Account,
    OperatorCompany Operator,
    PortalCredential Credential,
    DateOnly ReferenceDate);

public sealed record BillDraft(
    string Reference,
    DateOnly DueDate,
    decimal Amount,
    string FileName,
    string StoragePath);

public sealed record AutomationDownloadResult(
    AutomationRunStatus Status,
    string Message,
    BillDraft? Bill)
{
    public bool Succeeded => Status == AutomationRunStatus.Success && Bill is not null;
}

public interface IOperatorAutomationStrategy
{
    bool CanHandle(OperatorCompany operatorCompany);

    Task<AutomationDownloadResult> DownloadCurrentBillAsync(
        AutomationExecutionContext context,
        CancellationToken cancellationToken = default);
}

public interface IOperatorAutomationStrategyResolver
{
    IOperatorAutomationStrategy Resolve(OperatorCompany operatorCompany);
}
