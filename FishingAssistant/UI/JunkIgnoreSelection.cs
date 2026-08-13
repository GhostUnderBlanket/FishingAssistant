using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal enum JunkListMode
{
    Junk,
    Ignore
}

internal enum JunkItemState
{
    Normal,
    Junk,
    Ignore
}

internal static class JunkListSelection
{
    public static JunkListGroups GroupForMode(
        IEnumerable<ConfigItem> items,
        IReadOnlyCollection<string> junkIds,
        IReadOnlyCollection<string> ignoreIds,
        JunkListMode mode)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(junkIds);
        ArgumentNullException.ThrowIfNull(ignoreIds);

        JunkItemState selectedState = mode == JunkListMode.Junk
            ? JunkItemState.Junk
            : JunkItemState.Ignore;
        ConfigItem[] source = items.ToArray();
        return new JunkListGroups(
            source.Where(item => GetState(junkIds, ignoreIds, item.QualifiedItemId) == selectedState)
                .ToArray(),
            source.Where(item => GetState(junkIds, ignoreIds, item.QualifiedItemId) == JunkItemState.Normal)
                .ToArray());
    }

    public static JunkItemState Toggle(
        List<string> junkIds,
        List<string> ignoreIds,
        string qualifiedItemId,
        JunkListMode mode)
    {
        ArgumentNullException.ThrowIfNull(junkIds);
        ArgumentNullException.ThrowIfNull(ignoreIds);
        if (string.IsNullOrWhiteSpace(qualifiedItemId))
            throw new ArgumentException("The qualified item ID must not be empty.", nameof(qualifiedItemId));

        JunkItemState current = GetState(junkIds, ignoreIds, qualifiedItemId);
        JunkItemState target = mode == JunkListMode.Junk ? JunkItemState.Junk : JunkItemState.Ignore;
        JunkItemState result = current == target ? JunkItemState.Normal : target;

        junkIds.RemoveAll(value => string.Equals(value, qualifiedItemId, StringComparison.OrdinalIgnoreCase));
        ignoreIds.RemoveAll(value => string.Equals(value, qualifiedItemId, StringComparison.OrdinalIgnoreCase));

        if (result == JunkItemState.Junk)
            junkIds.Add(qualifiedItemId);
        else if (result == JunkItemState.Ignore)
            ignoreIds.Add(qualifiedItemId);

        return result;
    }

    public static JunkItemState GetState(
        IReadOnlyCollection<string> junkIds,
        IReadOnlyCollection<string> ignoreIds,
        string qualifiedItemId)
    {
        if (ignoreIds.Contains(qualifiedItemId, StringComparer.OrdinalIgnoreCase))
            return JunkItemState.Ignore;
        if (junkIds.Contains(qualifiedItemId, StringComparer.OrdinalIgnoreCase))
            return JunkItemState.Junk;
        return JunkItemState.Normal;
    }

    public static IReadOnlyList<ConfigItem> Filter(IEnumerable<ConfigItem> items, string? search)
    {
        ArgumentNullException.ThrowIfNull(items);
        string query = search?.Trim() ?? "";
        if (query.Length == 0)
            return items.ToArray();

        return items.Where(item =>
                item.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                || item.QualifiedItemId.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

internal sealed record JunkListGroups(
    IReadOnlyList<ConfigItem> Selected,
    IReadOnlyList<ConfigItem> Normal);
