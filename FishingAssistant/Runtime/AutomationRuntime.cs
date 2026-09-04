using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
using FishingAssistant.Inventory;
using FishingAssistant.HUD;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishingAssistant.Runtime;

internal sealed class AutomationRuntime(
    IMonitor monitor,
    Func<ModConfig> getConfig,
    Func<string, string> translate,
    PerfectCatchProgressService perfectCatchProgress,
    ActivityLogService? activityLog = null)
{
    private readonly PerScreen<AutomationScreenState> screens = new(() => new AutomationScreenState());
    private readonly AutoEatService autoEat = new(monitor, translate, activityLog);
    private readonly LateNightService lateNight = new(monitor, translate, activityLog);

    public AutomationSession Current => this.screens.Value.Session;

    public bool IsCurrentMinigameSkipResult()
    {
        BobberBarAdapter? bar = BobberBarAdapter.ForCurrentScreen();
        return bar is not null
            && ReferenceEquals(this.screens.Value.Pending.SkippedBobberBar, bar.Identity);
    }

    public bool TrySkipBeforeFirstDraw(BobberBar bobberBar)
    {
        ArgumentNullException.ThrowIfNull(bobberBar);

        try
        {
            AutomationScreenState screen = this.screens.Value;
            ModConfig config = getConfig();
            BobberBarAdapter bar = new(bobberBar);
            SkipMinigameDecision decision = SkipMinigamePolicy.Decide(
                bar.ReadSkipMinigameConditions(
                    config.SkipFishingMiniGame,
                    config.SkipMinigameCatchesRequired,
                    perfectCatchProgress,
                    allowOpeningAnimation: true));
            if (decision != SkipMinigameDecision.Skip)
                return false;

            bar.ApplyLiveCatchModifiers(config);
            FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
            TreasureChanceDecision chance = TreasureChancePolicy.Decide(
                bar.ReadTreasureChanceConditions(config, rod));
            bar.ApplyTreasureChance(chance, rod);

            screen.Pending.SkippedBobberBar = bar.Identity;
            screen.Pending.ConfiguredBobberBar = bar.Identity;
            screen.Pending.IsPursuingTreasure = false;
            bar.PrepareInvisibleCompletion(
                config.TreasureTargeting || config.InstantCatchTreasure);
            monitor.Log(
                $"Prepared an invisible fishing-minigame skip for local screen {Context.ScreenId}.",
                LogLevel.Trace);
            return true;
        }
        catch (Exception exception)
        {
            monitor.Log(
                $"The fishing minigame couldn't be skipped before its first draw; " +
                $"the normal compatibility path will be used instead.\n{exception}",
                LogLevel.Warn);
            return false;
        }
    }

    public BubbleCastPlan? GetBubbleMarkerPlanCurrent()
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
            return null;

        ModConfig config = getConfig();
        AutomationScreenState screen = this.screens.Value;
        int requestedPower;
        bool adjustPower;
        if (rod.IsTimingCast)
        {
            requestedPower = (int)Math.Round(rod.CastingPower * 100f);
            adjustPower = screen.Session.IsEnabled
                && config.AutomaticBubbleSteering
                && config.AutomaticCastPowerAdjustmentMode.AppliesToManual();
        }
        else if (rod.IsBobberInAir)
        {
            requestedPower = (int)Math.Round(rod.CastingPower * 100f);
            adjustPower = false;
        }
        else
        {
            requestedPower = screen.Pending.AutomaticCastPower
                ?? screen.Pending.SessionCastPower
                ?? config.DefaultCastPower;
            adjustPower = screen.Session.IsEnabled
                && config.AutomaticBubbleSteering
                && config.AutoCastFishingRod
                && config.AutomaticCastPowerAdjustmentMode.AppliesToAutomatic();
        }

        return rod.GetBubbleCastPlan(requestedPower, adjustPower, config.SteeringEffort);
    }

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
        ModConfig config = getConfig();
        AutomationTransition? lateNightStop = this.lateNight.UpdateCurrent(config, screen.Session);
        this.Log(lateNightStop);
        if (this.TryOpenInventoryAfterSafetyStop(config, lateNightStop))
            return;
        if (this.autoEat.UpdateCurrent(getConfig(), screen.Session))
            return;
        this.UpdateManualCastPower(screen);
        this.UpdateManualBubbleCastPower(screen);
        AutomationTransition? lowEnergyStop = this.UpdateLowEnergyStop(screen);
        this.Log(lowEnergyStop);
        if (this.TryOpenInventoryAfterSafetyStop(config, lowEnergyStop))
            return;
        this.UpdateBubbleSteering(screen);
        this.UpdateBiteWaitingTime(screen);
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

    public void ResetSessionCastPowerCurrent()
    {
        this.ResetManualCastTracking(this.screens.Value, preserveSessionPower: false);
    }

    public void InvalidateTreasureChestIgnoreCacheCurrent()
    {
        this.screens.Value.InvalidateTreasureChestIgnoreIds();
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
        int requestedCastPower = screen.Pending.SessionCastPower ?? config.DefaultCastPower;
        int castPower = screen.Pending.AutomaticCastPower ?? requestedCastPower;
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.Pending.ReadyTicks = 0;
            screen.Pending.AutomaticCastInProgress = false;
            screen.Pending.AutomaticCastPower = null;
            return;
        }

        if (screen.Pending.AutomaticCastInProgress)
        {
            if (rod.IsTimingCast)
                rod.SetCastPower(castPower);

            if (screen.Session.State is AutomationState.Ready or AutomationState.Casting)
            {
                screen.Pending.ReadyTicks = 0;
                return;
            }

            screen.Pending.AutomaticCastInProgress = false;
            screen.Pending.AutomaticCastPower = null;
        }

        BubbleCastPlan castPlan = rod.GetBubbleCastPlan(
            requestedCastPower,
            config.AutomaticBubbleSteering && config.AutomaticCastPowerAdjustmentMode.AppliesToAutomatic(),
            config.SteeringEffort);
        castPower = castPlan.CastPower;

        AutoCastConditions conditions = rod.ReadAutoCastConditions(
            screen.Session.IsEnabled,
            config.AutoCastFishingRod,
            screen.Session.State,
            castPower
        );
        int requiredTicks = (int)Math.Ceiling(config.AutoCastDelaySeconds * 60f);
        if (rod.IsSupportedFestivalFishing)
            requiredTicks = Math.Max(requiredTicks, 75);
        switch (AutoCastPolicy.Decide(conditions, screen.Pending.ReadyTicks, requiredTicks))
        {
            case AutoCastDecision.Reset:
                screen.Pending.ReadyTicks = 0;
                screen.Pending.AutomaticCastPower = null;
                break;
            case AutoCastDecision.Wait:
                screen.Pending.ReadyTicks++;
                break;
            case AutoCastDecision.Cast:
                screen.Pending.ReadyTicks = 0;
                screen.Pending.AutomaticCastInProgress = true;
                screen.Pending.AutomaticCastPower = castPower;
                rod.BeginAutomaticCast(castPower);
                monitor.Log($"Started an automatic cast for local screen {Context.ScreenId}.", LogLevel.Trace);
                break;
        }
    }

    private void UpdateManualCastPower(AutomationScreenState screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        ModConfig config = getConfig();
        bool isTimingCast = rod?.IsTimingCast == true;
        ManualCastPowerDecision decision = ManualCastPowerPolicy.Decide(new(
            screen.Session.IsEnabled,
            isTimingCast,
            screen.Pending.ManualCastWasTiming,
            screen.Pending.PlayerCastInputObserved,
            screen.Pending.AutomaticCastInProgress,
            screen.Pending.ManualCastPowerUnlocked,
            screen.Pending.ManualCastPowerTicks,
            config.UnlockCastPowerTime));

        switch (decision)
        {
            case ManualCastPowerDecision.Reset:
                this.ResetManualCastTracking(screen);
                break;
            case ManualCastPowerDecision.UseVanilla:
                screen.Pending.PlayerCastInputObserved = true;
                screen.Pending.ManualCastWasTiming = true;
                screen.Pending.ManualCastPowerUnlocked = true;
                screen.Pending.ReadyTicks = 0;
                break;
            case ManualCastPowerDecision.HoldSessionPower:
                screen.Pending.PlayerCastInputObserved = true;
                screen.Pending.ManualCastWasTiming = true;
                screen.Pending.ReadyTicks = 0;
                rod!.SetCastPower(screen.Pending.SessionCastPower ?? config.DefaultCastPower);
                screen.Pending.ManualCastPowerTicks++;
                break;
            case ManualCastPowerDecision.RememberVanillaPower:
                screen.Pending.SessionCastPower = (int)Math.Round(rod!.CastingPower * 100f);
                monitor.Log(
                    $"Remembered manual cast power {screen.Pending.SessionCastPower}% for local screen {Context.ScreenId}.",
                    LogLevel.Trace);
                this.ResetManualCastTracking(screen, preserveSessionPower: true);
                break;
        }
    }

    private void UpdateManualBubbleCastPower(AutomationScreenState screen)
    {
        ModConfig config = getConfig();
        if (!screen.Session.IsEnabled
            || !config.AutomaticBubbleSteering
            || screen.Pending.AutomaticCastInProgress
            || !config.AutomaticCastPowerAdjustmentMode.AppliesToManual())
            return;

        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod?.IsTimingCast != true)
            return;

        int requestedPower = (int)Math.Round(rod.CastingPower * 100f);
        BubbleCastPlan plan = rod.GetBubbleCastPlan(
            requestedPower,
            adjustCastPower: true,
            config.SteeringEffort);
        if (plan.IsReachable)
            rod.SetCastPower(plan.CastPower);
    }

    private void ResetManualCastTracking(AutomationScreenState screen, bool preserveSessionPower = true)
    {
        screen.Pending.ManualCastPowerTicks = 0;
        screen.Pending.PlayerCastInputObserved = false;
        screen.Pending.ManualCastWasTiming = false;
        screen.Pending.ManualCastPowerUnlocked = false;
        if (!preserveSessionPower)
            screen.Pending.SessionCastPower = null;
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
                    getConfig().SteeringEffort,
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
        rod.SteerToward(screen.Pending.BubbleSteeringTarget, getConfig().SteeringEffort, ref expectedPosition);
        screen.Pending.BubbleSteeringExpectedPosition = expectedPosition;
    }

    private AutomationTransition? UpdateLowEnergyStop(AutomationScreenState screen)
    {
        ModConfig config = getConfig();
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
            return null;

        LowEnergyStopDecision decision = LowEnergyStopPolicy.Decide(
            rod.ReadLowEnergyStopConditions(
                screen.Session.IsEnabled,
                config.AutoCastFishingRod,
                screen.Session.State,
                config.AutoEatFood
                && config.AutoEatTrigger == AutoEatTriggerBehavior.AtEnergyTarget,
                config.EnergyPercentToEat));
        if (decision == LowEnergyStopDecision.None)
            return null;

        string messageKey = decision == LowEnergyStopDecision.StopAtEatingThreshold
            ? "hud.energy.no_food"
            : "hud.energy.exhaustion";
        string message = translate(messageKey);
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
        activityLog?.Add(message, severity: ActivityLogSeverity.Error);
        monitor.Log(
            $"Paused fishing automation for low energy on local screen {Context.ScreenId} ({decision}).",
            LogLevel.Info);
        return screen.Session.Disable(AutomationTransitionReason.LowEnergy);
    }

    private bool TryOpenInventoryAfterSafetyStop(
        ModConfig config,
        AutomationTransition? transition)
    {
        OpenInventoryAfterStopConditions conditions = new(
            config.OpenInventoryOnStop,
            transition?.Reason,
            Context.IsWorldReady,
            Context.IsWorldReady && Game1.player.IsLocalPlayer,
            Game1.IsMultiplayer,
            Game1.activeClickableMenu is not null,
            Game1.currentMinigame is not null,
            Game1.eventUp,
            Game1.isFestival());
        if (!OpenInventoryAfterStopPolicy.ShouldOpen(conditions))
            return false;

        Game1.activeClickableMenu = new GameMenu();
        monitor.Log(
            $"Opened the inventory after a {transition!.Reason} safety stop for local screen {Context.ScreenId}.",
            LogLevel.Info);
        return true;
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

    private void UpdateBiteWaitingTime(AutomationScreenState screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.BiteWaitingTimeRod = null;
            return;
        }

        if (!rod.IsFishing)
        {
            screen.BiteWaitingTimeRod = null;
            return;
        }

        bool alreadyApplied = ReferenceEquals(screen.BiteWaitingTimeRod, rod.Identity);

        int waitingTimePercent = getConfig().BiteWaitingTimePercent;
        InstantBiteDecision decision = InstantBitePolicy.Decide(
            rod.ReadInstantBiteConditions(waitingTimePercent, alreadyApplied));
        if (decision != InstantBiteDecision.ApplyWaitingTime)
            return;

        rod.ApplyBiteWaitingTime(waitingTimePercent);
        screen.BiteWaitingTimeRod = rod.Identity;
        monitor.Log(
            $"Applied {waitingTimePercent}% bite waiting time for local screen {Context.ScreenId}.",
            LogLevel.Trace);
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
        int requiredTicks = SecondsToTicks(config.CatchPopupDurationSeconds);
        switch (AutoClosePopupPolicy.Decide(conditions, screen.Pending.FishPopupVisibleTicks, requiredTicks))
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
            screen.Pending.SkippedBobberBar = null;
            return;
        }

        ModConfig config = getConfig();
        bar.ApplyLiveCatchModifiers(config);
        if (!ReferenceEquals(screen.Pending.ConfiguredBobberBar, bar.Identity))
        {
            (int vanillaBarHeight, int finalBarHeight) = bar.ApplyBarSizeAssistance(config);
            FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
            TreasureChanceDecision chance = TreasureChancePolicy.Decide(
                bar.ReadTreasureChanceConditions(config, rod));
            bar.ApplyTreasureChance(chance, rod);
            screen.Pending.ConfiguredBobberBar = bar.Identity;
            monitor.Log(
                $"Configured fishing minigame for local screen {Context.ScreenId}: " +
                $"assistance={config.MinigameAssistance}, bar={vanillaBarHeight}->{finalBarHeight}, " +
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
            bar.ReadSkipMinigameConditions(
                config.SkipFishingMiniGame,
                config.SkipMinigameCatchesRequired,
                perfectCatchProgress));
        if (decision != SkipMinigameDecision.Skip)
            return false;

        screen.Pending.SkippedBobberBar = bar.Identity;
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

        ModConfig config = getConfig();
        if (!ReferenceEquals(screen.TreasureMenuIdentity, menu.Identity))
        {
            this.ResetTreasureLoot(screen);
            screen.TreasureMenuIdentity = menu.Identity;
            screen.TreasureLootRequiredTicks = SecondsToTicks(config.TreasureLootDelaySeconds);
        }

        IReadOnlySet<string> ignoredItemIds = screen.GetTreasureChestIgnoreIds(config);
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
                this.ContinueAfterFullInventoryResolution(screen, "hud.treasure_full.drop", "dropped");
                break;
            case TreasureLootDecision.DiscardBlocked:
                menu.DiscardBlockedItems(screen.BlockedTreasureItems, ignoredItemIds);
                this.ResolveIgnoredTreasureRemainder(menu, config.ActionIfOnlyIgnoredTreasureRemains);
                this.ContinueAfterFullInventoryResolution(screen, "hud.treasure_full.discard", "discarded");
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
        string message = translate(messageKey);
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
        activityLog?.Add(message, severity: ActivityLogSeverity.Error);
        if (screen.Session.IsEnabled)
        {
            AutomationTransition transition = screen.Session.Toggle();
            this.Log(transition);
        }

        monitor.Log($"Stopped fishing automation for local screen {Context.ScreenId} because the inventory " +
                    "couldn't accept the remaining treasure.", LogLevel.Warn);
    }

    private void ContinueAfterFullInventoryResolution(
        AutomationScreenState screen,
        string messageKey,
        string action)
    {
        string message = translate(messageKey);
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
        activityLog?.Add(message, severity: ActivityLogSeverity.Warning);
        this.ResetTreasureLoot(screen);
        monitor.Log(
            $"Fishing automation continued after treasure that could not fit was {action} for local screen "
            + $"{Context.ScreenId}.",
            LogLevel.Info);
    }

    private void ResetTreasureLoot(AutomationScreenState screen)
    {
        screen.ResetTreasureLoot();
    }

    private static int SecondsToTicks(float seconds)
    {
        return (int)Math.Ceiling(Math.Max(0f, seconds) * 60f);
    }
}
