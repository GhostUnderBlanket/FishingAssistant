using StardewModdingAPI;

namespace FishingAssistant;

internal sealed class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        this.Monitor.Log("Fishing Assistant 3 loaded.", LogLevel.Info);
    }
}
