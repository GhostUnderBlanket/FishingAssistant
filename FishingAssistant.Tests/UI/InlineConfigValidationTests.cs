using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class InlineConfigValidationTests
{
    [Fact]
    public void Evaluate_ReturnsNoMessagesForCompatibleSettings()
    {
        ModConfig config = new()
        {
            AutoAttachBait = true,
            SpawnBaitIfDontHave = true,
            AutoAttachTackles = true,
            SpawnTackleIfDontHave = true,
            AutoPlayMiniGame = true,
            SkipFishingMiniGame = SkipMinigameBehavior.Off
        };

        Assert.Empty(InlineConfigValidation.Evaluate(config));
    }

    [Fact]
    public void Evaluate_AttachesBaitDependencyMessageToSpawnOption()
    {
        ModConfig config = new()
        {
            AutoAttachBait = false,
            SpawnBaitIfDontHave = true
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("spawn_bait", message.OptionKey);
        Assert.Equal("config.warning.spawn_bait_requires_attach", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesTackleDependencyMessageToSpawnOption()
    {
        ModConfig config = new()
        {
            AutoAttachTackles = false,
            SpawnTackleIfDontHave = true
        };

        InlineConfigMessage message = Assert.Single(InlineConfigValidation.Evaluate(config));

        Assert.Equal("spawn_tackle", message.OptionKey);
        Assert.Equal("config.warning.spawn_tackle_requires_attach", message.TranslationKey);
    }

    [Fact]
    public void Evaluate_AttachesSkipConflictMessageToAutomaticMinigameOption()
    {
        ModConfig config = new()
        {
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
