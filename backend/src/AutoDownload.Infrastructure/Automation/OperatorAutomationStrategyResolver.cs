using AutoDownload.Application.Abstractions;
using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Common;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class OperatorAutomationStrategyResolver : IOperatorAutomationStrategyResolver
{
    private readonly IEnumerable<IOperatorAutomationStrategy> strategies;

    public OperatorAutomationStrategyResolver(IEnumerable<IOperatorAutomationStrategy> strategies)
    {
        this.strategies = strategies;
    }

    public IOperatorAutomationStrategy Resolve(OperatorCompany operatorCompany)
        => strategies.FirstOrDefault(strategy => strategy.CanHandle(operatorCompany))
            ?? throw new DomainException($"No automation strategy configured for operator {operatorCompany.Code}.");
}
