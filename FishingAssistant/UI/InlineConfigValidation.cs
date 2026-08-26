using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal sealed record InlineConfigMessage(
    string OptionKey,
    string TranslationKey,
    object?[]? FormatArguments = null);

internal static class InlineConfigValidation
{
    public static IReadOnlyList<InlineConfigMessage> Evaluate(
        ModConfig config,
        IEnumerable<InlineConfigMessage>? contextualMessages = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        List<InlineConfigMessage> messages = [];
        if (contextualMessages is not null)
            messages.AddRange(contextualMessages);

        messages.Add(new InlineConfigMessage(
            "automation_profile",
            "config.info.automation_profile"));

        if (config.AutomaticBubbleSteering
            && config.AutomaticCastPowerAdjustmentMode != CastPowerAdjustmentMode.Off)
        {
            messages.Add(new InlineConfigMessage(
                "automatic_cast_power_adjustment",
                "config.info.cast_power_bubble_only"));
        }

        if (config.ShowFishingBubbleMarker)
        {
            messages.Add(new InlineConfigMessage(
                "bubble_marker",
                "config.info.bubble_marker_colors"));
        }

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

        if (config.JunkDisposalMode == JunkDisposalMode.Immediately)
        {
            messages.Add(new InlineConfigMessage(
                "junk_disposal",
                "config.warning.junk_disposal"));
        }
        else if (config.JunkDisposalMode == JunkDisposalMode.WhenInventoryFull)
        {
            messages.Add(new InlineConfigMessage(
                "junk_disposal",
                "config.info.junk_disposal_full"));
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

        if (config.OpenInventoryOnStop)
        {
            messages.Add(new InlineConfigMessage(
                "open_inventory_on_stop",
                "config.info.single_player_only"));
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
