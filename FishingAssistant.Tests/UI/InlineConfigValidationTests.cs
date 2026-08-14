using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class InlineConfigValidationTests
{
    [Fact]
    public void Evaluate_ReturnsNoMessagesForSafeDefaults()
    {
        ModConfig config = new() { FishPreviewStyle = FishPreviewStyle.Classic };

        Assert.Empty(InlineConfigValidation.Evaluate(config));
    }

    [Fact]
    public void Evaluate_ExplainsThatFishNameIsClassicOnlyForSonarStyle()
    {
        ModConfig config = new() { FishPreviewStyle = FishPreviewStyle.Sonar };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("fish_name", message.OptionKey);
        Assert.Equal("config.info.fish_name_classic_only", message.TranslationKey);
    }

    [Theory]
    [InlineData((int)InventoryFullAction.Discard, "inventory_full_action", "config.warning.inventory_discard")]
    public void Evaluate_AttachesDestructiveInventoryActionMessage(
        int action,
        string optionKey,
        string translationKey)
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            ActionIfInventoryFull = (InventoryFullAction)action
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal(optionKey, message.OptionKey);
        Assert.Equal(translationKey, message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesDestructiveIgnoredTreasureMessage()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            ActionIfOnlyIgnoredTreasureRemains = IgnoredTreasureAction.Discard
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("ignored_treasure_action", message.OptionKey);
        Assert.Equal("config.warning.ignored_treasure_discard", message.TranslationKey);
    }

    [Theory]
    [InlineData("auto_trash", "config.warning.auto_trash")]
    [InlineData("trash_fish", "config.warning.trash_fish")]
    [InlineData("auto_eat", "config.warning.auto_eat")]
    [InlineData("eat_fish", "config.warning.eat_fish")]
    public void Evaluate_AttachesOptInConsumptionMessage(string optionKey, string translationKey)
    {
        ModConfig config = new() { FishPreviewStyle = FishPreviewStyle.Classic };
        switch (optionKey)
        {
            case "auto_trash":
                config.AutoTrashJunk = true;
                break;
            case "trash_fish":
                config.AllowTrashFish = true;
                break;
            case "auto_eat":
                config.AutoEatFood = true;
                break;
            case "eat_fish":
                config.AllowEatingFish = true;
                break;
        }

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal(optionKey, message.OptionKey);
        Assert.Equal(translationKey, message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesFreeItemMessageToSelectedStarterRod()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            StartWithFishingRod = "(T)IridiumRod"
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("starter_rod", message.OptionKey);
        Assert.Equal("config.warning.starter_rod_free", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesBaitDependencyMessageToSpawnOption()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            AutoAttachBait = false,
            SpawnBaitIfDontHave = true
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("spawn_bait", message.OptionKey);
        Assert.Equal("config.warning.spawn_bait_requires_attach", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesBaitCheatMessageWhenDependencyIsEnabled()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            AutoAttachBait = true,
            SpawnBaitIfDontHave = true
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("spawn_bait", message.OptionKey);
        Assert.Equal("config.warning.spawn_bait_cheat", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesTackleDependencyMessageToSpawnOption()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            AutoAttachTackles = false,
            SpawnTackleIfDontHave = true
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("spawn_tackle", message.OptionKey);
        Assert.Equal("config.warning.spawn_tackle_requires_attach", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesTackleCheatMessageWhenDependencyIsEnabled()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            AutoAttachTackles = true,
            SpawnTackleIfDontHave = true
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("spawn_tackle", message.OptionKey);
        Assert.Equal("config.warning.spawn_tackle_cheat", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesSkipConflictMessageToAutomaticMinigameOption()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            AutoPlayMiniGame = true,
            SkipFishingMiniGame = SkipMinigameBehavior.SkipAll
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("auto_minigame", message.OptionKey);
        Assert.Equal("config.warning.auto_minigame_overridden", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_ReturnsEveryActiveDependencyMessage()
    {
        ModConfig config = new()
        {
            FishPreviewStyle = FishPreviewStyle.Classic,
            AutoAttachBait = false,
            SpawnBaitIfDontHave = true,
            AutoAttachTackles = false,
            SpawnTackleIfDontHave = true,
            AutoPlayMiniGame = true,
            SkipFishingMiniGame = SkipMinigameBehavior.SkipOnlyCaught
        };

        IReadOnlyList<InlineConfigMessage> messages = InlineConfigValidation.Evaluate(config);

        Assert.Equal(3, messages.Count);
        Assert.Equal(["spawn_bait", "spawn_tackle", "auto_minigame"],
            messages.Select(message => message.OptionKey));
    }
}
