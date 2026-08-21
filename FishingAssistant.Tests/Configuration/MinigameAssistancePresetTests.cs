using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class MinigameAssistancePresetTests
{
    [Theory]
    [InlineData((int)MinigameAssistancePreset.Off, 100, 100, 100, 100, 100)]
    [InlineData((int)MinigameAssistancePreset.Light, 85, 115, 80, 125, 110)]
    [InlineData((int)MinigameAssistancePreset.Comfortable, 70, 135, 60, 160, 125)]
    [InlineData((int)MinigameAssistancePreset.Relaxed, 50, 175, 25, 225, 150)]
    public void Apply_PopulatesKnownPreset(
        int presetValue,
        int fishSpeed,
        int gain,
        int loss,
        int treasure,
        int barSize)
    {
        ModConfig config = new();
        MinigameAssistancePreset preset = (MinigameAssistancePreset)presetValue;

        MinigameAssistancePresets.Apply(config, preset);

        Assert.Equal(preset, config.MinigameAssistance);
        Assert.Equal(fishSpeed, config.FishSpeedPercent);
        Assert.Equal(gain, config.ProgressGainPercent);
        Assert.Equal(loss, config.ProgressLossPercent);
        Assert.Equal(treasure, config.TreasureSpeedPercent);
        Assert.Equal(barSize, config.BarSizePercent);
    }

    [Fact]
    public void Detect_ReturnsCustomWhenValuesDoNotMatchKnownPreset()
    {
        ModConfig config = new() { FishSpeedPercent = 73 };

        Assert.Equal(MinigameAssistancePreset.Custom, MinigameAssistancePresets.Detect(config));
    }

    [Fact]
    public void Detect_ResolvesExactKnownPreset()
    {
        ModConfig config = new();
        MinigameAssistancePresets.Apply(config, MinigameAssistancePreset.Comfortable);
        config.MinigameAssistance = MinigameAssistancePreset.Custom;

        Assert.Equal(MinigameAssistancePreset.Comfortable, MinigameAssistancePresets.Detect(config));
    }
}
