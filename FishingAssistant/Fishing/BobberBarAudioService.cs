using FishingAssistant.Runtime;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace FishingAssistant.Fishing;

internal sealed class BobberBarAudioService(IMonitor monitor)
{
    private readonly PerScreen<object?> activeBars = new(() => null);

    public void OnMenuChanged(IClickableMenu? oldMenu, IClickableMenu? newMenu)
    {
        this.activeBars.Value = newMenu is BobberBar newBar ? newBar : null;
        if (oldMenu is BobberBar && newMenu is not BobberBar)
            this.TryStopOrphaned("the fishing minigame closed");
    }

    public void OnMinigameCompleted(object barIdentity)
    {
        ArgumentNullException.ThrowIfNull(barIdentity);

        bool hasOtherActiveBar = this.activeBars.GetActiveValues()
            .Any(pair => pair.Value is not null && !ReferenceEquals(pair.Value, barIdentity));
        if (BobberBarAudioCleanupPolicy.ShouldStopForCompletion(hasOtherActiveBar))
            this.TryStopCues("Fishing Assistant completed the fishing minigame");
    }

    public void ResetCurrent(AutomationTransitionReason reason)
    {
        this.activeBars.Value = null;
        this.TryStopOrphaned($"the local screen reset for {reason}");
    }

    public void ResetAll(AutomationTransitionReason reason)
    {
        this.activeBars.ResetAllScreens();
        this.TryStopCues($"all local screens reset for {reason}");
    }

    private void TryStopOrphaned(string context)
    {
        bool hasAnyActiveBar = this.activeBars.GetActiveValues().Any(pair => pair.Value is not null);
        if (BobberBarAudioCleanupPolicy.ShouldStopOrphaned(hasAnyActiveBar))
            this.TryStopCues(context);
    }

    private void TryStopCues(string context)
    {
        try
        {
            bool stopped = StopCue(BobberBar.reelSound);
            stopped |= StopCue(BobberBar.unReelSound);
            if (stopped)
                monitor.Log($"Stopped fishing-minigame reel audio after {context}.", LogLevel.Trace);
        }
        catch (Exception exception)
        {
            monitor.Log(
                $"Fishing-minigame reel audio could not be cleaned up after {context}.\n{exception}",
                LogLevel.Warn);
        }
    }

    private static bool StopCue(ICue? cue)
    {
        if (cue is null || cue.IsStopped || cue.IsStopping)
            return false;

        cue.Stop(AudioStopOptions.Immediate);
        return true;
    }
}
