using System.Reflection;
using System.Text.Json;

namespace FishingAssistant.Configuration;

internal static class ConfigSchemaInspector
{
    private const int MaximumDisplayedValueLength = 120;

    private static readonly HashSet<string> KnownProperties = typeof(ModConfig)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Select(property => property.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> RetiredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "CatchTreasureButton",
        "JunkHighestPrice",
        nameof(ModConfig.FishDifficultyMultiplier),
        nameof(ModConfig.FishDifficultyAdditive)
    };

    public static bool IsLegacyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && !document.RootElement.TryGetProperty(nameof(ModConfig.ConfigVersion), out _);
    }

    public static IReadOnlyList<ConfigPropertySnapshot> FindUnknownProperties(string json)
    {
        return FindProperties(json, property =>
            !KnownProperties.Contains(property.Name) && !RetiredProperties.Contains(property.Name));
    }

    public static IReadOnlyList<ConfigPropertySnapshot> FindRetiredProperties(string json)
    {
        return FindProperties(json, property => RetiredProperties.Contains(property.Name));
    }

    private static IReadOnlyList<ConfigPropertySnapshot> FindProperties(
        string json,
        Func<JsonProperty, bool> predicate)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return [];

        return document.RootElement
            .EnumerateObject()
            .Where(predicate)
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(property => new ConfigPropertySnapshot(
                property.Name,
                DescribeValue(property.Name, property.Value)))
            .OrderBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string DescribeValue(string propertyName, JsonElement value)
    {
        if (IsSensitiveName(propertyName))
            return "[redacted]";

        string displayed = value.GetRawText();
        displayed = displayed.Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        return displayed.Length <= MaximumDisplayedValueLength
            ? displayed
            : $"{displayed[..MaximumDisplayedValueLength]}…";
    }

    private static bool IsSensitiveName(string propertyName)
    {
        return propertyName.Contains("password", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("token", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("apiKey", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("api_key", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record ConfigPropertySnapshot(string Name, string DisplayValue);
