namespace FishingAssistant.UI.Controls;

internal static class OptionAdjustment
{
    public static T Cycle<T>(IReadOnlyList<T> values, T current, int direction)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count == 0)
            throw new ArgumentException("At least one value is required.", nameof(values));

        int index = values.IndexOf(current);
        if (index < 0)
            index = 0;
        else
            index = (index + Math.Sign(direction) + values.Count) % values.Count;

        return values[index];
    }

    public static double Step(double current, int direction, double increment, double minimum, double maximum)
    {
        if (increment <= 0)
            throw new ArgumentOutOfRangeException(nameof(increment));
        if (minimum > maximum)
            throw new ArgumentOutOfRangeException(nameof(minimum));

        double adjusted = current + Math.Sign(direction) * increment;
        return Math.Round(Math.Clamp(adjusted, minimum, maximum), 4, MidpointRounding.AwayFromZero);
    }

    private static int IndexOf<T>(this IReadOnlyList<T> values, T value)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int index = 0; index < values.Count; index++)
        {
            if (comparer.Equals(values[index], value))
                return index;
        }

        return -1;
    }
}
