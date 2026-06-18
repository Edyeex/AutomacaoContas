using AutoDownload.Application.Common;
using AutoDownload.Application.Services;

namespace AutoDownload.Tests;

public sealed class BillServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsOnlyBillsWithActiveKnownOperator()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var knownOperator = TestData.Operator();
        var unknownOperatorId = Guid.NewGuid();
        var bills = new FakeBillRepository();
        bills.Items.Add(TestData.Bill(userId, accountId, knownOperator.Id));
        bills.Items.Add(TestData.Bill(userId, accountId, unknownOperatorId));
        var operators = new FakeOperatorRepository();
        operators.Items.Add(knownOperator);
        var service = new BillService(bills, operators);

        var result = await service.ListAsync(userId);

        Assert.True(result.IsSuccess);
        var response = Assert.Single(result.Value);
        Assert.Equal(knownOperator.Name, response.Operadora);
        Assert.Equal(129.90m, response.Valor);
    }

    [Fact]
    public async Task GetFileAsync_ForAnotherUser_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var bill = TestData.Bill(ownerId, Guid.NewGuid(), Guid.NewGuid());
        var bills = new FakeBillRepository();
        bills.Items.Add(bill);
        var service = new BillService(bills, new FakeOperatorRepository());

        var result = await service.GetFileAsync(Guid.NewGuid(), bill.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
        Assert.Equal("bill.not_found", result.Error?.Code);
    }
}
