using FishingAssistant.Configuration;

namespace FishingAssistant.Fishing;

internal sealed record FishPreviewStyleConditions(
    FishPreviewStyle ConfiguredStyle,
    bool CanSuppressVanillaPreview,
    bool HasEquippedSonarBobber);

internal sealed record FishPreviewStyleDecision(
    FishPreviewStyle EffectiveStyle,
    bool ShouldDrawModPreview,
    bool UsedCompatibilityFallback);

internal static class FishPreviewStylePolicy
{
    public static bool ShouldReserveChallengeBaitSpace(
        bool hasVanillaSonarBobber,
        bool fishPreviewEnabled,
        FishPreviewStyle previewStyle)
    {
        return hasVanillaSonarBobber
            || (fishPreviewEnabled && previewStyle == FishPreviewStyle.Sonar);
    }

    public static FishPreviewStyleDecision Decide(FishPreviewStyleConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (conditions.CanSuppressVanillaPreview)
            return new(conditions.ConfiguredStyle, true, false);

        if (conditions.HasEquippedSonarBobber)
            return new(FishPreviewStyle.Classic, false, true);

        return new(FishPreviewStyle.Classic, true,
            conditions.ConfiguredStyle != FishPreviewStyle.Classic);
    }
}
