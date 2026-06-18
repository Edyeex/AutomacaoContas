namespace AutoDownload.Infrastructure.Automation;

internal sealed class MonthlyScheduleOptions
{
    public string TimeZoneId { get; init; } = "America/Sao_Paulo";

    public int IntervalSeconds { get; init; } = 60;
}
