namespace AutoDownload.Infrastructure.Security;

public sealed class AccessTokenOptions
{
    public string Issuer { get; set; } = "AutoDownload";

    public string Audience { get; set; } = "AutoDownload.Frontend";

    public string SigningKey { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 120;
}
