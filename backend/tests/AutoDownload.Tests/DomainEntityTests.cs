using AutoDownload.Domain.Entities;
using AutoDownload.Domain.Enums;
using AutoDownload.Domain.ValueObjects;

namespace AutoDownload.Tests;

public sealed class DomainEntityTests
{
    [Fact]
    public void Register_NormalizesEmail()
    {
        var user = AppUser.Register(
            "Maria Silva",
            EmailAddress.Create("MARIA.SILVA@example.com"),
            "hashed-password",
            TestData.Now);

        Assert.Equal("maria.silva@example.com", user.Email.Value);
    }

    [Fact]
    public void RefreshDownload_RoundsAmountAndUpdatesMetadata()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var bill = Bill.Create(
            userId,
            accountId,
            operatorId,
            BillReference.FromDate(new DateOnly(2026, 5, 19)),
            new DateOnly(2026, 6, 10),
            123.456m,
            "boleto.pdf",
            "C:/storage/boleto.pdf",
            TestData.Now);

        bill.RefreshDownload(
            new DateOnly(2026, 6, 12),
            98.765m,
            "boleto-atualizado.pdf",
            "C:/storage/boleto-atualizado.pdf",
            TestData.Now.AddMinutes(5));

        Assert.Equal(98.77m, bill.Amount);
        Assert.Equal("boleto-atualizado.pdf", bill.FileName);
    }

    [Fact]
    public void AutomationRun_CalculatesDurationInSeconds()
    {
        var run = AutomationRun.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TestData.Now,
            TestData.Now.AddSeconds(13),
            AutomationRunStatus.Success,
            "Boleto baixado com sucesso.",
            "boleto.pdf");

        Assert.Equal(13, run.DurationSeconds);
    }
}
