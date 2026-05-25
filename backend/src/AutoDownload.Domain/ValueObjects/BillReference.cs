using System.Globalization;

namespace AutoDownload.Domain.ValueObjects;

public static class BillReference
{
    private static readonly CultureInfo PortugueseBrazil = CultureInfo.GetCultureInfo("pt-BR");

    public static string FromDate(DateOnly date)
    {
        var month = PortugueseBrazil.DateTimeFormat.GetMonthName(date.Month);
        return $"{char.ToUpperInvariant(month[0])}{month[1..]} {date.Year}";
    }
}
