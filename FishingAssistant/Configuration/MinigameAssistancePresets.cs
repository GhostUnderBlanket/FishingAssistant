using FishingAssistant.Fishing;

namespace FishingAssistant.Configuration;

internal static class MinigameAssistancePresets
{
    private static readonly IReadOnlyDictionary<MinigameAssistancePreset, MinigameAssistanceValues> Values =
        new Dictionary<MinigameAssistancePreset, MinigameAssistanceValues>
        {
            [MinigameAssistancePreset.Off] = new(100, 100, 100, 100, 100),
            [MinigameAssistancePreset.Light] = new(85, 115, 80, 125, 110),
            [MinigameAssistancePreset.Comfortable] = new(70, 135, 60, 160, 125),
            [MinigameAssistancePreset.Relaxed] = new(50, 175, 25, 225, 150)
        };

    public static MinigameAssistanceValues Get(MinigameAssistancePreset preset)
    {
        return Values.TryGetValue(preset, out MinigameAssistanceValues? values)
            ? values
            : Values[MinigameAssistancePreset.Off];
    }

    public static void Apply(ModConfig config, MinigameAssistancePreset preset)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (preset == MinigameAssistancePreset.Custom)
        {
            config.MinigameAssistance = Detect(config);
            return;
        }

        MinigameAssistanceValues values = Get(preset);
        config.FishSpeedPercent = values.FishSpeedPercent;
        config.ProgressGainPercent = values.ProgressGainPercent;
        config.ProgressLossPercent = values.ProgressLossPercent;
        config.TreasureSpeedPercent = values.TreasureSpeedPercent;
        config.BarSizePercent = values.BarSizePercent;
        config.MinigameAssistance = preset;
    }

    public static MinigameAssistancePreset Detect(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        MinigameAssistanceValues current = MinigameAssistancePolicy.Read(config);
        foreach ((MinigameAssistancePreset preset, MinigameAssistanceValues values) in Values)
        {
            if (current == values)
                return preset;
        }

        return MinigameAssistancePreset.Custom;
    }

    public static void DetectAndSet(ModConfig config)
    {
        config.MinigameAssistance = Detect(config);
    }
}
