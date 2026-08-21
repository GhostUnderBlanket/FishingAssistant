using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal sealed record InlineConfigMessage(string OptionKey, string TranslationKey);

internal static class InlineConfigValidation
{
    public static IReadOnlyList<InlineConfigMessage> Evaluate(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        List<InlineConfigMessage> messages = [];
        if (config.FishPreviewStyle == FishPreviewStyle.Sonar)
        {
            messages.Add(new InlineConfigMessage(
                "fish_name",
                "config.info.fish_name_classic_only"));
        }

        if (config.ActionIfInventoryFull == InventoryFullAction.Discard)
        {
            messages.Add(new InlineConfigMessage(
                "inventory_full_action",
                "config.warning.inventory_discard"));
        }

        if (config.ActionIfOnlyIgnoredTreasureRemains == IgnoredTreasureAction.Discard)
        {
            messages.Add(new InlineConfigMessage(
                "ignored_treasure_action",
                "config.warning.ignored_treasure_discard"));
        }

        if (config.JunkDisposalMode != JunkDisposalMode.Off)
        {
            messages.Add(new InlineConfigMessage(
                "junk_disposal",
                "config.warning.junk_disposal"));
        }

        if (config.AllowTrashFish)
        {
            messages.Add(new InlineConfigMessage(
                "trash_fish",
                "config.warning.trash_fish"));
        }

        if (config.AutoEatFood)
        {
            messages.Add(new InlineConfigMessage(
                "auto_eat",
                "config.warning.auto_eat"));
        }

        if (config.AllowEatingFish)
        {
            messages.Add(new InlineConfigMessage(
                "eat_fish",
                "config.warning.eat_fish"));
        }

        if (!string.Equals(config.StartWithFishingRod, ModConfig.DefaultStarterRod,
                StringComparison.OrdinalIgnoreCase))
        {
            messages.Add(new InlineConfigMessage(
                "starter_rod",
                "config.warning.starter_rod_free"));
        }

        if (config.SpawnBaitIfDontHave && !config.AutoAttachBait)
        {
            messages.Add(new InlineConfigMessage(
                "spawn_bait",
                "config.warning.spawn_bait_requires_attach"));
        }
        else if (config.SpawnBaitIfDontHave)
        {
            messages.Add(new InlineConfigMessage(
                "spawn_bait",
                "config.warning.spawn_bait_cheat"));
        }

        if (config.SpawnTackleIfDontHave && !config.AutoAttachTackles)
        {
            messages.Add(new InlineConfigMessage(
                "spawn_tackle",
                "config.warning.spawn_tackle_requires_attach"));
        }
        else if (config.SpawnTackleIfDontHave)
        {
            messages.Add(new InlineConfigMessage(
                "spawn_tackle",
                "config.warning.spawn_tackle_cheat"));
        }

        if (config.SkipFishingMiniGame != SkipMinigameBehavior.Off && config.AutoPlayMiniGame)
        {
            messages.Add(new InlineConfigMessage(
                "auto_minigame",
                "config.warning.auto_minigame_overridden"));
        }

        if (config.SkipFishingMiniGame != SkipMinigameBehavior.Off)
        {
            foreach (string optionKey in new[]
                     {
                         "minigame_assistance",
                         "fish_speed",
                         "progress_gain",
                         "progress_loss",
                         "treasure_speed",
                         "bar_size"
                     })
            {
                messages.Add(new InlineConfigMessage(
                    optionKey,
                    "config.info.minigame_assistance_skipped"));
            }
        }

        return messages;
    }
}
