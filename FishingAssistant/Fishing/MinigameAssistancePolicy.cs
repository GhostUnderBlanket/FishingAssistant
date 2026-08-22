using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal sealed record MinigameAssistanceValues(
    int FishSpeedPercent,
    int ProgressGainPercent,
    int ProgressLossPercent,
    int TreasureSpeedPercent,
    int BarSizePercent);

internal static class MinigameAssistancePolicy
{
    public const int FishSpeedMinimum = 0;
    public const int FishSpeedMaximum = 200;
    public const int ProgressGainMinimum = 25;
    public const int ProgressGainMaximum = 300;
    public const int ProgressLossMinimum = 0;
    public const int ProgressLossMaximum = 200;
    public const int TreasureSpeedMinimum = 25;
    public const int TreasureSpeedMaximum = 300;
    public const int BarSizeMinimum = 50;
    public const int BarSizeMaximum = 200;
    public const int VanillaPercent = 100;
    public const int MinimumBarHeight = 16;
    public const int MaximumBarHeight = 568;

    public static float ScaleDelta(float vanillaDelta, int percent)
    {
        if (!float.IsFinite(vanillaDelta))
            return 0f;
        return vanillaDelta * percent / VanillaPercent;
    }

    public static float ScaleFishMovement(float vanillaDelta, int percent) => ScaleDelta(vanillaDelta, percent);

    public static float ScaleProgressGain(float vanillaGain, int percent) => ScaleDelta(vanillaGain, percent);

    public static float ScaleTreasureGain(float vanillaGain, int percent) => ScaleDelta(vanillaGain, percent);

    public static float ScaleProgressLossModifier(float vanillaModifier, int percent)
    {
        if (!float.IsFinite(vanillaModifier))
            return vanillaModifier;
        return vanillaModifier * percent / VanillaPercent;
    }

    public static int ScaleBarSize(int vanillaHeight, int percent)
    {
        if (vanillaHeight <= 0)
            return MinimumBarHeight;
        double scaled = vanillaHeight * (double)percent / VanillaPercent;
        return Math.Clamp((int)Math.Round(scaled, MidpointRounding.AwayFromZero),
            MinimumBarHeight, MaximumBarHeight);
    }

    public static MinigameAssistanceValues Read(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new MinigameAssistanceValues(
            config.FishSpeedPercent,
            config.ProgressGainPercent,
            config.ProgressLossPercent,
            config.TreasureSpeedPercent,
            config.BarSizePercent);
    }
}
