namespace FishingAssistant.Equipment;

internal enum AttachmentPreservationAction
{
    None,
    Capture,
    Restore
}

internal sealed record AttachmentPreservationConditions(
    bool HasSnapshot,
    bool IsSameRod,
    bool IsRodInUse,
    bool HasPreservableAttachment);

internal static class AttachmentPreservationPolicy
{
    public static AttachmentPreservationAction Decide(AttachmentPreservationConditions conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        if (conditions.HasSnapshot)
        {
            return !conditions.IsSameRod
                || !conditions.IsRodInUse
                || !conditions.HasPreservableAttachment
                ? AttachmentPreservationAction.Restore
                : AttachmentPreservationAction.None;
        }

        return conditions.IsRodInUse && conditions.HasPreservableAttachment
            ? AttachmentPreservationAction.Capture
            : AttachmentPreservationAction.None;
    }
}
