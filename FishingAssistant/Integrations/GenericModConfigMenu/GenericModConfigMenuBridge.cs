using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace FishingAssistant.Integrations.GenericModConfigMenu;

/// <summary>Optionally redirects Fishing Assistant's GMCM entry to its custom configuration menu.</summary>
internal sealed class GenericModConfigMenuBridge
{
    private const string ModId = "spacechase0.GenericModConfigMenu";

    private readonly IModHelper helper;
    private readonly IManifest manifest;
    private readonly IMonitor monitor;
    private readonly Func<bool> openConfigMenu;
    private readonly Func<string> loadSaveMessage;
    private readonly PerScreen<TransitionState> transitions = new(() => TransitionState.None);
    private IGenericModConfigMenuApi? api;
    private bool useCurrentComplexOptionApi;

    public GenericModConfigMenuBridge(
        IModHelper helper,
        IManifest manifest,
        IMonitor monitor,
        Func<bool> openConfigMenu,
        Func<string> loadSaveMessage)
    {
        this.helper = helper;
        this.manifest = manifest;
        this.monitor = monitor;
        this.openConfigMenu = openConfigMenu;
        this.loadSaveMessage = loadSaveMessage;
    }

    /// <summary>Register the optional integration after all mods have loaded.</summary>
    public void Register()
    {
        this.api = this.helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(ModId);
        if (this.api is null)
            return;

        this.useCurrentComplexOptionApi =
            this.helper.ModRegistry.Get(ModId)?.Manifest.Version.IsOlderThan("1.17.0") == false;

        try
        {
            this.api.Register(this.manifest, static () => { }, static () => { });
            this.api.AddParagraph(this.manifest, this.loadSaveMessage);
            this.AddRedirectOption();
            this.monitor.Log(
                "Registered the optional Generic Mod Config Menu launcher.",
                LogLevel.Debug);
        }
        catch (Exception exception)
        {
            try
            {
                this.api.Unregister(this.manifest);
            }
            catch
            {
                // The integration is optional; retain the original error below.
            }

            this.api = null;
            this.monitor.Log(
                $"Generic Mod Config Menu is installed, but its optional launcher couldn't be registered.\n{exception}",
                LogLevel.Warn);
        }
    }

    /// <summary>Advance a deferred handoff from GMCM to Fishing Assistant's custom menu.</summary>
    public void UpdateCurrent()
    {
        switch (this.transitions.Value)
        {
            case TransitionState.None:
                return;

            case TransitionState.Requested:
                this.transitions.Value = TransitionState.CloseGmcm;
                return;

            case TransitionState.CloseGmcm:
                if (!Context.IsWorldReady)
                {
                    this.transitions.Value = TransitionState.None;
                    return;
                }

                if (this.IsFishingAssistantGmcmMenu())
                {
                    Game1.activeClickableMenu?.exitThisMenuNoSound();
                    this.transitions.Value = TransitionState.OpenCustomMenu;
                    return;
                }

                if (Game1.activeClickableMenu is null)
                {
                    this.transitions.Value = TransitionState.OpenCustomMenu;
                    return;
                }

                this.transitions.Value = TransitionState.None;
                this.monitor.Log(
                    "Canceled the GMCM configuration handoff because another menu became active.",
                    LogLevel.Debug);
                return;

            case TransitionState.OpenCustomMenu:
                this.transitions.Value = TransitionState.None;
                this.openConfigMenu();
                return;
        }
    }

    /// <summary>Clear pending handoffs for every local screen.</summary>
    public void Reset() => this.transitions.ResetAllScreens();

    private void AddRedirectOption()
    {
        if (this.useCurrentComplexOptionApi)
        {
            IGenericModConfigMenuApiCurrent? currentApi =
                this.helper.ModRegistry.GetApi<IGenericModConfigMenuApiCurrent>(ModId);
            if (currentApi is null)
                throw new InvalidOperationException("GMCM's current complex-option API is unavailable.");

            currentApi.AddComplexOptionWithGamepadSupport(
                this.manifest,
                static () => string.Empty,
                static (_, _) => { },
                beforeMenuOpened: this.RequestOpen,
                height: static () => 0,
                fieldId: "open_fishing_assistant_config");
            return;
        }

        IGenericModConfigMenuApiLegacy? legacyApi =
            this.helper.ModRegistry.GetApi<IGenericModConfigMenuApiLegacy>(ModId);
        if (legacyApi is null)
            throw new InvalidOperationException("GMCM's compatible complex-option API is unavailable.");

        legacyApi.AddComplexOption(
            this.manifest,
            static () => string.Empty,
            static (_, _) => { },
            beforeMenuOpened: this.RequestOpen,
            height: static () => 0,
            fieldId: "open_fishing_assistant_config");
    }

    private void RequestOpen()
    {
        if (Context.IsWorldReady)
            this.transitions.Value = TransitionState.Requested;
    }

    private bool IsFishingAssistantGmcmMenu()
    {
        return this.api?.TryGetCurrentMenu(out IManifest? mod, out _) == true
            && string.Equals(mod?.UniqueID, this.manifest.UniqueID, StringComparison.OrdinalIgnoreCase);
    }

    private enum TransitionState
    {
        None,
        Requested,
        CloseGmcm,
        OpenCustomMenu
    }
}
