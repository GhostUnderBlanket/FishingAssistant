using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal sealed record InlineConfigMessage(string OptionKey, string TranslationKey);

internal static class InlineConfigValidation
{
    public static IReadOnlyList<InlineConfigMessage> Evaluate(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        List<InlineConfigMessage> messages = [];
        if (config.SpawnBaitIfDontHave && !config.AutoAttachBait)
        {
            messages.Add(new InlineConfigMessage(
                "spawn_bait",
                "config.warning.spawn_bait_requires_attach"));
        }

        if (config.SpawnTackleIfDontHave && !config.AutoAttachTackles)
        {
            messages.Add(new InlineConfigMessage(
                "spawn_tackle",
                "config.warning.spawn_tackle_requires_attach"));
        }

        if (config.SkipFishingMiniGame != SkipMinigameBehavior.Off && config.AutoPlayMiniGame)
        {
            messages.Add(new InlineConfigMessage(
                "auto_minigame",
                "config.warning.auto_minigame_overridden"));
        }

        return messages;
    }
}
