using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishingAssistant.Fishing;

/// <summary>
/// Records the actual catch instances that vanilla places in a fishing chest menu.
/// This deliberately tracks provenance instead of inferring catches from item category,
/// so non-fish aquatic catches and items supplied by content mods remain safe.
/// </summary>
internal static class FishingTreasureProtectionPatch
{
    private static readonly PerScreen<ScreenState> Screens = new(() => new ScreenState());
    private static IMonitor? monitor;

    public static void Apply(Harmony harmony, IMonitor modMonitor)
    {
        ArgumentNullException.ThrowIfNull(harmony);
        monitor = modMonitor ?? throw new ArgumentNullException(nameof(modMonitor));

        MethodInfo? createFish = AccessTools.Method(typeof(FishingRod), "CreateFish");
        MethodInfo? openTreasureMenu = AccessTools.Method(
            typeof(FishingRod),
            nameof(FishingRod.openTreasureMenuEndFunction));
        MethodInfo? openDerbyMenu = AccessTools.Method(
            typeof(FishingRod),
            nameof(FishingRod.justGotDerbyTagEndFunction));
        if (createFish is null || openTreasureMenu is null || openDerbyMenu is null)
        {
            monitor.Log(
                "Fishing catch protection could not find the vanilla fishing-menu methods. "
                + "Automatic treasure Drop and Discard will retain safety-critical items, but catch provenance "
                + "tracking is unavailable.",
                LogLevel.Warn);
            return;
        }

        HarmonyMethod beginCapture = new(typeof(FishingTreasureProtectionPatch), nameof(BeforeFishingMenuCreated))
        {
            priority = Priority.Last
        };
        HarmonyMethod finishCapture = new(typeof(FishingTreasureProtectionPatch), nameof(AfterFishingMenuCreated))
        {
            priority = Priority.Last
        };
        harmony.Patch(openTreasureMenu, prefix: beginCapture, postfix: finishCapture);
        harmony.Patch(openDerbyMenu, prefix: beginCapture, postfix: finishCapture);
        harmony.Patch(
            createFish,
            postfix: new HarmonyMethod(typeof(FishingTreasureProtectionPatch), nameof(AfterCatchCreated))
            {
                priority = Priority.Last
            });
    }

    public static void CopyProtectedItems(ItemGrabMenu menu, ISet<Item> destination)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(destination);

        ScreenState screen = Screens.Value;
        if (ReferenceEquals(screen.Menu, menu))
        {
            foreach (Item item in screen.ProtectedItems)
                destination.Add(item);
        }

        // Keep the safety boundary useful if another mod replaces vanilla's menu
        // after the provenance patch runs, and protect event/quest items which are
        // rewards rather than the caught item itself.
        foreach (Item? item in menu.ItemsToGrabMenu.actualInventory)
        {
            if (item is not null && FishingTreasureProtectionPolicy.IsSafetyCritical(item))
                destination.Add(item);
        }
    }

    private static void BeforeFishingMenuCreated(
        FishingRod __instance,
        MethodBase __originalMethod,
        ref int remainingFish,
        out CaptureScope? __state)
    {
        __state = null;
        if (__instance.lastUser is not { IsLocalPlayer: true })
            return;

        ScreenState screen = Screens.Value;
        screen.BeginCapture(__instance, Math.Max(0, remainingFish));
        __state = new CaptureScope(screen, __originalMethod.Name);

        // Stardew Valley 1.6.15 only recreates the caught stack when this argument
        // equals one. Normalize the signal for multi-catches; the CreateFish postfix
        // restores the exact remainder before vanilla handles that item.
        if (remainingFish > 0)
            remainingFish = 1;
    }

    private static void AfterCatchCreated(FishingRod __instance, Item __result)
    {
        ScreenState screen = Screens.Value;
        if (screen.IsCapturing
            && ReferenceEquals(screen.Rod, __instance)
            && !screen.HasCapturedCatch
            && screen.RemainingFish > 0
            && __result is not null)
        {
            // CreateFish normally recreates the entire multi-catch stack. The menu
            // must receive only the portion which failed to enter the inventory.
            __result.Stack = screen.RemainingFish;
            screen.CreatedItems.Add(__result);
            screen.HasCapturedCatch = true;
        }
    }

    private static void AfterFishingMenuCreated(FishingRod __instance, CaptureScope? __state)
    {
        if (__state is null)
            return;

        try
        {
            ScreenState screen = __state.Screen;
            if (Game1.activeClickableMenu is not ItemGrabMenu
                {
                    source: ItemGrabMenu.source_fishingChest,
                    context: FishingRod contextRod
                } menu
                || !ReferenceEquals(contextRod, __instance))
            {
                return;
            }

            screen.Menu = menu;
            screen.ProtectedItems.Clear();
            IList<Item> menuItems = menu.ItemsToGrabMenu.actualInventory;
            Item? caughtRemainder = screen.CreatedItems.FirstOrDefault(menuItems.Contains);
            if (caughtRemainder is not null)
            {
                screen.ProtectedItems.Add(caughtRemainder);
            }

            foreach (Item? item in menuItems)
            {
                if (item is not null && FishingTreasureProtectionPolicy.IsSafetyCritical(item))
                    screen.ProtectedItems.Add(item);
            }

            monitor?.Log(
                $"Protected {screen.ProtectedItems.Count} caught or safety-critical fishing item(s) in "
                + $"{__state.SourceMethod} for local screen {Context.ScreenId}.",
                LogLevel.Trace);
        }
        catch (Exception exception)
        {
            monitor?.Log(
                "Fishing catch protection could not inspect the newly created fishing menu. "
                + $"Safety-critical items will still be recognized when the menu is processed.\n{exception}",
                LogLevel.Error);
        }
        finally
        {
            __state.Screen.EndCapture();
        }
    }

    private sealed class CaptureScope(ScreenState screen, string sourceMethod)
    {
        public ScreenState Screen { get; } = screen;

        public string SourceMethod { get; } = sourceMethod;
    }

    private sealed class ScreenState
    {
        public bool IsCapturing { get; private set; }

        public FishingRod? Rod { get; private set; }

        public int RemainingFish { get; private set; }

        public bool HasCapturedCatch { get; set; }

        public List<Item> CreatedItems { get; } = [];

        public ItemGrabMenu? Menu { get; set; }

        public HashSet<Item> ProtectedItems { get; } = new(ReferenceEqualityComparer.Instance);

        public void BeginCapture(FishingRod rod, int remainingFish)
        {
            this.IsCapturing = true;
            this.Rod = rod;
            this.RemainingFish = remainingFish;
            this.HasCapturedCatch = false;
            this.CreatedItems.Clear();
            this.Menu = null;
            this.ProtectedItems.Clear();
        }

        public void EndCapture()
        {
            this.IsCapturing = false;
            this.Rod = null;
            this.RemainingFish = 0;
            this.HasCapturedCatch = false;
            this.CreatedItems.Clear();
        }
    }
}
