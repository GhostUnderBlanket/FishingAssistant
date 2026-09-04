using FishingAssistant.Configuration;
using FishingAssistant.UI;
using FishingAssistant.Fishing;
using FishingAssistant.Runtime;
using FishingAssistant.HUD;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace FishingAssistant.Inventory;

internal sealed class AutoEatService(
    IMonitor monitor,
    Func<string, string> translate,
    ActivityLogService? activityLog = null)
{
    private const int RetryDelayTicks = 60;
    private readonly PerScreen<ScreenState> screens = new(() => new ScreenState());

    public bool UpdateCurrent(ModConfig config, AutomationSession session)
    {
        ScreenState screen = this.screens.Value;
        this.RestoreFacingDirectionAfterEating(screen);
        if (!ReferenceEquals(screen.Config, config))
        {
            screen.Config = config;
            screen.ResetRecoveryCycle();
        }

        Farmer? player = Context.IsWorldReady ? Game1.player : null;
        FishingRod? rod = player?.CurrentTool as FishingRod;
        FishingRodAdapter? rodAdapter = FishingRodAdapter.ForCurrentPlayer();
        bool isSafeToEat = this.IsSafeToEat(player, rod);
        float stamina = player?.Stamina ?? 0f;
        float maxStamina = player?.MaxStamina ?? 0f;
        float castStaminaCost = rodAdapter?.CastStaminaCost ?? 0f;
        bool castConsumesStamina = rodAdapter?.CastConsumesStamina ?? false;
        float targetStamina = Math.Max(
            maxStamina * config.EnergyPercentToEat / 100f,
            config.AutoEatTrigger == AutoEatTriggerBehavior.BeforeNextCast
                ? castStaminaCost + 0.01f
                : 0f);
        if (!config.AutoEatFood
            || !session.IsEnabled
            || rodAdapter is null)
        {
            screen.ResetRecoveryCycle();
        }
        else if (stamina >= targetStamina)
        {
            screen.ResetRecoveryCycle();
        }

        if (screen.RetryTicks > 0)
        {
            screen.RetryTicks--;
            return screen.IsRecoveryCycleActive;
        }

        if (player?.isEating == true)
            return true;

        if (screen.PendingFood is not null)
        {
            if (!screen.IsRecoveryCycleActive || !isSafeToEat)
            {
                screen.EndRecoveryCycleUntilRearmed();
                return false;
            }

            if (screen.FoodDelayTicksRemaining > 0)
            {
                screen.FoodDelayTicksRemaining--;
                if (screen.FoodDelayTicksRemaining > 0)
                    return true;
            }

            Item pendingFood = screen.PendingFood;
            int pendingIndex = player?.Items.IndexOf(pendingFood) ?? -1;
            screen.ClearPendingFood();
            if (player is null || pendingIndex < 0 || !screen.CanConsume(pendingFood))
            {
                screen.EndRecoveryCycleUntilRearmed();
                return false;
            }

            int pendingFacingDirection = player.FacingDirection;
            if (!this.TryEat(player, pendingIndex))
            {
                player.faceDirection(pendingFacingDirection);
                screen.RetryTicks = RetryDelayTicks;
                return true;
            }

            screen.RecordConsumed(pendingFood);
            screen.EatingPlayer = player;
            screen.FacingDirectionToRestore = pendingFacingDirection;
            return true;
        }

        bool triggerReached = config.AutoEatTrigger switch
        {
            AutoEatTriggerBehavior.AtEnergyTarget => stamina < targetStamina,
            AutoEatTriggerBehavior.BeforeNextCast => castConsumesStamina && stamina <= castStaminaCost,
            _ => false
        };
        if (config.AutoEatFood
            && session.IsEnabled
            && isSafeToEat
            && triggerReached
            && !screen.IsRecoveryCycleActive
            && !screen.IsSuppressedUntilRearmed
            && player is not null)
        {
            screen.StartRecoveryCycle(player);
        }

        bool shouldInspectInventory = config.AutoEatFood
            && session.IsEnabled
            && isSafeToEat
            && player is { MaxStamina: > 0 }
            && screen.IsRecoveryCycleActive;
        AutoEatConditions conditions = new(
            config.AutoEatFood,
            session.IsEnabled,
            isSafeToEat,
            stamina,
            maxStamina,
            config.EnergyPercentToEat,
            config.AutoEatTrigger,
            screen.IsRecoveryCycleActive,
            castConsumesStamina,
            castStaminaCost,
            config.AllowEatingFish,
            config.PreferredFoods,
            config.FoodFallback,
            shouldInspectInventory ? this.GetCandidates(player!, screen) : []
        );
        AutoEatDecision decision = AutoEatPolicy.Decide(conditions);
        if (decision.Action != AutoEatAction.Eat || player is null)
        {
            if (shouldInspectInventory)
            {
                screen.EndRecoveryCycleUntilRearmed();
                monitor.Log(
                    $"Automatic food recovery exhausted the inventory available at the start of its cycle " +
                    $"for local screen {Context.ScreenId}; newly acquired food will be ignored until rearmed.",
                    LogLevel.Trace);
            }
            return false;
        }

        int originalFacingDirection = player.FacingDirection;
        Item? consumedItem = decision.InventoryIndex >= 0 && decision.InventoryIndex < player.Items.Count
            ? player.Items[decision.InventoryIndex]
            : null;
        int delayTicks = Math.Max(0, (int)Math.Round(
            config.FoodConsumptionDelaySeconds * 60f,
            MidpointRounding.AwayFromZero));
        if (consumedItem is not null && delayTicks > 0)
        {
            screen.ScheduleFood(consumedItem, delayTicks);
            return true;
        }
        if (!this.TryEat(player, decision.InventoryIndex))
        {
            player.faceDirection(originalFacingDirection);
            screen.RetryTicks = RetryDelayTicks;
            return true;
        }

        if (consumedItem is not null)
            screen.RecordConsumed(consumedItem);
        screen.EatingPlayer = player;
        screen.FacingDirectionToRestore = originalFacingDirection;
        return true;
    }

    public void ResetCurrent()
    {
        this.screens.Value.RetryTicks = 0;
        this.screens.Value.Config = null;
        this.screens.Value.ResetRecoveryCycle();
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

    private IReadOnlyList<FoodInventoryCandidate> GetCandidates(Farmer player, ScreenState screen)
    {
        List<FoodInventoryCandidate> candidates = [];
        for (int index = 0; index < player.Items.Count; index++)
        {
            if (player.Items[index] is not SObject { Stack: > 0, Edibility: > 0 } item)
                continue;
            if (!screen.CanConsume(item))
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

        HudNotification.ShowItem(string.Format(
            translate("hud.food.ate"), food.DisplayName, food.staminaRecoveredOnConsumption()), food,
            activityLog);
        monitor.Log(
            $"Automatically ate {food.DisplayName} from local screen {Context.ScreenId} " +
            $"(stack {originalStack} -> {Math.Max(0, food.Stack)}).",
            LogLevel.Trace);
        return true;
    }

    private void RestoreFacingDirectionAfterEating(ScreenState screen)
    {
        if (screen.EatingPlayer is not Farmer player
            || screen.FacingDirectionToRestore is not int facingDirection
            || player.isEating)
        {
            return;
        }

        screen.EatingPlayer = null;
        screen.FacingDirectionToRestore = null;
        if (player.IsLocalPlayer
            && player.FacingDirection == Game1.down
            && facingDirection is >= Game1.up and <= Game1.left)
        {
            player.faceDirection(facingDirection);
        }
    }

    private sealed class ScreenState
    {
        private readonly Dictionary<Item, int> recoveryFood = new(ReferenceEqualityComparer.Instance);

        public ModConfig? Config { get; set; }

        public Farmer? EatingPlayer { get; set; }

        public int? FacingDirectionToRestore { get; set; }

        public int RetryTicks { get; set; }

        public Item? PendingFood { get; private set; }

        public int FoodDelayTicksRemaining { get; set; }

        public bool IsRecoveryCycleActive { get; private set; }

        public bool IsSuppressedUntilRearmed { get; private set; }

        public void StartRecoveryCycle(Farmer player)
        {
            this.recoveryFood.Clear();
            foreach (Item? item in player.Items)
            {
                if (item is SObject { Stack: > 0, Edibility: > 0 })
                    this.recoveryFood[item] = item.Stack;
            }

            this.IsRecoveryCycleActive = true;
            this.IsSuppressedUntilRearmed = false;
        }

        public bool CanConsume(Item item)
        {
            return this.recoveryFood.TryGetValue(item, out int remaining) && remaining > 0;
        }

        public void RecordConsumed(Item item)
        {
            if (!this.recoveryFood.TryGetValue(item, out int remaining))
                return;

            if (remaining <= 1)
                this.recoveryFood.Remove(item);
            else
                this.recoveryFood[item] = remaining - 1;
        }

        public void ScheduleFood(Item item, int delayTicks)
        {
            this.PendingFood = item;
            this.FoodDelayTicksRemaining = Math.Max(0, delayTicks);
        }

        public void ClearPendingFood()
        {
            this.PendingFood = null;
            this.FoodDelayTicksRemaining = 0;
        }

        public void EndRecoveryCycleUntilRearmed()
        {
            this.recoveryFood.Clear();
            this.ClearPendingFood();
            this.IsRecoveryCycleActive = false;
            this.IsSuppressedUntilRearmed = true;
            this.RetryTicks = 0;
        }

        public void ResetRecoveryCycle()
        {
            this.recoveryFood.Clear();
            this.ClearPendingFood();
            this.IsRecoveryCycleActive = false;
            this.IsSuppressedUntilRearmed = false;
            this.RetryTicks = 0;
        }
    }
}
