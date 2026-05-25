using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;
using AutoDownload.Domain.Enums;

namespace AutoDownload.Application.Services;

public sealed class HistoryService
{
    private readonly IAutomationRunRepository runs;
    private readonly IOperatorRepository operators;

    public HistoryService(IAutomationRunRepository runs, IOperatorRepository operators)
    {
        this.runs = runs;
        this.operators = operators;
    }

    public async Task<Result<IReadOnlyList<HistoryResponse>>> ListAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
    {
        var allRuns = await runs.ListByUserAsync(userId, null, cancellationToken);
        var filtered = (status ?? "todos").ToLowerInvariant() switch
        {
            "sucesso" => allRuns.Where(item => item.Status == AutomationRunStatus.Success),
            "indisponivel" => allRuns.Where(item => item.Status == AutomationRunStatus.BillUnavailable),
            "falha" => allRuns.Where(item => item.Status is not AutomationRunStatus.Success and not AutomationRunStatus.BillUnavailable),
            _ => allRuns
        };

        var operatorMap = (await operators.ListActiveAsync(cancellationToken)).ToDictionary(item => item.Id);
        var response = filtered
            .Where(item => operatorMap.ContainsKey(item.OperatorId))
            .Select(item => item.ToResponse(operatorMap[item.OperatorId]))
            .ToList();

        return Result<IReadOnlyList<HistoryResponse>>.Success(response);
    }
}
