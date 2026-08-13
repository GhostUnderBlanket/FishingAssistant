using FishingAssistant.Configuration;
using FishingAssistant.UI;

namespace FishingAssistant.Tests.UI;

public sealed class ConfigResetWorkflowTests
{
    [Fact]
    public void Request_DoesNotCreateOrApplyDefaultsBeforeConfirmation()
    {
        int createCount = 0;
        ConfigResetWorkflow workflow = new(() =>
        {
            createCount++;
            return new ModConfig();
        });

        workflow.Request();

        Assert.True(workflow.IsPending);
        Assert.Equal(0, createCount);
    }

    [Fact]
    public void Cancel_DiscardsPendingReset()
    {
        int createCount = 0;
        ConfigResetWorkflow workflow = new(() =>
        {
            createCount++;
            return new ModConfig();
        });
        workflow.Request();

        workflow.Cancel();
        ModConfig? defaults = workflow.Confirm();

        Assert.False(workflow.IsPending);
        Assert.Null(defaults);
        Assert.Equal(0, createCount);
    }

    [Fact]
    public void Confirm_CreatesDefaultsExactlyOnceAndClearsPendingReset()
    {
        ModConfig expected = new() { AutoCastFishingRod = false };
        int createCount = 0;
        ConfigResetWorkflow workflow = new(() =>
        {
            createCount++;
            return expected;
        });
        workflow.Request();

        ModConfig? defaults = workflow.Confirm();
        ModConfig? duplicate = workflow.Confirm();

        Assert.Same(expected, defaults);
        Assert.Null(duplicate);
        Assert.False(workflow.IsPending);
        Assert.Equal(1, createCount);
    }
}
