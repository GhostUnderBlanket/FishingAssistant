namespace FishingAssistant.HUD;

internal static class AutomationHudAnimation
{
    internal const int FrameCount = 4;
    internal const int FrameDurationMilliseconds = 250;

    public static int GetEmoteFrame(int baseFrame, double elapsedMilliseconds)
    {
        int frameOffset = (int)(Math.Max(0d, elapsedMilliseconds) / FrameDurationMilliseconds) % FrameCount;
        return baseFrame + frameOffset;
    }
}
