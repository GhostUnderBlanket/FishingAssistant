using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigApplyFeedbackTests
{
    [Fact]
    public void Create_ReportsNoMessagesForCleanApply()
    {
        ConfigApplyFeedback feedback = ConfigApplyFeedback.Create(new ConfigValidationReport());

        Assert.False(feedback.HasMessages);
        Assert.Equal(0, feedback.CorrectionCount);
        Assert.Equal(0, feedback.WarningCount);
        Assert.Empty(feedback.AffectedProperties);
        Assert.Equal(0, feedback.AdditionalPropertyCount);
    }

    [Fact]
    public void Create_SummarizesCorrectionsWarningsAndDistinctProperties()
    {
        ConfigValidationReport report = new();
        report.Add("AutoCastFishingRod", true, false, "corrected");
        report.Warn("AutoCastFishingRod", false, "warning");
        report.Warn("AutoPlayMiniGame", true, "warning");

        ConfigApplyFeedback feedback = ConfigApplyFeedback.Create(report);

        Assert.True(feedback.HasMessages);
        Assert.Equal(1, feedback.CorrectionCount);
        Assert.Equal(2, feedback.WarningCount);
        Assert.Equal(["AutoCastFishingRod", "AutoPlayMiniGame"], feedback.AffectedProperties);
        Assert.Equal(0, feedback.AdditionalPropertyCount);
    }

    [Fact]
    public void Create_LimitsAffectedPropertiesAndCountsTheRemainder()
    {
        ConfigValidationReport report = new();
        report.Warn("One", 1, "warning");
        report.Warn("Two", 2, "warning");
        report.Warn("Three", 3, "warning");

        ConfigApplyFeedback feedback = ConfigApplyFeedback.Create(report, propertyLimit: 2);

        Assert.Equal(["One", "Two"], feedback.AffectedProperties);
        Assert.Equal(1, feedback.AdditionalPropertyCount);
    }

    [Fact]
    public void Create_RejectsNegativePropertyLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConfigApplyFeedback.Create(new ConfigValidationReport(), propertyLimit: -1));
    }
}
