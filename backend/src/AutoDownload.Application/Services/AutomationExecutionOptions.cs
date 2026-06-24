namespace AutoDownload.Application.Services;

public sealed class AutomationExecutionOptions
{
    public int TimeoutSeconds { get; init; } = 180;

    public TimeSpan Timeout
        => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 30, 600));
}
