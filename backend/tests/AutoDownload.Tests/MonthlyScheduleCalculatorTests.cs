using AutoDownload.Infrastructure.Automation;
using Microsoft.Extensions.Options;

namespace AutoDownload.Tests;

public sealed class MonthlyScheduleCalculatorTests
{
    private readonly MonthlyScheduleCalculator calculator = new(
        Options.Create(new MonthlyScheduleOptions
        {
            TimeZoneId = "America/Sao_Paulo"
        }));

    [Fact]
    public void CalculateNext_AfterRun_SchedulesTheFollowingMonth()
    {
        var lastRun = new DateTimeOffset(2026, 6, 18, 15, 0, 0, TimeSpan.Zero);

        var result = calculator.CalculateNext(lastRun, lastRun, 31, new TimeOnly(9, 0));

        Assert.Equal(new DateTimeOffset(2026, 7, 31, 12, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void CalculateNext_Day31_ClampsToLastDayOfShortMonth()
    {
        var now = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
        var lastRun = new DateTimeOffset(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

        var result = calculator.CalculateNext(now, lastRun, 31, new TimeOnly(9, 0));

        Assert.Equal(new DateTimeOffset(2026, 2, 28, 12, 0, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void CalculateNext_LastDay_HandlesLeapYear()
    {
        var now = new DateTimeOffset(2028, 1, 20, 12, 0, 0, TimeSpan.Zero);

        var result = calculator.CalculateNext(now, now, null, new TimeOnly(18, 30));

        Assert.Equal(new DateTimeOffset(2028, 2, 29, 21, 30, 0, TimeSpan.Zero), result);
    }

    [Fact]
    public void CalculateNext_WithoutPreviousRun_UsesUpcomingDayInCurrentMonth()
    {
        var now = new DateTimeOffset(2026, 6, 18, 15, 0, 0, TimeSpan.Zero);

        var result = calculator.CalculateNext(now, null, 20, new TimeOnly(8, 0));

        Assert.Equal(new DateTimeOffset(2026, 6, 20, 11, 0, 0, TimeSpan.Zero), result);
    }
}
