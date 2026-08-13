using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal sealed record ConfigApplyFeedback(
    int CorrectionCount,
    int WarningCount,
    IReadOnlyList<string> AffectedProperties,
    int AdditionalPropertyCount)
{
    internal const int DefaultPropertyLimit = 5;

    public bool HasMessages => this.CorrectionCount > 0 || this.WarningCount > 0;

    public static ConfigApplyFeedback Create(
        ConfigValidationReport report,
        int propertyLimit = DefaultPropertyLimit)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (propertyLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(propertyLimit));

        string[] properties = report.Corrections
            .Select(correction => correction.Property)
            .Concat(report.Warnings.Select(warning => warning.Property))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new ConfigApplyFeedback(
            report.Corrections.Count,
            report.Warnings.Count,
            properties.Take(propertyLimit).ToArray(),
            Math.Max(0, properties.Length - propertyLimit));
    }
}
