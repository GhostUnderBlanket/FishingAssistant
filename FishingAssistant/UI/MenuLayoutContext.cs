namespace FishingAssistant.UI;

internal readonly record struct MenuLayoutContext(
    int ViewportWidth,
    int ViewportHeight,
    float UiScale,
    float ZoomLevel,
    string Language,
    string VisibleOptionSignature)
{
    public static MenuLayoutContext Create(
        int viewportWidth,
        int viewportHeight,
        float uiScale,
        float zoomLevel,
        string language,
        IEnumerable<string> visibleOptionKeys)
    {
        if (string.IsNullOrEmpty(language))
            throw new ArgumentException("The current language is required.", nameof(language));
        ArgumentNullException.ThrowIfNull(visibleOptionKeys);

        return new MenuLayoutContext(
            viewportWidth,
            viewportHeight,
            uiScale,
            zoomLevel,
            language,
            string.Join('\u001F', visibleOptionKeys));
    }
}
