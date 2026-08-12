namespace FishingAssistant.UI;

internal sealed record MenuLayout(
    int X,
    int Y,
    int Width,
    int Height,
    int Padding,
    int HeaderHeight,
    int FooterHeight,
    int OptionHeight)
{
    public int ContentTop => this.Y + this.HeaderHeight;

    public int ContentBottom => this.Y + this.Height - this.FooterHeight;

    public static MenuLayout Calculate(int viewportWidth, int viewportHeight, int visibleOptionCount)
    {
        if (viewportWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        if (viewportHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewportHeight));
        if (visibleOptionCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(visibleOptionCount));

        int margin = viewportWidth >= 960 && viewportHeight >= 640 ? 48 : 12;
        int width = Math.Min(920, Math.Max(1, viewportWidth - margin * 2));
        int height = Math.Min(680, Math.Max(1, viewportHeight - margin * 2));
        int padding = width >= 640 ? 48 : 24;
        int headerHeight = Math.Min(88, Math.Max(48, height / 6));
        int footerHeight = Math.Min(88, Math.Max(56, height / 5));
        int contentHeight = Math.Max(1, height - headerHeight - footerHeight);
        int optionHeight = Math.Max(1, contentHeight / visibleOptionCount);

        return new MenuLayout(
            X: (viewportWidth - width) / 2,
            Y: (viewportHeight - height) / 2,
            Width: width,
            Height: height,
            Padding: padding,
            HeaderHeight: headerHeight,
            FooterHeight: footerHeight,
            OptionHeight: optionHeight
        );
    }
}
