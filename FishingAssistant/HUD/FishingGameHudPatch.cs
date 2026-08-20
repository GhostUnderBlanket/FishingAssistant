using FishingAssistant.Configuration;
using FishingAssistant.Runtime;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Minigames;

namespace FishingAssistant.HUD;

/// <summary>Draws the automation HUD after the Fair's standalone fishing minigame.</summary>
internal static class FishingGameHudPatch
{
    private static readonly PerScreen<bool> RuntimeFailureLogged = new(() => false);
    private static AutomationHudRenderer? renderer;
    private static Func<AutomationSession>? getSession;
    private static Func<ModConfig>? getConfig;
    private static IMonitor? monitor;

    public static void Apply(
        Harmony harmony,
        AutomationHudRenderer hudRenderer,
        Func<AutomationSession> sessionProvider,
        Func<ModConfig> configProvider,
        IMonitor modMonitor)
    {
        renderer = hudRenderer;
        getSession = sessionProvider;
        getConfig = configProvider;
        monitor = modMonitor;

        harmony.Patch(
            AccessTools.Method(typeof(FishingGame), nameof(FishingGame.draw), [typeof(SpriteBatch)]),
            postfix: new HarmonyMethod(typeof(FishingGameHudPatch), nameof(AfterFishingGameDraw)));
    }

    private static void AfterFishingGameDraw(SpriteBatch b)
    {
        try
        {
            if (Game1.currentMinigame is not FishingGame
                || renderer is null
                || getSession is null
                || getConfig is null)
            {
                return;
            }

            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            renderer.Draw(b, getSession(), getConfig());
            b.End();
        }
        catch (Exception exception)
        {
            if (RuntimeFailureLogged.Value)
                return;

            RuntimeFailureLogged.Value = true;
            monitor?.Log(
                $"Automation HUD could not be drawn over the FishingGame minigame.\n{exception}",
                LogLevel.Error);
        }
    }
}
