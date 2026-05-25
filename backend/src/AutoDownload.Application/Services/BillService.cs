using AutoDownload.Application.Abstractions;
using AutoDownload.Application.Common;
using AutoDownload.Application.Contracts;
using AutoDownload.Application.Mappings;

namespace AutoDownload.Application.Services;

public sealed class BillService
{
    private readonly IBillRepository bills;
    private readonly IOperatorRepository operators;

    public BillService(IBillRepository bills, IOperatorRepository operators)
    {
        this.bills = bills;
        this.operators = operators;
    }

    public async Task<Result<IReadOnlyList<BillResponse>>> ListAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userBills = await bills.ListByUserAsync(userId, cancellationToken);
        var operatorMap = (await operators.ListActiveAsync(cancellationToken)).ToDictionary(item => item.Id);

        var response = userBills
            .Where(item => operatorMap.ContainsKey(item.OperatorId))
            .Select(item => item.ToResponse(operatorMap[item.OperatorId]))
            .ToList();

        return Result<IReadOnlyList<BillResponse>>.Success(response);
    }

    public async Task<Result<BillFileResponse>> GetFileAsync(Guid userId, Guid billId, CancellationToken cancellationToken = default)
    {
        var bill = await bills.FindByIdForUserAsync(userId, billId, cancellationToken);
        if (bill is null)
        {
            return Result<BillFileResponse>.Failure(Error.NotFound("bill.not_found", "Bill not found."));
        }

        return Result<BillFileResponse>.Success(new BillFileResponse(bill.Id, bill.FileName, bill.StoragePath));
    }
}
