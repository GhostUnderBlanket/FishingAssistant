using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace FishingAssistant.UI.Controls;

internal interface IConfigControl
{
    ClickableComponent Component { get; }

    string Description { get; }

    int InlineMessageRight { get; }

    void ReceiveLeftClick(int x, int y);

    bool Adjust(int direction);

    void Draw(SpriteBatch batch, bool highlighted, int labelBottomInset = 0);
}
