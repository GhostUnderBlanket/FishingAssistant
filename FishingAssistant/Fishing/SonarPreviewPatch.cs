using System.Reflection.Emit;
using FishingAssistant.Configuration;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;

namespace FishingAssistant.Fishing;

internal static class SonarPreviewPatch
{
    private const string SonarBobberId = "(O)SonarBobber";

    private static readonly PerScreen<bool> HookHealthyForCurrentDraw = new(() => false);
    private static readonly PerScreen<bool> RuntimeFailureLogged = new(() => false);
    private static Func<ModConfig>? getConfig;
    private static IMonitor? monitor;

    public static bool IsInstalled { get; private set; }

    public static bool CanSuppressCurrentDraw => IsInstalled && HookHealthyForCurrentDraw.Value;

    public static void Apply(Harmony harmony, Func<ModConfig> configProvider, IMonitor modMonitor)
    {
        getConfig = configProvider;
        monitor = modMonitor;
        IsInstalled = false;
        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(BobberBar), nameof(BobberBar.draw), [typeof(SpriteBatch)]),
                transpiler: new HarmonyMethod(typeof(SonarPreviewPatch), nameof(TranspileDraw)));
        }
        catch (Exception exception)
        {
            IsInstalled = false;
            modMonitor.Log(
                $"Vanilla Sonar preview suppression is unavailable; Fish Preview will use its compatibility fallback.\n{exception}",
                LogLevel.Warn);
        }
    }

    public static void BeginActiveMenuDraw()
    {
        HookHealthyForCurrentDraw.Value = false;
    }

    private static IEnumerable<CodeInstruction> TranspileDraw(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = instructions.ToList();
        List<int> sonarLoads = codes
            .Select((instruction, index) => (instruction, index))
            .Where(entry => entry.instruction.opcode == OpCodes.Ldstr
                && string.Equals(entry.instruction.operand as string, SonarBobberId, StringComparison.Ordinal))
            .Select(entry => entry.index)
            .ToList();
        if (sonarLoads.Count != 2)
        {
            throw new InvalidOperationException(
                $"Expected two Sonar Bobber checks in BobberBar.draw, but found {sonarLoads.Count}.");
        }

        List<int> containsCalls = [];
        foreach (int sonarLoad in sonarLoads)
        {
            int containsCall = -1;
            for (int index = sonarLoad + 1; index < Math.Min(codes.Count, sonarLoad + 5); index++)
            {
                if (codes[index].Calls(AccessTools.Method(
                        typeof(List<string>), nameof(List<string>.Contains), [typeof(string)])))
                {
                    containsCall = index;
                    break;
                }
            }

            if (containsCall < 0)
                throw new InvalidOperationException("Couldn't locate a Vanilla Sonar condition.");
            containsCalls.Add(containsCall);
        }

        // Insert at the later site first so the earlier instruction index stays valid.
        // The second Sonar check controls Challenge Bait placement. Reserve Vanilla's
        // Sonar space only for the mod's Sonar style. Classic handles the collision in
        // its own layout by moving to the other side of BobberBar.
        codes.Insert(containsCalls[1] + 1,
            new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(SonarPreviewPatch), nameof(ReserveSonarPreviewSpace))));
        codes.Insert(containsCalls[0] + 1,
            new CodeInstruction(OpCodes.Call,
                AccessTools.Method(typeof(SonarPreviewPatch), nameof(FilterVanillaPreview))));
        IsInstalled = true;
        monitor?.Log("Installed Vanilla Sonar preview suppression compatibility hook.", LogLevel.Trace);
        return codes;
    }

    private static bool FilterVanillaPreview(bool vanillaShouldDraw)
    {
        try
        {
            if (getConfig is null)
                return vanillaShouldDraw;

            bool previewEnabled = getConfig().DisplayFishPreview;
            HookHealthyForCurrentDraw.Value = true;
            return vanillaShouldDraw && !previewEnabled;
        }
        catch (Exception exception)
        {
            HookHealthyForCurrentDraw.Value = false;
            if (!RuntimeFailureLogged.Value)
            {
                RuntimeFailureLogged.Value = true;
                monitor?.Log(
                    $"Vanilla Sonar preview suppression failed open for the current screen.\n{exception}",
                    LogLevel.Error);
            }
            return vanillaShouldDraw;
        }
    }

    private static bool ReserveSonarPreviewSpace(bool hasVanillaSonarBobber)
    {
        try
        {
            ModConfig? config = getConfig?.Invoke();
            return FishPreviewStylePolicy.ShouldReserveChallengeBaitSpace(
                hasVanillaSonarBobber,
                config?.DisplayFishPreview == true,
                config?.FishPreviewStyle ?? FishPreviewStyle.Classic);
        }
        catch
        {
            return hasVanillaSonarBobber;
        }
    }
}
