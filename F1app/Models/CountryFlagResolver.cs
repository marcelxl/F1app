namespace F1app.Models;

public static class CountryFlagResolver
{
    private static readonly Dictionary<string, string> Flags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["usa"] = "🇺🇸",
        ["united states"] = "🇺🇸",
        ["united states of america"] = "🇺🇸",
        ["uae"] = "🇦🇪",
        ["united arab emirates"] = "🇦🇪",
        ["abu dhabi"] = "🇦🇪",
        ["uk"] = "🇬🇧",
        ["great britain"] = "🇬🇧",
        ["united kingdom"] = "🇬🇧",
        ["saudi arabia"] = "🇸🇦",
        ["azerbaijan"] = "🇦🇿",
        ["bahrain"] = "🇧🇭",
        ["qatar"] = "🇶🇦",
        ["monaco"] = "🇲🇨",
        ["italy"] = "🇮🇹",
        ["italia"] = "🇮🇹",
        ["spain"] = "🇪🇸",
        ["canada"] = "🇨🇦",
        ["austria"] = "🇦🇹",
        ["hungary"] = "🇭🇺",
        ["belgium"] = "🇧🇪",
        ["netherlands"] = "🇳🇱",
        ["dutch"] = "🇳🇱",
        ["singapore"] = "🇸🇬",
        ["japan"] = "🇯🇵",
        ["mexico"] = "🇲🇽",
        ["brazil"] = "🇧🇷",
        ["australia"] = "🇦🇺",
        ["china"] = "🇨🇳",
        ["malaysia"] = "🇲🇾",
        ["france"] = "🇫🇷",
        ["germany"] = "🇩🇪",
        ["portugal"] = "🇵🇹"
    };

    public static string Resolve(string? country, string? circuitName, string? raceName)
    {
        if (TryResolve(country, out var countryFlag))
        {
            return countryFlag;
        }

        var fallbackText = string.Join(
            " ",
            circuitName ?? string.Empty,
            raceName ?? string.Empty).Trim();
        if (fallbackText.Contains("las vegas", StringComparison.OrdinalIgnoreCase) ||
            fallbackText.Contains("miami", StringComparison.OrdinalIgnoreCase) ||
            fallbackText.Contains("austin", StringComparison.OrdinalIgnoreCase))
        {
            return TryGetFlag("usa");
        }

        if (fallbackText.Contains("abu dhabi", StringComparison.OrdinalIgnoreCase))
        {
            return TryGetFlag("uae");
        }

        return "🏁";
    }

    private static bool TryResolve(string? country, out string flag)
    {
        flag = string.Empty;
        if (string.IsNullOrWhiteSpace(country))
        {
            return false;
        }

        var normalizedCountry = country.Trim().ToLowerInvariant();
        if (Flags.TryGetValue(normalizedCountry, out var resolvedFlag) &&
            !string.IsNullOrWhiteSpace(resolvedFlag))
        {
            flag = resolvedFlag;
            return true;
        }

        return false;
    }

    private static string TryGetFlag(string key)
    {
        return Flags.TryGetValue(key, out var flag) && !string.IsNullOrWhiteSpace(flag)
            ? flag
            : "🏁";
    }
}