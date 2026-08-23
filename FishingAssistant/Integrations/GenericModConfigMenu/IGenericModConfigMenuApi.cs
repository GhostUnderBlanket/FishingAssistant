using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;

namespace FishingAssistant.Integrations.GenericModConfigMenu;

/// <summary>The subset of the Generic Mod Config Menu API used by Fishing Assistant.</summary>
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    void Unregister(IManifest mod);

    void AddParagraph(IManifest mod, Func<string> text);

    bool TryGetCurrentMenu(out IManifest? mod, out string? page);
}

/// <summary>The redirect callback exposed by GMCM 1.17 and later.</summary>
public interface IGenericModConfigMenuApiCurrent
{
    void AddComplexOptionWithGamepadSupport(
        IManifest mod,
        Func<string> name,
        Action<SpriteBatch, Vector2> draw,
        Func<string>? tooltip = null,
        Action? beforeMenuOpened = null,
        Action? beforeSave = null,
        Action? afterSave = null,
        Action? beforeReset = null,
        Action? afterReset = null,
        Action? beforeMenuClosed = null,
        Func<IEnumerable<ClickableComponent>>? snapRegionsOverride = null,
        Func<bool>? snapRegionsNeedRefreshing = null,
        Func<bool?>? usingGamepadMovement = null,
        Func<int>? height = null,
        string? fieldId = null);
}

/// <summary>The compatible redirect callback exposed by GMCM versions before 1.17.</summary>
public interface IGenericModConfigMenuApiLegacy
{
    void AddComplexOption(
        IManifest mod,
        Func<string> name,
        Action<SpriteBatch, Vector2> draw,
        Func<string>? tooltip = null,
        Action? beforeMenuOpened = null,
        Action? beforeSave = null,
        Action? afterSave = null,
        Action? beforeReset = null,
        Action? afterReset = null,
        Action? beforeMenuClosed = null,
        Func<int>? height = null,
        string? fieldId = null);
}
