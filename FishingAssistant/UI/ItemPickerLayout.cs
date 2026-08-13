namespace FishingAssistant.UI;

internal sealed record ItemPickerLayout(
    int X,
    int Y,
    int Width,
    int Height,
    int Padding,
    int HeaderHeight,
    int SearchHeight,
    int FooterHeight,
    int Columns,
    int Rows,
    int CardWidth,
    int CardHeight,
    int Gap)
{
    public int ContentX => this.X + this.Padding;

    public int ContentTop => this.Y + this.HeaderHeight + this.SearchHeight;

    public int ContentWidth => this.Width - this.Padding * 2;

    public int ContentBottom => this.Y + this.Height - this.FooterHeight;

    public int PageSize => this.Columns * this.Rows;

    public static ItemPickerLayout Calculate(int viewportWidth, int viewportHeight)
    {
        if (viewportWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        if (viewportHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(viewportHeight));

        int margin = viewportWidth >= 960 && viewportHeight >= 640 ? 40 : 10;
        int width = Math.Min(980, Math.Max(1, viewportWidth - margin * 2));
        int height = Math.Min(720, Math.Max(1, viewportHeight - margin * 2));
        int padding = width >= 640 ? 40 : 18;
        int headerHeight = Math.Min(76, Math.Max(44, height / 8));
        int searchHeight = Math.Min(60, Math.Max(44, height / 10));
        int footerHeight = Math.Min(72, Math.Max(52, height / 8));
        int gap = width >= 600 ? 10 : 6;
        int contentWidth = Math.Max(1, width - padding * 2);
        int contentHeight = Math.Max(1, height - headerHeight - searchHeight - footerHeight);
        int columns = Math.Clamp((contentWidth + gap) / 180, 1, 5);
        int cardWidth = Math.Max(1, (contentWidth - gap * (columns - 1)) / columns);
        int rows = Math.Max(1, (contentHeight + gap) / 80);
        int cardHeight = Math.Max(1, (contentHeight - gap * (rows - 1)) / rows);

        return new ItemPickerLayout(
            (viewportWidth - width) / 2,
            (viewportHeight - height) / 2,
            width,
            height,
            padding,
            headerHeight,
            searchHeight,
            footerHeight,
            columns,
            rows,
            cardWidth,
            cardHeight,
            gap
        );
    }
}
