using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal enum JunkItemState
{
    Normal,
    Junk,
    Ignore
}

internal static class JunkListSelection
{
    public static JunkListGroups Group(
        IEnumerable<ConfigItem> items,
        IReadOnlyCollection<string> selectedIds)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(selectedIds);
        ConfigItem[] source = items.ToArray();
        return new JunkListGroups(
            source.Where(item => selectedIds.Contains(item.QualifiedItemId, StringComparer.OrdinalIgnoreCase))
                .ToArray(),
            source.Where(item => !selectedIds.Contains(item.QualifiedItemId, StringComparer.OrdinalIgnoreCase))
                .ToArray());
    }

    public static JunkItemState Toggle(
        List<string> selectedIds,
        string qualifiedItemId,
        JunkItemState selectedState)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);
        if (string.IsNullOrWhiteSpace(qualifiedItemId))
            throw new ArgumentException("The qualified item ID must not be empty.", nameof(qualifiedItemId));

        bool removed = selectedIds.RemoveAll(value =>
            string.Equals(value, qualifiedItemId, StringComparison.OrdinalIgnoreCase)) > 0;
        JunkItemState result = removed ? JunkItemState.Normal : selectedState;
        if (!removed)
            selectedIds.Add(qualifiedItemId);

        return result;
    }

    public static JunkItemState GetState(
        IReadOnlyCollection<string> selectedIds,
        string qualifiedItemId,
        JunkItemState selectedState)
    {
        return selectedIds.Contains(qualifiedItemId, StringComparer.OrdinalIgnoreCase)
            ? selectedState
            : JunkItemState.Normal;
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
