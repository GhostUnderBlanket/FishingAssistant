namespace FishingAssistant.Configuration;

internal sealed record AutomationTimingValues(
    float RecastDelaySeconds,
    float CatchPopupDurationSeconds,
    float TreasureLootDelaySeconds,
    float FoodConsumptionDelaySeconds);

internal static class AutomationTimingPresets
{
    private static readonly IReadOnlyDictionary<AutomationTimingPreset, AutomationTimingValues> Values =
        new Dictionary<AutomationTimingPreset, AutomationTimingValues>
        {
            [AutomationTimingPreset.Slow] = new(2f, 3f, 1.5f, 2f),
            [AutomationTimingPreset.Normal] = new(1f, 1.5f, 0.5f, 1f),
            [AutomationTimingPreset.Fast] = new(0.5f, 0.75f, 0.25f, 0.5f)
        };

    public static void Apply(ModConfig config, AutomationTimingPreset preset)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (preset == AutomationTimingPreset.Custom)
        {
            DetectAndSet(config);
            return;
        }

        AutomationTimingValues values = Values[preset];
        config.AutoCastDelaySeconds = values.RecastDelaySeconds;
        config.CatchPopupDurationSeconds = values.CatchPopupDurationSeconds;
        config.TreasureLootDelaySeconds = values.TreasureLootDelaySeconds;
        config.FoodConsumptionDelaySeconds = values.FoodConsumptionDelaySeconds;
        config.AutomationTiming = preset;
    }

    public static AutomationTimingPreset Detect(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        foreach ((AutomationTimingPreset preset, AutomationTimingValues values) in Values)
        {
            if (NearlyEqual(config.AutoCastDelaySeconds, values.RecastDelaySeconds)
                && NearlyEqual(config.CatchPopupDurationSeconds, values.CatchPopupDurationSeconds)
                && NearlyEqual(config.TreasureLootDelaySeconds, values.TreasureLootDelaySeconds)
                && NearlyEqual(config.FoodConsumptionDelaySeconds, values.FoodConsumptionDelaySeconds))
            {
                return preset;
            }
        }

        return AutomationTimingPreset.Custom;
    }

    public static void DetectAndSet(ModConfig config)
    {
        config.AutomationTiming = Detect(config);
    }

    private static bool NearlyEqual(float left, float right)
    {
        return Math.Abs(left - right) < 0.001f;
    }
}
