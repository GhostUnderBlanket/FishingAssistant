using System.Reflection.Emit;
using FishingAssistant.Configuration;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley.Menus;

namespace FishingAssistant.Fishing;

internal static class MinigameAssistancePatch
{
    private static Func<ModConfig>? getConfig;
    private static IMonitor? monitor;

    public static bool IsInstalled { get; private set; }

    public static void Apply(Harmony harmony, Func<ModConfig> configProvider, IMonitor modMonitor)
    {
        getConfig = configProvider;
        monitor = modMonitor;
        IsInstalled = false;
        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(BobberBar), nameof(BobberBar.update), [typeof(GameTime)]),
                prefix: new HarmonyMethod(typeof(MinigameAssistancePatch), nameof(BeforeUpdate)),
                postfix: new HarmonyMethod(typeof(MinigameAssistancePatch), nameof(AfterUpdate)),
                transpiler: new HarmonyMethod(typeof(MinigameAssistancePatch), nameof(TranspileUpdate)));
        }
        catch (Exception exception)
        {
            IsInstalled = false;
            modMonitor.Log(
                $"Minigame Assistance movement and progress modifiers are unavailable because their Vanilla compatibility hook couldn't be installed. Bar Size assistance remains available.\n{exception}",
                LogLevel.Warn);
        }
    }

    private static void BeforeUpdate(BobberBar __instance, out float __state)
    {
        __state = __instance.distanceFromCatchPenaltyModifier;
        try
        {
            ModConfig? config = getConfig?.Invoke();
            if (config is null)
                return;
            __instance.distanceFromCatchPenaltyModifier =
                MinigameAssistancePolicy.ScaleProgressLossModifier(
                    __state,
                    config.ProgressLossPercent);
        }
        catch (Exception exception)
        {
            __instance.distanceFromCatchPenaltyModifier = __state;
            monitor?.Log($"Progress Loss assistance was skipped for the current frame.\n{exception}", LogLevel.Error);
        }
    }

    private static void AfterUpdate(BobberBar __instance, float __state)
    {
        __instance.distanceFromCatchPenaltyModifier = __state;
    }

    private static IEnumerable<CodeInstruction> TranspileUpdate(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        int fishMovementAdd = FindFishMovementAdd(codes);
        int progressGainConstant = FindUniqueFloatConstant(codes, 0.002f, "catch-progress gain", requireAddStore: true);
        int treasureGainConstant = FindUniqueFloatConstant(codes, 0.0135f, "treasure-progress gain");

        List<(int Index, CodeInstruction Instruction)> insertions =
        [
            (fishMovementAdd + 1, new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(MinigameAssistancePatch), nameof(ScaleFishMovement)))),
            (progressGainConstant + 1, new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(MinigameAssistancePatch), nameof(ScaleProgressGain)))),
            (treasureGainConstant + 1, new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(MinigameAssistancePatch), nameof(ScaleTreasureGain))))
        ];
        foreach ((int index, CodeInstruction instruction) in insertions.OrderByDescending(entry => entry.Index))
            codes.Insert(index, instruction);

        IsInstalled = true;
        monitor?.Log("Installed Minigame Assistance Vanilla-calculation hooks.", LogLevel.Trace);
        return codes;
    }

    private static int FindFishMovementAdd(IReadOnlyList<CodeInstruction> codes)
    {
        var accelerationField = AccessTools.Field(typeof(BobberBar), nameof(BobberBar.floaterSinkerAcceleration));
        List<int> matches = [];
        for (int index = 0; index < codes.Count - 1; index++)
        {
            if (codes[index].LoadsField(accelerationField) && codes[index + 1].opcode == OpCodes.Add)
                matches.Add(index + 1);
        }

        if (matches.Count != 1)
            throw new InvalidOperationException($"Expected one final fish-movement calculation, but found {matches.Count}.");
        return matches[0];
    }

    private static int FindUniqueFloatConstant(
        IReadOnlyList<CodeInstruction> codes,
        float value,
        string description,
        bool requireAddStore = false)
    {
        List<int> matches = [];
        for (int index = 0; index < codes.Count; index++)
        {
            if (codes[index].opcode != OpCodes.Ldc_R4 || codes[index].operand is not float operand
                || !operand.Equals(value))
                continue;
            if (requireAddStore && !HasCatchProgressAddStore(codes, index))
                continue;
            matches.Add(index);
        }

        if (matches.Count != 1)
            throw new InvalidOperationException($"Expected one Vanilla {description} constant, but found {matches.Count}.");
        return matches[0];
    }

    private static bool HasCatchProgressAddStore(IReadOnlyList<CodeInstruction> codes, int constantIndex)
    {
        var progressField = AccessTools.Field(typeof(BobberBar), nameof(BobberBar.distanceFromCatching));
        int end = Math.Min(codes.Count, constantIndex + 4);
        bool sawAdd = false;
        for (int index = constantIndex + 1; index < end; index++)
        {
            sawAdd |= codes[index].opcode == OpCodes.Add;
            if (codes[index].StoresField(progressField))
                return sawAdd;
        }
        return false;
    }

    private static float ScaleFishMovement(float vanillaDelta)
    {
        return Scale(vanillaDelta, config => config.FishSpeedPercent,
            MinigameAssistancePolicy.ScaleFishMovement);
    }

    private static float ScaleProgressGain(float vanillaGain)
    {
        return Scale(vanillaGain, config => config.ProgressGainPercent,
            MinigameAssistancePolicy.ScaleProgressGain);
    }

    private static float ScaleTreasureGain(float vanillaGain)
    {
        return Scale(vanillaGain, config => config.TreasureSpeedPercent,
            MinigameAssistancePolicy.ScaleTreasureGain);
    }

    private static float Scale(
        float vanillaValue,
        Func<ModConfig, int> getPercent,
        Func<float, int, float> scale)
    {
        try
        {
            ModConfig? config = getConfig?.Invoke();
            return config is null
                ? vanillaValue
                : scale(vanillaValue, getPercent(config));
        }
        catch (Exception exception)
        {
            monitor?.Log($"A Minigame Assistance modifier was skipped for the current frame.\n{exception}", LogLevel.Error);
            return vanillaValue;
        }
    }
}
