using AutoDownload.Application.Contracts;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Application.Mappings;

internal static class ResponseMapper
{
    public static UserResponse ToResponse(this AppUser user)
        => new(user.Id, user.Name, user.Email.Value, BuildInitials(user.Name), user.CreatedAt);

    public static OperatorResponse ToResponse(this OperatorCompany operatorCompany)
        => new(
            operatorCompany.Id,
            operatorCompany.Name,
            ToServiceTypeLabel(operatorCompany.ServiceType),
            ToServiceIcon(operatorCompany.ServiceType));

    public static AccountResponse ToResponse(this UserAccount account, OperatorCompany operatorCompany)
        => new(
            account.Id,
            operatorCompany.Id,
            operatorCompany.Name,
            ToServiceTypeLabel(operatorCompany.ServiceType),
            ToServiceIcon(operatorCompany.ServiceType),
            account.PortalLogin,
            account.CustomerIdentifier,
            ToAccountStatusLabel(account.Status),
            account.LastRunAt,
            account.NextRunAt);

    public static BillResponse ToResponse(this Bill bill, OperatorCompany operatorCompany)
        => new(
            bill.Id,
            bill.AccountId,
            operatorCompany.Name,
            ToServiceTypeLabel(operatorCompany.ServiceType),
            ToServiceIcon(operatorCompany.ServiceType),
            bill.Reference,
            bill.DueDate,
            bill.Amount,
            bill.FileName,
            bill.DownloadedAt,
            bill.Status == BillStatus.Available ? "disponivel" : "arquivado");

    public static HistoryResponse ToResponse(this AutomationRun run, OperatorCompany operatorCompany)
        => new(
            run.Id,
            run.AccountId,
            operatorCompany.Name,
            ToServiceTypeLabel(operatorCompany.ServiceType),
            run.StartedAt,
            ToAutomationStatusLabel(run.Status),
            run.Message,
            run.FileName,
            $"{run.DurationSeconds}s");

    public static NotificationResponse ToResponse(this Notification notification)
        => new(
            notification.Id,
            notification.Text,
            notification.CreatedAt,
            notification.IsRead,
            notification.Type.ToString().ToLowerInvariant());

    public static string ToAutomationStatusLabel(AutomationRunStatus status)
        => status switch
        {
            AutomationRunStatus.Success => "sucesso",
            AutomationRunStatus.BillUnavailable => "indisponivel",
            AutomationRunStatus.LoginFailed => "falha",
            AutomationRunStatus.ConnectionError => "falha",
            _ => "falha"
        };

    public static string ToServiceTypeLabel(ServiceType serviceType)
        => serviceType switch
        {
            ServiceType.Energy => "Energia",
            ServiceType.Water => "Água",
            ServiceType.Internet => "Internet",
            ServiceType.Phone => "Telefonia",
            ServiceType.Gas => "Gás",
            ServiceType.Tv => "TV",
            _ => "Outro"
        };

    public static string ToServiceIcon(ServiceType serviceType)
        => serviceType switch
        {
            ServiceType.Energy => "⚡",
            ServiceType.Water => "💧",
            ServiceType.Internet => "📡",
            ServiceType.Phone => "☎",
            ServiceType.Gas => "🔥",
            ServiceType.Tv => "📺",
            _ => "📄"
        };

    private static string ToAccountStatusLabel(AccountStatus status)
        => status switch
        {
            AccountStatus.Active => "ativa",
            AccountStatus.Inactive => "inativa",
            AccountStatus.Blocked => "bloqueada",
            _ => "inativa"
        };

    private static string BuildInitials(string name)
    {
        var parts = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]));

        return string.Concat(parts);
    }
}
