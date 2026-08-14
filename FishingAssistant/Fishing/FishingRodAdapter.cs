using FishingAssistant.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Minigames;
using StardewValley.Tools;

namespace FishingAssistant.Fishing;

internal sealed class FishingRodAdapter(Farmer player, FishingRod rod)
{
    public static FishingRodAdapter? ForCurrentPlayer()
    {
        return Context.IsWorldReady && Game1.player.CurrentTool is FishingRod rod
            ? new FishingRodAdapter(Game1.player, rod)
            : null;
    }

    public bool IsTimingCast => rod.isTimingCast;

    public bool IsBobberInAir => rod.castedButBobberStillInAir;

    public bool IsCastInProgress => IsCastInProgressFor(rod);

    public object Identity => rod;

    public Vector2 BobberPosition => rod.bobber.Value;

    public float CastingPower => Math.Clamp(rod.castingPower, 0f, 1f);

    internal static bool IsCastInProgressFor(FishingRod fishingRod)
    {
        ArgumentNullException.ThrowIfNull(fishingRod);
        return fishingRod.isTimingCast
            || fishingRod.isCasting
            || fishingRod.castedButBobberStillInAir;
    }

    public bool IsSupportedFishingMinigame => Game1.currentMinigame is FishingGame { gameDone: false };

    public AutoHookConditions ReadAutoHookConditions(
        bool automationEnabled,
        bool autoHookEnabled,
        AutomationState state,
        bool hookAlreadyAttempted)
    {
        return new AutoHookConditions(
            automationEnabled,
            autoHookEnabled,
            state,
            rod.isNibbling,
            hookAlreadyAttempted,
            rod.hasEnchantmentOfType<AutoHookEnchantment>(),
            Game1.activeClickableMenu is not null,
            Game1.isFestival(),
            this.IsSupportedFishingMinigame,
            rod is
            {
                hit: false,
                isReeling: false,
                pullingOutOfWater: false,
                fishCaught: false,
                showingTreasure: false
            }
        );
    }

    public AutoCastConditions ReadAutoCastConditions(
        bool automationEnabled,
        bool autoCastEnabled,
        AutomationState state,
        int castPower)
    {
        float staminaCost = this.GetCastStaminaCost();
        return new AutoCastConditions(
            automationEnabled,
            autoCastEnabled,
            state,
            Context.IsPlayerFree,
            player.CanMove,
            player.isMoving(),
            !this.DoesCastConsumeStamina() || player.Stamina > staminaCost,
            Game1.isFestival(),
            this.IsSupportedFishingMinigame,
            IsCastInputReleased(),
            this.IsCastTargetFishable(castPower)
        );
    }

    internal static bool IsCastInputReleased()
    {
        return Game1.areAllOfTheseKeysUp(Game1.GetKeyboardState(), Game1.options.useToolButton)
            && Game1.input.GetMouseState().LeftButton == ButtonState.Released
            && (!Game1.options.gamepadControls
                || Game1.input.GetGamePadState().IsButtonUp(Buttons.X));
    }

    public LowEnergyStopConditions ReadLowEnergyStopConditions(
        bool automationEnabled,
        bool autoCastEnabled,
        AutomationState state,
        bool autoEatEnabled,
        int eatingThresholdPercent)
    {
        return new LowEnergyStopConditions(
            automationEnabled,
            autoCastEnabled,
            state,
            this.DoesCastConsumeStamina(),
            player.isEating,
            player.Stamina,
            player.MaxStamina,
            this.GetCastStaminaCost(),
            autoEatEnabled,
            eatingThresholdPercent
        );
    }

    public AutoClosePopupConditions ReadAutoClosePopupConditions(
        bool automationEnabled,
        bool autoClosePopupEnabled,
        AutomationState state,
        bool closeAlreadyAttempted)
    {
        bool isPopupVisible = player.IsLocalPlayer
            && rod.fishCaught
            && rod.inUse()
            && rod is
            {
                isCasting: false,
                isTimingCast: false,
                isReeling: false,
                pullingOutOfWater: false,
                showingTreasure: false
            };

        return new AutoClosePopupConditions(
            automationEnabled,
            autoClosePopupEnabled,
            state,
            isPopupVisible,
            Game1.activeClickableMenu is not null,
            Game1.isFestival(),
            closeAlreadyAttempted
        );
    }

    public InstantBiteConditions ReadInstantBiteConditions(bool instantBiteEnabled)
    {
        return new InstantBiteConditions(
            instantBiteEnabled,
            rod.isFishing,
            rod.isNibbling,
            rod.timeUntilFishingBite > 0f,
            Game1.activeClickableMenu is not null,
            Game1.isFestival(),
            this.IsSupportedFishingMinigame
        );
    }

    public void BeginAutomaticCast(int castPower)
    {
        player.BeginUsingTool();
        this.SetCastPower(castPower);
    }

    public void CancelAutomaticCast()
    {
        if (rod is { isTimingCast: false, isCasting: false, castedButBobberStillInAir: false })
            return;

        player.completelyStopAnimatingOrDoingAction();
        ResetCancelledCastState(rod);
    }

    internal static void ResetCancelledCastState(FishingRod rod)
    {
        ArgumentNullException.ThrowIfNull(rod);

        // Vanilla resetState clears the charge and casting flags, but can leave the
        // in-flight flag set when a cast is cancelled before the bobber lands. That
        // stale flag classifies the next enabled observation as Casting forever.
        rod.resetState();
        rod.castedButBobberStillInAir = false;
    }

    private float GetCastStaminaCost()
    {
        return Math.Max(0f, 8f - player.FishingLevel * 0.1f);
    }

    private bool DoesCastConsumeStamina()
    {
        return !Game1.eventUp
            && player.IsLocalPlayer
            && !rod.hasEnchantmentOfType<EfficientToolEnchantment>();
    }

    public void SetCastPower(int castPower)
    {
        if (rod.isTimingCast)
            rod.castingPower = Math.Clamp(castPower / 100f, 0f, 1f);
    }

    public void HookFish()
    {
        rod.timePerBobberBob = 1f;
        rod.timeUntilFishingNibbleDone = FishingRod.maxTimeToNibble;
        rod.DoFunction(player.currentLocation, (int)rod.bobber.X, (int)rod.bobber.Y, 1, player);
        Rumble.rumble(0.95f, 200f);
    }

    public void CloseFishPopup()
    {
        rod.doneHoldingFish(player);
    }

    public void TriggerInstantBite()
    {
        if (rod.timeUntilFishingBite > 0f)
            rod.timeUntilFishingBite = 0f;
    }

    public bool TryGetBubbleSteeringTarget(bool enabled, out Vector2 target)
    {
        Point bubble = player.currentLocation.fishSplashPoint.Value;
        float flightMilliseconds = rod.animations.Count > 0 ? rod.animations[0].interval : 0f;
        return BubbleSteeringPolicy.TryGetTarget(new BubbleSteeringConditions(
            enabled,
            rod.castedButBobberStillInAir,
            player.currentLocation.canFishHere(),
            bubble != Point.Zero && player.currentLocation.isTileFishable(bubble.X, bubble.Y),
            player.FacingDirection,
            player.StandingPixel.ToVector2(),
            rod.bobber.Value,
            bubble,
            flightMilliseconds), out target);
    }

    public bool SteerToward(Vector2 target, ref Vector2 expectedPosition)
    {
        if (!rod.castedButBobberStillInAir)
            return false;

        Vector2 step = BubbleSteeringPolicy.GetSteeringStep(expectedPosition, target, player.FacingDirection);
        if (step == Vector2.Zero)
            return true;

        Vector2 nextPosition = expectedPosition + step;
        Vector2 correction = nextPosition - rod.bobber.Value;
        rod.bobber.Set(nextPosition);
        if (rod.animations.Count > 0)
            rod.animations[0].position += correction;
        expectedPosition = nextPosition;
        return false;
    }

    private bool IsCastTargetFishable(int castPower)
    {
        if (!player.currentLocation.canFishHere())
            return false;

        Point target = CalculateTargetTile(
            player.StandingPixel,
            player.FacingDirection,
            player.FishingLevel,
            castPower / 100f
        );
        if (player.currentLocation.isTileFishable(target.X, target.Y))
            return true;

        return player.FacingDirection is Game1.left or Game1.right
            ? player.currentLocation.isTileFishable(target.X, target.Y - 1)
              || player.currentLocation.isTileFishable(target.X, target.Y + 1)
            : player.currentLocation.isTileFishable(target.X - 1, target.Y)
              || player.currentLocation.isTileFishable(target.X + 1, target.Y);
    }

    internal static Point CalculateTargetTile(Point standingPixel, int facingDirection, int fishingLevel,
        float castPower)
    {
        int addedDistance = fishingLevel switch
        {
            >= 15 => 4,
            >= 8 => 3,
            >= 4 => 2,
            >= 1 => 1,
            _ => 0
        };
        float power = Math.Clamp(castPower, 0f, 1f);
        bool horizontal = facingDirection is Game1.left or Game1.right;
        float distance = Math.Max(128f, power * (addedDistance + (horizontal ? 4 : 3)) * Game1.tileSize);
        if (horizontal)
            distance -= 8f;

        Point pixel = facingDirection switch
        {
            Game1.up => new Point(standingPixel.X, standingPixel.Y - (int)distance),
            Game1.right => new Point(standingPixel.X + (int)distance, standingPixel.Y),
            Game1.down => new Point(standingPixel.X, standingPixel.Y + (int)distance),
            Game1.left => new Point(standingPixel.X - (int)distance, standingPixel.Y),
            _ => standingPixel
        };
        return new Point(pixel.X / Game1.tileSize, pixel.Y / Game1.tileSize);
    }
}
