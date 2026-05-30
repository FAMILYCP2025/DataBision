namespace DataBision.Shared.Watermarks;

/// <summary>
/// Normalises a SAP B1 UpdateTS / CreateTS value (which can arrive as int or string)
/// into a stable CHAR(6) HHMMSS representation. Authoritative implementation —
/// agents may pre-compute but the Ingest API always recomputes.
/// </summary>
public static class TsNormalizer
{
    /// <summary>
    /// Normalises <paramref name="rawTs"/> to a 6-digit HHMMSS string.
    /// Returns null when input is null/empty/no digits.
    /// </summary>
    public static string? Normalize(object? rawTs)
    {
        if (rawTs is null) return null;

        var asString = Convert.ToString(rawTs, System.Globalization.CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(asString)) return null;

        // SAP can return "HMMSS", "HHMMSS", "12:34:56", "123456.0", or int "123456" — strip non-digits.
        var digits = new string(asString.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        // Pad or truncate to exactly 6 digits.
        if (digits.Length < 6) digits = digits.PadLeft(6, '0');
        else if (digits.Length > 6) digits = digits[..6];

        return digits;
    }
}
