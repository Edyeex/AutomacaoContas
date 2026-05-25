namespace AutoDownload.Application.Abstractions;

public interface IClock
{
    DateTimeOffset Now { get; }

    DateOnly Today { get; }
}
