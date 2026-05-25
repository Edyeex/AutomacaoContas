using AutoDownload.Application.Abstractions;

namespace AutoDownload.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(Now.UtcDateTime);
}
