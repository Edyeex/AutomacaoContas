using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class DemoOperatorAutomationStrategy : IOperatorAutomationStrategy
{
    public const string OperatorCode = "operador-demo";

    private static readonly HashSet<string> SupportedOperatorCodes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            OperatorCode,
            "ceee-equatorial",
            "corsan"
        };

    private readonly IDemoBillPdfGenerator pdfGenerator;

    public DemoOperatorAutomationStrategy(IDemoBillPdfGenerator pdfGenerator)
    {
        this.pdfGenerator = pdfGenerator;
    }

    public bool CanHandle(OperatorCompany operatorCompany)
        => operatorCompany.IsActive &&
           SupportedOperatorCodes.Contains(operatorCompany.Code);

    public async Task<AutomationDownloadResult> DownloadCurrentBillAsync(
        AutomationExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.Credential.Password))
        {
            return new AutomationDownloadResult(
                AutomationRunStatus.LoginFailed,
                "Credenciais do portal estao incompletas.",
                null);
        }

        var reference = BillReference.FromDate(context.ReferenceDate);
        var dueDate = BuildDueDate(context.Operator.ServiceType, context.ReferenceDate);
        var fileName = $"{context.Operator.Code}_{context.ReferenceDate:yyyy_MM}.pdf";
        var amount = EstimateAmount(context.Operator.ServiceType);
        var storagePath = await pdfGenerator.GenerateAsync(
            new DemoBillDocument(
                context.UserId,
                context.Account.Id,
                context.Operator.Name,
                reference,
                dueDate,
                amount,
                fileName),
            cancellationToken);

        return new AutomationDownloadResult(
            AutomationRunStatus.Success,
            $"Boleto de {reference} baixado com sucesso.",
            new BillDraft(
                reference,
                dueDate,
                amount,
                fileName,
                storagePath));
    }

    private static DateOnly BuildDueDate(ServiceType serviceType, DateOnly referenceDate)
    {
        var day = serviceType switch
        {
            ServiceType.Energy => 10,
            ServiceType.Water => 15,
            ServiceType.Internet => 20,
            _ => 25
        };

        return new DateOnly(referenceDate.Year, referenceDate.Month, day).AddMonths(1);
    }

    private static decimal EstimateAmount(ServiceType serviceType)
        => serviceType switch
        {
            ServiceType.Energy => 187.42m,
            ServiceType.Water => 94.18m,
            ServiceType.Internet => 129.90m,
            ServiceType.Phone => 79.90m,
            ServiceType.Gas => 66.50m,
            ServiceType.Tv => 119.90m,
            _ => 100m
        };
}
