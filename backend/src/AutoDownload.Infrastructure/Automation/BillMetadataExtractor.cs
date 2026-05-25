using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoDownload.Infrastructure.Automation;

internal static class BillMetadataExtractor
{
    private static readonly string[] AmountPropertyNames =
    [
        "finalAmount",
        "updatedAmount",
        "amount",
        "totalAmount",
        "paymentValue",
        "payableAmount",
        "valueToPay",
        "totalToPay",
        "invoiceAmount",
        "originalAmount",
        "value",
        "total",
        "valor",
        "valorFinal"
    ];

    private static readonly string[] DueDatePropertyNames =
    [
        "dueDate",
        "due_date",
        "expirationDate",
        "vencimento"
    ];

    public static decimal? TryParseAmountFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var labeledAmount = TryParseLabeledAmount(text);
        if (labeledAmount is not null)
        {
            return labeledAmount;
        }

        var amounts = Regex
            .Matches(text, @"R\$\s*(\d{1,3}(?:\.\d{3})*,\d{2}|\d+,\d{2})")
            .Select(match => ParseBrazilianDecimal(match.Groups[1].Value))
            .Where(amount => amount is > 0m)
            .Select(amount => amount!.Value)
            .ToList();

        return amounts.Count == 0 ? null : amounts.Max();
    }

    public static DateOnly? TryParseDueDateFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = Regex.Match(text, @"\b(\d{2})/(\d{2})/(\d{4})\b");
        if (!match.Success)
        {
            return null;
        }

        return DateOnly.TryParseExact(
            match.Value,
            "dd/MM/yyyy",
            CultureInfo.GetCultureInfo("pt-BR"),
            DateTimeStyles.None,
            out var dueDate)
            ? dueDate
            : null;
    }

    public static decimal? TryGetAmountFromJson(JsonElement element)
    {
        foreach (var propertyName in AmountPropertyNames)
        {
            var amount = GetJsonPropertyAsDecimal(element, propertyName);
            if (amount is not null)
            {
                return amount;
            }
        }

        return null;
    }

    public static DateOnly? TryGetDueDateFromJson(JsonElement element)
    {
        foreach (var propertyName in DueDatePropertyNames)
        {
            var dueDate = GetJsonPropertyAsDate(element, propertyName);
            if (dueDate is not null)
            {
                return dueDate;
            }
        }

        return null;
    }

    public static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static decimal? TryParseLabeledAmount(string text)
    {
        foreach (var label in new[]
                 {
                     "total a pagar",
                     "valor total",
                     "valor final",
                     "total",
                     "valor",
                     "fatura"
                 })
        {
            var pattern = $"{Regex.Escape(label)}\\s*[:\\-]?\\s*R\\$\\s*(\\d{{1,3}}(?:\\.\\d{{3}})*,\\d{{2}}|\\d+,\\d{{2}})";
            var match = Regex.Match(Normalize(text), pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                continue;
            }

            var amount = ParseBrazilianDecimal(match.Groups[1].Value);
            if (amount is > 0m)
            {
                return amount;
            }
        }

        return null;
    }

    private static decimal? GetJsonPropertyAsDecimal(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number &&
            value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return ParseFlexibleDecimal(value.GetString());
    }

    private static DateOnly? GetJsonPropertyAsDate(JsonElement element, string propertyName)
    {
        var value = GetJsonPropertyAsString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
        {
            return dateOnly;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTimeOffset))
        {
            return DateOnly.FromDateTime(dateTimeOffset.LocalDateTime);
        }

        return null;
    }

    private static string? GetJsonPropertyAsString(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static decimal? ParseFlexibleDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantAmount)
            ? invariantAmount
            : ParseBrazilianDecimal(value);
    }

    private static decimal? ParseBrazilianDecimal(string value)
        => decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.GetCultureInfo("pt-BR"),
            out var amount)
            ? amount
            : null;

    private static string Normalize(string value)
    {
        var formD = value.Normalize(NormalizationForm.FormD);
        var chars = formD
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
