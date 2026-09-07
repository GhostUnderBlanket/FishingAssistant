using FishingAssistant.Configuration;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;

namespace FishingAssistant.Equipment;

internal sealed class RodEnchantmentService(IMonitor monitor, Func<string, string> translate)
{
    private readonly PerScreen<ScreenState> screens = new(() => new ScreenState());

    public void UpdateCurrent(ModConfig config)
    {
        if (!Context.IsWorldReady)
            return;

        ScreenState screen = this.screens.Value;
        screen.ScreenId = Context.ScreenId;
        screen.Player = Game1.player;
        screen.CurrentRod = Game1.player.CurrentTool as FishingRod;
        if (screen.SuspendedForSave)
            return;

        IReadOnlySet<RodEnchantmentKind> requested = GetRequested(config);
        if (Context.HasRemotePlayers)
        {
            this.RemoveAllManaged(screen);
            if (requested.Count == 0)
                screen.MultiplayerWarningShown = false;
            if (requested.Count > 0 && !screen.MultiplayerWarningShown)
            {
                screen.MultiplayerWarningShown = true;
                Game1.addHUDMessage(new HUDMessage(
                    translate("hud.enchantment.remote_unsupported"), HUDMessage.error_type));
                monitor.Log(
                    $"Temporary rod enchantments are disabled for local screen {screen.ScreenId} " +
                    "while remote players are connected, to prevent save synchronization leaks.",
                    LogLevel.Warn);
            }
            return;
        }

        screen.MultiplayerWarningShown = false;
        this.SynchronizeTrackedRods(screen, config, requested);
        if (screen.CurrentRod is not null && !screen.Rods.ContainsKey(screen.CurrentRod))
        {
            RodState state = new(screen.CurrentRod);
            screen.Rods.Add(screen.CurrentRod, state);
            this.SynchronizeRod(screen, state, config, requested, isEquipped: true);
        }
    }

    public void SuspendAllForSave()
    {
        foreach (ScreenState screen in this.screens.GetActiveValues().Select(pair => pair.Value))
        {
            screen.SuspendedForSave = true;
            foreach (RodState rod in screen.Rods.Values)
            {
                rod.SuspendedKinds.UnionWith(rod.Managed.Keys);
                this.RemoveManaged(screen, rod, [.. rod.Managed.Keys]);
            }
        }
    }

    public void ResumeAllAfterSave(ModConfig config)
    {
        IReadOnlySet<RodEnchantmentKind> requested = GetRequested(config);
        foreach (ScreenState screen in this.screens.GetActiveValues().Select(pair => pair.Value))
        {
            screen.SuspendedForSave = false;
            if (Context.HasRemotePlayers)
            {
                foreach (RodState rod in screen.Rods.Values)
                    rod.SuspendedKinds.Clear();
                continue;
            }

            foreach (RodState rod in screen.Rods.Values.ToList())
            {
                bool isEquipped = ReferenceEquals(screen.CurrentRod, rod.Rod);
                foreach (RodEnchantmentKind kind in rod.SuspendedKinds.ToList())
                {
                    if (requested.Contains(kind) && (isEquipped || !config.RemoveWhenUnequipped))
                        this.TryAdd(screen, rod, kind, isEquipped);
                }
                rod.SuspendedKinds.Clear();
                if (rod.Managed.Count == 0 && !isEquipped)
                    screen.Rods.Remove(rod.Rod);
            }
        }
    }

    public void RemoveAllForRemoteConnection()
    {
        foreach (ScreenState screen in this.screens.GetActiveValues().Select(pair => pair.Value))
            this.RemoveAllManaged(screen);
    }

    public void RemoveAllAndReset()
    {
        foreach (ScreenState screen in this.screens.GetActiveValues().Select(pair => pair.Value))
            this.RemoveAllManaged(screen);
        this.screens.ResetAllScreens();
    }

    public void ResetAll()
    {
        this.screens.ResetAllScreens();
    }

    private static IReadOnlySet<RodEnchantmentKind> GetRequested(ModConfig config)
    {
        HashSet<RodEnchantmentKind> requested = [];
        if (config.AddAutoHookEnchantment)
            requested.Add(RodEnchantmentKind.AutoHook);
        if (config.AddEfficientEnchantment)
            requested.Add(RodEnchantmentKind.Efficient);
        if (config.AddMasterEnchantment)
            requested.Add(RodEnchantmentKind.Master);
        if (config.AddPreservingEnchantment)
            requested.Add(RodEnchantmentKind.Preserving);
        return requested;
    }

    private void SynchronizeTrackedRods(
        ScreenState screen,
        ModConfig config,
        IReadOnlySet<RodEnchantmentKind> requested)
    {
        foreach (RodState rod in screen.Rods.Values.ToList())
        {
            bool isEquipped = ReferenceEquals(screen.CurrentRod, rod.Rod);
            this.SynchronizeRod(screen, rod, config, requested, isEquipped);
            if (rod.Managed.Count == 0 && rod.SuspendedKinds.Count == 0 && !isEquipped)
                screen.Rods.Remove(rod.Rod);
        }
    }

    private void SynchronizeRod(
        ScreenState screen,
        RodState rod,
        ModConfig config,
        IReadOnlySet<RodEnchantmentKind> requested,
        bool isEquipped)
    {
        foreach ((RodEnchantmentKind kind, BaseEnchantment enchantment) in rod.Managed.ToList())
        {
            if (!rod.Rod.enchantments.Contains(enchantment))
                rod.Managed.Remove(kind);
        }

        HashSet<RodEnchantmentKind> existing = rod.Rod.enchantments
            .Select(GetKind)
            .Where(kind => kind is not null)
            .Select(kind => kind!.Value)
            .ToHashSet();
        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(new RodEnchantmentConditions(
            Context.HasRemotePlayers,
            isEquipped,
            config.RemoveWhenUnequipped,
            requested,
            existing,
            rod.Managed.Keys.ToHashSet()
        ));
        this.RemoveManaged(screen, rod, decision.Remove);
        foreach (RodEnchantmentKind kind in decision.Add)
            this.TryAdd(screen, rod, kind, isEquipped);
    }

    private void TryAdd(ScreenState screen, RodState rod, RodEnchantmentKind kind, bool isEquipped)
    {
        if (rod.Managed.ContainsKey(kind)
            || rod.Rod.enchantments.Any(enchantment => GetKind(enchantment) == kind))
        {
            return;
        }

        BaseEnchantment enchantment = Create(kind);
        if (!enchantment.CanApplyTo(rod.Rod))
            return;

        rod.Rod.enchantments.Add(enchantment);
        enchantment.ApplyTo(rod.Rod, isEquipped ? screen.Player : null);
        rod.Managed.Add(kind, enchantment);
        monitor.Log(
            $"Temporarily added {kind} to {rod.Rod.DisplayName} for local screen {screen.ScreenId}.",
            LogLevel.Trace);
    }

    private void RemoveManaged(
        ScreenState screen,
        RodState rod,
        IReadOnlyCollection<RodEnchantmentKind> kinds)
    {
        foreach (RodEnchantmentKind kind in kinds)
        {
            if (!rod.Managed.Remove(kind, out BaseEnchantment? enchantment))
                continue;

            if (rod.Rod.enchantments.Remove(enchantment))
                enchantment.UnapplyTo(rod.Rod, ReferenceEquals(screen.CurrentRod, rod.Rod) ? screen.Player : null);
            monitor.Log(
                $"Removed temporary {kind} from {rod.Rod.DisplayName} for local screen {screen.ScreenId}.",
                LogLevel.Trace);
        }
    }

    private void RemoveAllManaged(ScreenState screen)
    {
        foreach (RodState rod in screen.Rods.Values)
        {
            this.RemoveManaged(screen, rod, [.. rod.Managed.Keys]);
            rod.SuspendedKinds.Clear();
        }
        screen.Rods.Clear();
        screen.CurrentRod = null;
        screen.SuspendedForSave = false;
    }

    private static RodEnchantmentKind? GetKind(BaseEnchantment enchantment)
    {
        return enchantment switch
        {
            AutoHookEnchantment => RodEnchantmentKind.AutoHook,
            EfficientToolEnchantment => RodEnchantmentKind.Efficient,
            MasterEnchantment => RodEnchantmentKind.Master,
            PreservingEnchantment => RodEnchantmentKind.Preserving,
            _ => null
        };
    }

    private static BaseEnchantment Create(RodEnchantmentKind kind)
    {
        return kind switch
        {
            RodEnchantmentKind.AutoHook => new AutoHookEnchantment(),
            RodEnchantmentKind.Efficient => new EfficientToolEnchantment(),
            RodEnchantmentKind.Master => new MasterEnchantment(),
            RodEnchantmentKind.Preserving => new PreservingEnchantment(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private sealed class ScreenState
    {
        public Dictionary<FishingRod, RodState> Rods { get; } = new(ReferenceEqualityComparer.Instance);

        public int ScreenId { get; set; }

        public Farmer? Player { get; set; }

        public FishingRod? CurrentRod { get; set; }

        public bool SuspendedForSave { get; set; }

        public bool MultiplayerWarningShown { get; set; }
    }

    private sealed class RodState(FishingRod rod)
    {
        public FishingRod Rod { get; } = rod;

        public Dictionary<RodEnchantmentKind, BaseEnchantment> Managed { get; } = [];

        public HashSet<RodEnchantmentKind> SuspendedKinds { get; } = [];
    }
}
