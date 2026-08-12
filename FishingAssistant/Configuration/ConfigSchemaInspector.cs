using System.Reflection;
using System.Text.Json;

namespace FishingAssistant.Configuration;

internal static class ConfigSchemaInspector
{
    private static readonly HashSet<string> KnownProperties = typeof(ModConfig)
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Select(property => property.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsLegacyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && !document.RootElement.TryGetProperty(nameof(ModConfig.ConfigVersion), out _);
    }

    public static IReadOnlyList<string> FindUnknownProperties(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return [];

        return document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Where(property => !KnownProperties.Contains(property))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(property => property, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
