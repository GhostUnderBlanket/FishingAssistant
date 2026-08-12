using FishingAssistant.Configuration;

namespace FishingAssistant.UI;

internal static class JunkIgnoreSelection
{
    public static bool Toggle(List<string> selectedIds, string qualifiedItemId)
    {
        ArgumentNullException.ThrowIfNull(selectedIds);
        if (string.IsNullOrWhiteSpace(qualifiedItemId))
            throw new ArgumentException("The qualified item ID must not be empty.", nameof(qualifiedItemId));

        int index = selectedIds.FindIndex(value =>
            string.Equals(value, qualifiedItemId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            selectedIds.RemoveAt(index);
            return false;
        }

        selectedIds.Add(qualifiedItemId);
        return true;
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
