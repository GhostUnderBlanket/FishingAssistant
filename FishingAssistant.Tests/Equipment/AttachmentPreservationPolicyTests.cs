using FishingAssistant.Equipment;

namespace FishingAssistant.Tests.Equipment;

public sealed class AttachmentPreservationPolicyTests
{
    [Fact]
    public void Decide_CapturesWhenRodStartsUsingPreservableAttachment()
    {
        AttachmentPreservationConditions conditions = new(
            HasSnapshot: false,
            IsSameRod: false,
            IsRodInUse: true,
            HasPreservableAttachment: true);

        Assert.Equal(AttachmentPreservationAction.Capture,
            AttachmentPreservationPolicy.Decide(conditions));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Decide_RestoresWhenUseEndsOrRodChanges(bool isSameRod, bool isRodInUse)
    {
        AttachmentPreservationConditions conditions = new(
            HasSnapshot: true,
            IsSameRod: isSameRod,
            IsRodInUse: isRodInUse,
            HasPreservableAttachment: true);

        Assert.Equal(AttachmentPreservationAction.Restore,
            AttachmentPreservationPolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_WaitsWhileCapturedRodRemainsInUse()
    {
        AttachmentPreservationConditions conditions = new(true, true, true, true);

        Assert.Equal(AttachmentPreservationAction.None,
            AttachmentPreservationPolicy.Decide(conditions));
    }

    [Fact]
    public void Decide_RestoresImmediatelyWhenInfiniteOptionsAreDisabledDuringUse()
    {
        AttachmentPreservationConditions conditions = new(
            HasSnapshot: true,
            IsSameRod: true,
            IsRodInUse: true,
            HasPreservableAttachment: false);

        Assert.Equal(AttachmentPreservationAction.Restore,
            AttachmentPreservationPolicy.Decide(conditions));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public void Decide_DoesNotCaptureWithoutActiveUseAndAttachment(bool inUse, bool hasAttachment)
    {
        AttachmentPreservationConditions conditions = new(false, false, inUse, hasAttachment);

        Assert.Equal(AttachmentPreservationAction.None,
            AttachmentPreservationPolicy.Decide(conditions));
    }
}
