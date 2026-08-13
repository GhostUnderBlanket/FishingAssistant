using FishingAssistant.Configuration;
using FishingAssistant.Runtime;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace FishingAssistant.Inventory;

internal sealed class AutoEatService(IMonitor monitor, Func<string, string> translate)
{
    private const int RetryDelayTicks = 60;
    private readonly PerScreen<ScreenState> screens = new(() => new ScreenState());

    public void UpdateCurrent(ModConfig config, AutomationSession session)
    {
        ScreenState screen = this.screens.Value;
        if (screen.RetryTicks > 0)
        {
            screen.RetryTicks--;
            return;
        }

        Farmer? player = Context.IsWorldReady ? Game1.player : null;
        FishingRod? rod = player?.CurrentTool as FishingRod;
        bool isSafeToEat = this.IsSafeToEat(player, rod);
        bool shouldInspectInventory = config.AutoEatFood
            && session.IsEnabled
            && isSafeToEat
            && player is { MaxStamina: > 0 }
            && player.Stamina <= player.MaxStamina * config.EnergyPercentToEat / 100f;
        AutoEatConditions conditions = new(
            config.AutoEatFood,
            session.IsEnabled,
            isSafeToEat,
            player?.Stamina ?? 0f,
            player?.MaxStamina ?? 0f,
            config.EnergyPercentToEat,
            config.AllowEatingFish,
            shouldInspectInventory ? this.GetCandidates(player!) : []
        );
        AutoEatDecision decision = AutoEatPolicy.Decide(conditions);
        if (decision.Action != AutoEatAction.Eat || player is null)
        {
            if (shouldInspectInventory)
                screen.RetryTicks = RetryDelayTicks;
            return;
        }

        if (!this.TryEat(player, decision.InventoryIndex))
            screen.RetryTicks = RetryDelayTicks;
    }

    public void ResetCurrent()
    {
        this.screens.Value.RetryTicks = 0;
    }

    public void ResetAll()
    {
        this.screens.ResetAllScreens();
    }

    private bool IsSafeToEat(Farmer? player, FishingRod? rod)
    {
        return player is { IsLocalPlayer: true, isEating: false, CanMove: true }
            && rod is not null
            && !rod.inUse()
            && Context.IsPlayerFree
            && Game1.activeClickableMenu is null
            && Game1.currentMinigame is null
            && !Game1.eventUp
            && !Game1.isFestival()
            && !Game1.fadeToBlack;
    }

    private IReadOnlyList<FoodInventoryCandidate> GetCandidates(Farmer player)
    {
        List<FoodInventoryCandidate> candidates = [];
        for (int index = 0; index < player.Items.Count; index++)
        {
            if (player.Items[index] is not SObject { Stack: > 0, Edibility: > 0 } item)
                continue;

            bool isDrink = Game1.objectData.TryGetValue(item.ItemId, out var data) && data.IsDrink;
            candidates.Add(new FoodInventoryCandidate(
                index,
                item.QualifiedItemId,
                item.staminaRecoveredOnConsumption(),
                item.salePrice(),
                item.Category == SObject.FishCategory,
                item.questItem.Value || item.QualifiedItemId == "(O)434",
                item.GetFoodOrDrinkBuffs().Any(),
                player.hasBuff(isDrink ? "7" : "6")
            ));
        }

        return candidates;
    }

    private bool TryEat(Farmer player, int inventoryIndex)
    {
        if (inventoryIndex < 0
            || inventoryIndex >= player.Items.Count
            || player.Items[inventoryIndex] is not SObject { Stack: > 0, Edibility: > 0 } food)
        {
            return false;
        }

        int originalStack = food.Stack;
        player.eatObject(food);
        if (!player.isEating)
        {
            monitor.Log($"The game declined automatic consumption of {food.DisplayName}.", LogLevel.Debug);
            return false;
        }

        food.Stack--;
        if (food.Stack <= 0)
            player.removeItemFromInventory(food);

        Game1.addHUDMessage(new HUDMessage(string.Format(
            translate("hud.food.ate"), food.DisplayName, food.staminaRecoveredOnConsumption())));
        monitor.Log(
            $"Automatically ate {food.DisplayName} from local screen {Context.ScreenId} " +
            $"(stack {originalStack} -> {Math.Max(0, food.Stack)}).",
            LogLevel.Trace);
        return true;
    }

    private sealed class ScreenState
    {
        public int RetryTicks { get; set; }
    }
}
