using System.Text.RegularExpressions;
using AutoDownload.Domain.Common;

namespace AutoDownload.Domain.ValueObjects;

public sealed partial record EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();

        if (normalized.Length is < 5 or > 254 || !EmailRegex().IsMatch(normalized))
        {
            throw new DomainException("Invalid email address.");
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
