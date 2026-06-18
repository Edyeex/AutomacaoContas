using AutoDownload.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace AutoDownload.Infrastructure.Automation;

internal sealed class MonthlyScheduleCalculator : IMonthlyScheduleCalculator
{
    private readonly TimeZoneInfo timeZone;

    public MonthlyScheduleCalculator(IOptions<MonthlyScheduleOptions> options)
    {
        timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZoneId);
    }

    public DateTimeOffset CalculateNext(
        DateTimeOffset now,
        DateTimeOffset? lastRunAt,
        int? dayOfMonth,
        TimeOnly scheduleTime)
    {
        if (dayOfMonth is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), "Schedule day must be between 1 and 31.");
        }

        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var reference = lastRunAt.HasValue
            ? TimeZoneInfo.ConvertTime(lastRunAt.Value, timeZone).AddMonths(1)
            : localNow;

        var year = reference.Year;
        var month = reference.Month;

        while (true)
        {
            var day = dayOfMonth.HasValue
                ? Math.Min(dayOfMonth.Value, DateTime.DaysInMonth(year, month))
                : DateTime.DaysInMonth(year, month);
            var localCandidate = new DateTime(
                year,
                month,
                day,
                scheduleTime.Hour,
                scheduleTime.Minute,
                scheduleTime.Second,
                DateTimeKind.Unspecified);
            var candidate = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localCandidate, timeZone));

            if (candidate > now)
            {
                return candidate;
            }

            var nextMonth = new DateTime(year, month, 1).AddMonths(1);
            year = nextMonth.Year;
            month = nextMonth.Month;
        }
    }
}
