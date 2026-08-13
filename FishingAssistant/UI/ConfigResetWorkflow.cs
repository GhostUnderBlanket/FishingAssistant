using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal sealed class ConfigResetWorkflow(Func<ModConfig> createDefaults)
{
    public bool IsPending { get; private set; }

    public void Request()
    {
        this.IsPending = true;
    }

    public ModConfig? Confirm()
    {
        if (!this.IsPending)
            return null;

        this.IsPending = false;
        return createDefaults();
    }

    public void Cancel()
    {
        this.IsPending = false;
    }
}
