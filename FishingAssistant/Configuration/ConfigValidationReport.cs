namespace FishingAssistant.Configuration;

internal sealed record ConfigCorrection(
    string Property,
    string OriginalValue,
    string CorrectedValue,
    string Reason
);

internal sealed record ConfigWarning(string Property, string Value, string Reason);

internal sealed class ConfigValidationReport
{
    private readonly List<ConfigCorrection> corrections = [];
    private readonly List<ConfigWarning> warnings = [];

    public IReadOnlyList<ConfigCorrection> Corrections => this.corrections;

    public IReadOnlyList<ConfigWarning> Warnings => this.warnings;

    public bool WasChanged => this.corrections.Count > 0;

    internal void Add(string property, object? original, object? corrected, string reason)
    {
        this.corrections.Add(new ConfigCorrection(
            property,
            original?.ToString() ?? "null",
            corrected?.ToString() ?? "null",
            reason
        ));
    }

    internal void Warn(string property, object? value, string reason)
    {
        this.warnings.Add(new ConfigWarning(property, value?.ToString() ?? "null", reason));
    }

    internal void Append(ConfigValidationReport report)
    {
        this.corrections.AddRange(report.Corrections);
        this.warnings.AddRange(report.Warnings);
    }
}
