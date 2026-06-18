namespace AutoDownload.Application.Abstractions;

public interface IMonthlyScheduleCalculator
{
    DateTimeOffset CalculateNext(
        DateTimeOffset now,
        DateTimeOffset? lastRunAt,
        int? dayOfMonth,
        TimeOnly scheduleTime);
}
