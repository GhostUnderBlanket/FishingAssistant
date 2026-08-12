using FishingAssistant.Configuration;
using FishingAssistant.Fishing;
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
    private readonly PerScreen<ScreenContext> screens = new(() => new ScreenContext());

    public AutomationSession Current => this.screens.Value.Session;

    public void UpdateCurrent()
    {
        if (!Context.IsWorldReady)
            return;

        ScreenContext screen = this.screens.Value;
        Tool? currentTool = Game1.player.CurrentTool;
        if (screen.HasObservedTool && !ReferenceEquals(screen.LastTool, currentTool))
            this.Log(screen.Session.Reset(AutomationTransitionReason.ToolChanged));

        screen.LastTool = currentTool;
        screen.HasObservedTool = true;
        this.Log(screen.Session.Observe(FishingContextReader.Read(screen.Session.IsEnabled)));
        this.UpdateInstantBite();
        this.UpdateAutomaticMinigame(screen);
        this.UpdateAutomaticCatchPopup(screen);
        this.UpdateAutomaticTreasureLoot(screen);
        this.UpdateAutomaticHook(screen);
        this.UpdateAutomaticCast(screen);
    }

    public void ToggleCurrent()
    {
        AutomationTransition transition = this.screens.Value.Session.Toggle();
        monitor.Log(
            $"Automation {(this.Current.IsEnabled ? "enabled" : "disabled")} for local screen {Context.ScreenId}.",
            LogLevel.Info);
        this.Log(transition);
    }

    public void ToggleTreasureTargetingCurrent()
    {
        ScreenContext screen = this.screens.Value;
        bool enabled = screen.Session.ToggleTreasureTargeting();
        if (!enabled)
            screen.IsPursuingTreasure = false;
        monitor.Log(
            $"Treasure targeting {(enabled ? "enabled" : "disabled")} for local screen {Context.ScreenId}.",
            LogLevel.Info);
    }

    public void ResetCurrent(AutomationTransitionReason reason)
    {
        ScreenContext screen = this.screens.Value;
        screen.HasObservedTool = false;
        screen.LastTool = null;
        screen.ReadyTicks = 0;
        screen.AutomaticCastInProgress = false;
        screen.HookAttemptedForNibble = false;
        screen.IsPursuingTreasure = false;
        screen.ConfiguredBobberBar = null;
        screen.FishPopupVisibleTicks = 0;
        screen.FishPopupCloseAttempted = false;
        this.ResetTreasureLoot(screen);
        this.Log(screen.Session.Reset(reason));
    }

    public void ResetAll(AutomationTransitionReason reason)
    {
        foreach (AutomationSession session in this.screens.GetActiveValues().Select(pair => pair.Value.Session))
            session.Reset(reason);
        this.screens.ResetAllScreens();
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

    private void UpdateAutomaticCast(ScreenContext screen)
    {
        ModConfig config = getConfig();
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.ReadyTicks = 0;
            screen.AutomaticCastInProgress = false;
            return;
        }

        if (screen.AutomaticCastInProgress)
        {
            if (rod.IsTimingCast)
                rod.SetCastPower(config.DefaultCastPower);

            if (screen.Session.State is AutomationState.Ready or AutomationState.Casting)
            {
                screen.ReadyTicks = 0;
                return;
            }

            screen.AutomaticCastInProgress = false;
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
        switch (AutoCastPolicy.Decide(conditions, screen.ReadyTicks, requiredTicks))
        {
            case AutoCastDecision.Reset:
                screen.ReadyTicks = 0;
                break;
            case AutoCastDecision.Wait:
                screen.ReadyTicks++;
                break;
            case AutoCastDecision.Cast:
                screen.ReadyTicks = 0;
                screen.AutomaticCastInProgress = true;
                rod.BeginAutomaticCast(config.DefaultCastPower);
                monitor.Log($"Started an automatic cast for local screen {Context.ScreenId}.", LogLevel.Trace);
                break;
        }
    }

    private void UpdateAutomaticHook(ScreenContext screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.HookAttemptedForNibble = false;
            return;
        }

        ModConfig config = getConfig();
        AutoHookConditions conditions = rod.ReadAutoHookConditions(
            screen.Session.IsEnabled,
            config.AutoHookFish,
            screen.Session.State,
            screen.HookAttemptedForNibble
        );
        switch (AutoHookPolicy.Decide(conditions))
        {
            case AutoHookDecision.ResetAttempt:
                screen.HookAttemptedForNibble = false;
                break;
            case AutoHookDecision.Wait:
                break;
            case AutoHookDecision.Hook:
                screen.HookAttemptedForNibble = true;
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

    private void UpdateAutomaticCatchPopup(ScreenContext screen)
    {
        FishingRodAdapter? rod = FishingRodAdapter.ForCurrentPlayer();
        if (rod is null)
        {
            screen.FishPopupVisibleTicks = 0;
            screen.FishPopupCloseAttempted = false;
            return;
        }

        ModConfig config = getConfig();
        AutoClosePopupConditions conditions = rod.ReadAutoClosePopupConditions(
            screen.Session.IsEnabled,
            config.AutoClosePopup,
            screen.Session.State,
            screen.FishPopupCloseAttempted
        );
        switch (AutoClosePopupPolicy.Decide(conditions, screen.FishPopupVisibleTicks))
        {
            case AutoClosePopupDecision.Reset:
                screen.FishPopupVisibleTicks = 0;
                screen.FishPopupCloseAttempted = false;
                break;
            case AutoClosePopupDecision.Wait:
                if (conditions.IsEligible)
                    screen.FishPopupVisibleTicks++;
                break;
            case AutoClosePopupDecision.Close:
                screen.FishPopupCloseAttempted = true;
                rod.CloseFishPopup();
                monitor.Log($"Closed the catch popup automatically for local screen {Context.ScreenId}.",
                    LogLevel.Trace);
                break;
        }
    }

    private void UpdateAutomaticMinigame(ScreenContext screen)
    {
        BobberBarAdapter? bar = BobberBarAdapter.ForCurrentScreen();
        if (bar is null)
        {
            screen.IsPursuingTreasure = false;
            screen.ConfiguredBobberBar = null;
            return;
        }

        ModConfig config = getConfig();
        if (!ReferenceEquals(screen.ConfiguredBobberBar, bar.Identity))
        {
            TreasureChanceDecision chance = TreasureChancePolicy.Decide(
                bar.ReadTreasureChanceConditions(config));
            bar.ApplyTreasureChance(chance);
            screen.ConfiguredBobberBar = bar.Identity;
            monitor.Log(
                $"Applied treasure chance for local screen {Context.ScreenId}: " +
                $"treasure={chance.HasTreasure}, golden={chance.IsGoldenTreasure}.",
                LogLevel.Trace);
        }

        bool assistanceActive = screen.Session.IsEnabled
            && config.AutoPlayMiniGame
            && screen.Session.State == AutomationState.Minigame;
        TreasureTargetDecision target = TreasureTargetPolicy.Decide(bar.ReadTreasureConditions(
            assistanceActive,
            screen.Session.IsTreasureTargetingEnabled,
            screen.IsPursuingTreasure
        ));
        screen.IsPursuingTreasure = target.IsTargetingTreasure;
        MinigameControlDecision decision = MinigameControlPolicy.Decide(bar.ReadConditions(
            screen.Session.IsEnabled,
            config.AutoPlayMiniGame,
            screen.Session.State,
            target.Position
        ));
        if (decision.ShouldControl)
            bar.SetBarSpeed(decision.BarSpeed);
    }

    private void UpdateAutomaticTreasureLoot(ScreenContext screen)
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
        TreasureLootConditions conditions = new(
            screen.Session.IsEnabled,
            config.AutoLootTreasure,
            IsFishingTreasureMenu: true,
            menu.IsPlayerHoldingItem,
            screen.TreasureCollectionStopped,
            menu.HasRemainingItems,
            menu.HasUnblockedItem(screen.BlockedTreasureItems),
            config.ActionIfInventoryFull
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
                this.CollectNextTreasureItem(screen, menu);
                break;
            case TreasureLootDecision.Close:
                menu.Close();
                this.ResetTreasureLoot(screen);
                break;
            case TreasureLootDecision.Stop:
                this.StopForFullInventory(screen, "hud.treasure_full.stop");
                break;
            case TreasureLootDecision.Drop:
                menu.DropRemainingItems();
                this.StopForFullInventory(screen, "hud.treasure_full.drop");
                break;
            case TreasureLootDecision.Discard:
                menu.DiscardRemainingItems();
                this.StopForFullInventory(screen, "hud.treasure_full.discard");
                break;
        }
    }

    private void CollectNextTreasureItem(ScreenContext screen, FishingTreasureMenuAdapter menu)
    {
        TreasureCollectResult result = menu.TryCollectNext(screen.BlockedTreasureItems);
        screen.TreasureLootElapsedTicks = 0;
        screen.TreasureLootRequiredTicks = TreasureLootPolicy.ItemDelayTicks;
        if (result is TreasureCollectResult.Collected or TreasureCollectResult.PartiallyCollected)
        {
            monitor.Log($"Collected fishing treasure for local screen {Context.ScreenId} ({result}).",
                LogLevel.Trace);
        }
    }

    private void StopForFullInventory(ScreenContext screen, string messageKey)
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

    private void ResetTreasureLoot(ScreenContext screen)
    {
        screen.TreasureMenuIdentity = null;
        screen.TreasureLootElapsedTicks = 0;
        screen.TreasureLootRequiredTicks = TreasureLootPolicy.InitialDelayTicks;
        screen.TreasureCollectionStopped = false;
        screen.BlockedTreasureItems.Clear();
    }

    private sealed class ScreenContext
    {
        public AutomationSession Session { get; } = new();

        public Tool? LastTool { get; set; }

        public bool HasObservedTool { get; set; }

        public int ReadyTicks { get; set; }

        public bool AutomaticCastInProgress { get; set; }

        public bool HookAttemptedForNibble { get; set; }

        public bool IsPursuingTreasure { get; set; }

        public object? ConfiguredBobberBar { get; set; }

        public int FishPopupVisibleTicks { get; set; }

        public bool FishPopupCloseAttempted { get; set; }

        public object? TreasureMenuIdentity { get; set; }

        public int TreasureLootElapsedTicks { get; set; }

        public int TreasureLootRequiredTicks { get; set; } = TreasureLootPolicy.InitialDelayTicks;

        public bool TreasureCollectionStopped { get; set; }

        public HashSet<Item> BlockedTreasureItems { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
