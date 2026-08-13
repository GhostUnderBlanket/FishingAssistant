using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using FishingAssistant.Inventory;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace FishingAssistant.Runtime;

internal sealed class AutomationRuntime(
    IMonitor monitor,
    Func<ModConfig> getConfig,
    Func<string, string> translate)
{
    private readonly PerScreen<AutomationScreenState> screens = new(() => new AutomationScreenState());
    private readonly AutoEatService autoEat = new(monitor, translate);
    private readonly LateNightService lateNight = new(monitor, translate);

    public AutomationSession Current => this.screens.Value.Session;

    public void UpdateCurrent()
    {
        if (!Context.IsWorldReady)
            return;

        AutomationScreenState screen = this.screens.Value;
        Tool? currentTool = Game1.player.CurrentTool;
        if (screen.HasObservedTool && !ReferenceEquals(screen.LastTool, currentTool))
            this.CancelPendingActions(screen, AutomationTransitionReason.ToolChanged, disable: false);

        screen.LastTool = currentTool;
        screen.HasObservedTool = true;
        FishingObservation observation = FishingContextReader.Read(screen.Session.IsEnabled);
        this.Log(screen.Session.Observe(observation));
        AutomationRecoveryConditions recovery = this.GetRecoveryConditions(screen);
        if (AutomationRecoveryPolicy.ShouldCancelForBlockingMenu(recovery, observation.HasBlockingMenu))
        {
            this.CancelPendingActions(screen, AutomationTransitionReason.MenuInterrupted, disable: false);
            return;
        }
        if (this.UpdateRecoveryTimeout(screen))
            return;
        this.Log(this.lateNight.UpdateCurrent(getConfig(), screen.Session));
        this.autoEat.UpdateCurrent(getConfig(), screen.Session);
        this.UpdateLowEnergyStop(screen);
        this.UpdateBubbleSteering(screen);
        this.UpdateInstantBite();
        this.UpdateAutomaticMinigame(screen);
        this.UpdateAutomaticCatchPopup(screen);
        this.UpdateAutomaticTreasureLoot(screen);
        this.UpdateAutomaticHook(screen);
        this.UpdateAutomaticCast(screen);
    }

    public void ToggleCurrent()
    {
        AutomationScreenState screen = this.screens.Value;
        AutomationTransition transition = screen.Toggle();
        this.autoEat.ResetCurrent();
        monitor.Log(
            $"Automation {(this.Current.IsEnabled ? "enabled" : "disabled")} for local screen {Context.ScreenId}.",
            LogLevel.Info);
        this.Log(transition);
    }

    public void OnTimeChanged(int newTime)
    {
        this.lateNight.OnTimeChanged(getConfig(), this.screens.Value.Session, newTime);
    }

    public void ResetCurrent(AutomationTransitionReason reason)
    {
        AutomationScreenState screen = this.screens.Value;
        this.CancelPendingActions(screen, reason, disable: false);
        screen.ResetObservedTool();
        this.autoEat.ResetCurrent();
        if (reason is AutomationTransitionReason.DayStarted or AutomationTransitionReason.SaveLoaded)
            this.lateNight.ResetCurrent();
    }

    public void ResetAll(AutomationTransitionReason reason)
    {
        this.ResetActiveScreens(reason);
        this.screens.ResetAllScreens();
    }

    public void ResetActiveScreens(AutomationTransitionReason reason)
    {
        foreach (AutomationScreenState screen in this.screens.GetActiveValues().Select(pair => pair.Value))
        {
            this.Log(screen.Cancel(reason, disable: false));
            screen.ResetObservedTool();
        }
        this.autoEat.ResetAll();
        this.lateNight.ResetAll();
    }

    private void Log(AutomationTransition? transition)
    {
        if (transition is null)
            return;

        monitor.Log(
            $"Automation state for local screen {Context.ScreenId}: {transition.Previous} -> " +
            $"{transition.Current} ({transition.Reason}{(transition.WasRecovery ? ", recovered" : "")}).",
            transition.WasRecovery ? LogLevel.Debug : LogLevel.Trace);
    }

    private bool UpdateRecoveryTimeout(AutomationScreenState screen)
    {
        PendingAutomationAction action = AutomationRecoveryPolicy.GetPendingAction(
            this.GetRecoveryConditions(screen));
        if (action == PendingAutomationAction.None)
        {
            screen.Pending.Action = PendingAutomationAction.None;
            screen.Pending.ActionTicks = 0;
            return false;
        }

        if (screen.Pending.Action != action)
        {
            screen.Pending.Action = action;
            screen.Pending.ActionTicks = 1;
        }
        else
        {
            screen.Pending.ActionTicks++;
        }

        if (!AutomationRecoveryPolicy.HasTimedOut(action, screen.Pending.ActionTicks))
            return false;

        monitor.Log(
            $"Disabled fishing automation for local screen {Context.ScreenId} after {action} timed out.",
            LogLevel.Warn);
        this.CancelPendingActions(screen, AutomationTransitionReason.TimedOut, disable: true);
        return true;
    }

    private AutomationRecoveryConditions GetRecoveryConditions(AutomationScreenState screen)
    {
        return new AutomationRecoveryConditions(
            screen.Session.State,
            screen.Pending.AutomaticCastInProgress,
            screen.Pending.HookAttemptedForNibble,
            screen.Pending.FishPopupCloseAttempted);
    }

    private void CancelPendingActions(
        AutomationScreenState screen,
        AutomationTransitionReason reason,
        bool disable)
    {
        if (screen.Pending.AutomaticCastInProgress)
            FishingRodAdapter.ForCurrentPlayer()?.CancelAutomaticCast();
        this.autoEat.ResetCurrent();
        this.Log(screen.Cancel(reason, disable));
    }

    private void UpdateAutomaticCast(AutomationScreenState screen)
    {
        ModConfig config = getConfig();
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.Pending.ReadyTicks = 0;
            screen.Pending.AutomaticCastInProgress = false;
            return;
        }

        if (screen.Pending.AutomaticCastInProgress)
        {
            if (rod.IsTimingCast)
                rod.SetCastPower(config.DefaultCastPower);

            if (screen.Session.State is AutomationState.Ready or AutomationState.Casting)
            {
                screen.Pending.ReadyTicks = 0;
                return;
            }

            screen.Pending.AutomaticCastInProgress = false;
        }

        AutoCastConditions conditions = rod.ReadAutoCastConditions(
            screen.Session.IsEnabled,
            config.AutoCastFishingRod,
            screen.Session.State,
            config.DefaultCastPower
        );
        int requiredTicks = (int)Math.Ceiling(config.AutoCastDelaySeconds * 60f);
        if (rod.IsSupportedFishingMinigame)
            requiredTicks = Math.Max(requiredTicks, 75);
        switch (AutoCastPolicy.Decide(conditions, screen.Pending.ReadyTicks, requiredTicks))
        {
            case AutoCastDecision.Reset:
                screen.Pending.ReadyTicks = 0;
                break;
            case AutoCastDecision.Wait:
                screen.Pending.ReadyTicks++;
                break;
            case AutoCastDecision.Cast:
                screen.Pending.ReadyTicks = 0;
                screen.Pending.AutomaticCastInProgress = true;
                rod.BeginAutomaticCast(config.DefaultCastPower);
                monitor.Log($"Started an automatic cast for local screen {Context.ScreenId}.", LogLevel.Trace);
                break;
        }
    }

    private void UpdateBubbleSteering(AutomationScreenState screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null || !rod.IsBobberInAir)
        {
            screen.Pending.BubbleSteeringRod = null;
            screen.Pending.BubbleSteeringTarget = Microsoft.Xna.Framework.Vector2.Zero;
            screen.Pending.BubbleSteeringExpectedPosition = Microsoft.Xna.Framework.Vector2.Zero;
            return;
        }

        if (screen.Pending.BubbleSteeringRod is null)
        {
            if (!rod.TryGetBubbleSteeringTarget(
                    getConfig().AutomaticBubbleSteering,
                    out Microsoft.Xna.Framework.Vector2 target))
                return;

            screen.Pending.BubbleSteeringRod = rod.Identity;
            screen.Pending.BubbleSteeringTarget = target;
            screen.Pending.BubbleSteeringExpectedPosition = rod.BobberPosition;
            monitor.Log($"Started steering a cast toward a fishing bubble for local screen {Context.ScreenId}.",
                LogLevel.Trace);
        }

        if (!ReferenceEquals(screen.Pending.BubbleSteeringRod, rod.Identity))
        {
            screen.Pending.BubbleSteeringRod = null;
            return;
        }

        Microsoft.Xna.Framework.Vector2 expectedPosition = screen.Pending.BubbleSteeringExpectedPosition;
        rod.SteerToward(screen.Pending.BubbleSteeringTarget, ref expectedPosition);
        screen.Pending.BubbleSteeringExpectedPosition = expectedPosition;
    }

    private void UpdateLowEnergyStop(AutomationScreenState screen)
    {
        ModConfig config = getConfig();
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
            return;

        LowEnergyStopDecision decision = LowEnergyStopPolicy.Decide(
            rod.ReadLowEnergyStopConditions(
                screen.Session.IsEnabled,
                config.AutoCastFishingRod,
                screen.Session.State,
                config.AutoEatFood,
                config.EnergyPercentToEat));
        if (decision == LowEnergyStopDecision.None)
            return;

        string messageKey = decision == LowEnergyStopDecision.StopAtEatingThreshold
            ? "hud.energy.no_food"
            : "hud.energy.exhaustion";
        Game1.addHUDMessage(new HUDMessage(translate(messageKey), HUDMessage.error_type));
        monitor.Log(
            $"Paused fishing automation for low energy on local screen {Context.ScreenId} ({decision}).",
            LogLevel.Info);
        this.Log(screen.Session.Disable(AutomationTransitionReason.LowEnergy));
    }

    private void UpdateAutomaticHook(AutomationScreenState screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.Pending.HookAttemptedForNibble = false;
            return;
        }

        ModConfig config = getConfig();
        AutoHookConditions conditions = rod.ReadAutoHookConditions(
            screen.Session.IsEnabled,
            config.AutoHookFish,
            screen.Session.State,
            screen.Pending.HookAttemptedForNibble
        );
        switch (AutoHookPolicy.Decide(conditions))
        {
            case AutoHookDecision.ResetAttempt:
                screen.Pending.HookAttemptedForNibble = false;
                break;
            case AutoHookDecision.Wait:
                break;
            case AutoHookDecision.Hook:
                screen.Pending.HookAttemptedForNibble = true;
                rod.HookFish();
                monitor.Log($"Hooked a fish automatically for local screen {Context.ScreenId}.", LogLevel.Trace);
                break;
        }
    }

    private void UpdateInstantBite()
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
            return;

        InstantBiteDecision decision = InstantBitePolicy.Decide(
            rod.ReadInstantBiteConditions(getConfig().InstantFishBite));
        if (decision != InstantBiteDecision.Trigger)
            return;

        rod.TriggerInstantBite();
        monitor.Log($"Triggered an instant fish bite for local screen {Context.ScreenId}.", LogLevel.Trace);
    }

    private void UpdateAutomaticCatchPopup(AutomationScreenState screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.Pending.FishPopupVisibleTicks = 0;
            screen.Pending.FishPopupCloseAttempted = false;
            return;
        }

        ModConfig config = getConfig();
        AutoClosePopupConditions conditions = rod.ReadAutoClosePopupConditions(
            screen.Session.IsEnabled,
            config.AutoClosePopup,
            screen.Session.State,
            screen.Pending.FishPopupCloseAttempted
        );
        switch (AutoClosePopupPolicy.Decide(conditions, screen.Pending.FishPopupVisibleTicks))
        {
            case AutoClosePopupDecision.Reset:
                screen.Pending.FishPopupVisibleTicks = 0;
                screen.Pending.FishPopupCloseAttempted = false;
                break;
            case AutoClosePopupDecision.Wait:
                if (conditions.IsEligible)
                    screen.Pending.FishPopupVisibleTicks++;
                break;
            case AutoClosePopupDecision.Close:
                screen.Pending.FishPopupCloseAttempted = true;
                rod.CloseFishPopup();
                monitor.Log($"Closed the catch popup automatically for local screen {Context.ScreenId}.",
                    LogLevel.Trace);
                break;
        }
    }

    private void UpdateAutomaticMinigame(AutomationScreenState screen)
    {
        BobberBarAdapter? bar = BobberBarAdapter.ForCurrentScreen();
        if (bar is null)
        {
            screen.Pending.IsPursuingTreasure = false;
            screen.Pending.ConfiguredBobberBar = null;
            return;
        }

        ModConfig config = getConfig();
        bar.ApplyLiveCatchModifiers(config);
        if (!ReferenceEquals(screen.Pending.ConfiguredBobberBar, bar.Identity))
        {
            FishDifficultyDecision difficulty = bar.ApplyDifficulty(config);
            TreasureChanceDecision chance = TreasureChancePolicy.Decide(
                bar.ReadTreasureChanceConditions(config));
            bar.ApplyTreasureChance(chance);
            screen.Pending.ConfiguredBobberBar = bar.Identity;
            monitor.Log(
                $"Configured fishing minigame for local screen {Context.ScreenId}: " +
                $"difficulty={difficulty.VanillaDifficulty:0.##}->{difficulty.AdjustedDifficulty:0.##}, " +
                $"treasure={chance.HasTreasure}, golden={chance.IsGoldenTreasure}.",
                LogLevel.Trace);
        }

        if (InstantTreasurePolicy.Decide(bar.ReadInstantTreasureConditions(config.InstantCatchTreasure))
            == InstantTreasureDecision.Capture)
        {
            bar.CaptureTreasure();
            screen.Pending.IsPursuingTreasure = false;
            monitor.Log($"Captured fishing treasure instantly for local screen {Context.ScreenId}.",
                LogLevel.Trace);
        }

        if (this.TrySkipMinigame(screen, bar, config))
            return;

        bool assistanceActive = screen.Session.IsEnabled
            && config.AutoPlayMiniGame
            && screen.Session.State == AutomationState.Minigame;
        TreasureTargetDecision target = TreasureTargetPolicy.Decide(bar.ReadTreasureConditions(
            assistanceActive,
            config.TreasureTargeting,
            screen.Pending.IsPursuingTreasure
        ));
        screen.Pending.IsPursuingTreasure = target.IsTargetingTreasure;
        MinigameControlDecision decision = MinigameControlPolicy.Decide(bar.ReadConditions(
            screen.Session.IsEnabled,
            config.AutoPlayMiniGame,
            screen.Session.State,
            target.Position
        ));
        if (decision.ShouldControl)
            bar.SetBarSpeed(decision.BarSpeed);
    }

    private bool TrySkipMinigame(AutomationScreenState screen, BobberBarAdapter bar, ModConfig config)
    {
        SkipMinigameDecision decision = SkipMinigamePolicy.Decide(
            bar.ReadSkipMinigameConditions(config.SkipFishingMiniGame));
        if (decision != SkipMinigameDecision.Skip)
            return false;

        bar.CompleteMinigame(
            config.TreasureTargeting || config.InstantCatchTreasure);
        screen.Pending.IsPursuingTreasure = false;
        monitor.Log(
            $"Skipped the fishing minigame for local screen {Context.ScreenId}; treasure targeting was " +
            $"{(config.TreasureTargeting ? "enabled" : "disabled")} in config.",
            LogLevel.Trace);
        return true;
    }

    private void UpdateAutomaticTreasureLoot(AutomationScreenState screen)
    {
        FishingTreasureMenuAdapter? menu = FishingTreasureMenuAdapter.ForCurrentScreen();
        if (menu is null)
        {
            this.ResetTreasureLoot(screen);
            return;
        }

        if (!ReferenceEquals(screen.TreasureMenuIdentity, menu.Identity))
        {
            this.ResetTreasureLoot(screen);
            screen.TreasureMenuIdentity = menu.Identity;
        }

        ModConfig config = getConfig();
        HashSet<string> ignoredItemIds = config.TreasureChestIgnoreList
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        TreasureLootConditions conditions = new(
            screen.Session.IsEnabled,
            config.AutoLootTreasure,
            IsFishingTreasureMenu: true,
            menu.IsPlayerHoldingItem,
            screen.TreasureCollectionStopped,
            menu.HasRemainingItems,
            menu.HasCollectibleItem(screen.BlockedTreasureItems, ignoredItemIds),
            menu.HasBlockedNonIgnoredItem(screen.BlockedTreasureItems, ignoredItemIds),
            menu.HasIgnoredItem(ignoredItemIds),
            config.ActionIfInventoryFull,
            config.ActionIfOnlyIgnoredTreasureRemains
        );
        TreasureLootDecision decision = TreasureLootPolicy.Decide(
            conditions,
            screen.TreasureLootElapsedTicks,
            screen.TreasureLootRequiredTicks
        );
        switch (decision)
        {
            case TreasureLootDecision.Reset:
                this.ResetTreasureLoot(screen);
                break;
            case TreasureLootDecision.Wait:
                if (conditions.IsEligible)
                    screen.TreasureLootElapsedTicks++;
                break;
            case TreasureLootDecision.Collect:
                this.CollectNextTreasureItem(screen, menu, ignoredItemIds);
                break;
            case TreasureLootDecision.Close:
                menu.Close();
                this.ResetTreasureLoot(screen);
                break;
            case TreasureLootDecision.Stop:
                this.StopForFullInventory(screen, "hud.treasure_full.stop");
                break;
            case TreasureLootDecision.DropBlocked:
                menu.DropBlockedItems(screen.BlockedTreasureItems, ignoredItemIds);
                this.ResolveIgnoredTreasureRemainder(menu, config.ActionIfOnlyIgnoredTreasureRemains);
                this.StopForFullInventory(screen, "hud.treasure_full.drop");
                break;
            case TreasureLootDecision.DiscardBlocked:
                menu.DiscardBlockedItems(screen.BlockedTreasureItems, ignoredItemIds);
                this.ResolveIgnoredTreasureRemainder(menu, config.ActionIfOnlyIgnoredTreasureRemains);
                this.StopForFullInventory(screen, "hud.treasure_full.discard");
                break;
            case TreasureLootDecision.KeepIgnoredOpen:
                screen.TreasureCollectionStopped = true;
                monitor.Log($"Left ignored fishing treasure open for local screen {Context.ScreenId}.",
                    LogLevel.Trace);
                break;
            case TreasureLootDecision.DropIgnored:
                menu.DropRemainingItems();
                this.ResetTreasureLoot(screen);
                break;
            case TreasureLootDecision.DiscardIgnored:
                menu.DiscardRemainingItems();
                this.ResetTreasureLoot(screen);
                break;
        }
    }

    private void ResolveIgnoredTreasureRemainder(
        FishingTreasureMenuAdapter menu,
        IgnoredTreasureAction action)
    {
        if (!menu.HasRemainingItems)
        {
            menu.Close();
            return;
        }

        switch (action)
        {
            case IgnoredTreasureAction.Drop:
                menu.DropRemainingItems();
                break;
            case IgnoredTreasureAction.Discard:
                menu.DiscardRemainingItems();
                break;
        }
    }

    private void CollectNextTreasureItem(
        AutomationScreenState screen,
        FishingTreasureMenuAdapter menu,
        IReadOnlySet<string> ignoredItemIds)
    {
        TreasureCollectResult result = menu.TryCollectNext(screen.BlockedTreasureItems, ignoredItemIds);
        screen.TreasureLootElapsedTicks = 0;
        screen.TreasureLootRequiredTicks = TreasureLootPolicy.ItemDelayTicks;
        if (result is TreasureCollectResult.Collected or TreasureCollectResult.PartiallyCollected)
        {
            monitor.Log($"Collected fishing treasure for local screen {Context.ScreenId} ({result}).",
                LogLevel.Trace);
        }
    }

    private void StopForFullInventory(AutomationScreenState screen, string messageKey)
    {
        screen.TreasureCollectionStopped = true;
        Game1.addHUDMessage(new HUDMessage(translate(messageKey), HUDMessage.error_type));
        if (screen.Session.IsEnabled)
        {
            AutomationTransition transition = screen.Session.Toggle();
            this.Log(transition);
        }

        monitor.Log($"Stopped fishing automation for local screen {Context.ScreenId} because the inventory " +
                    "couldn't accept the remaining treasure.", LogLevel.Warn);
    }

    private void ResetTreasureLoot(AutomationScreenState screen)
    {
        screen.ResetTreasureLoot();
    }
}
