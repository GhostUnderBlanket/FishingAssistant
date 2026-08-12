using FishingAssistant.Equipment;

namespace FishingAssistant.Tests.Equipment;

public sealed class RodEnchantmentPolicyTests
{
    private static readonly IReadOnlySet<RodEnchantmentKind> Requested = new HashSet<RodEnchantmentKind>
    {
        RodEnchantmentKind.AutoHook,
        RodEnchantmentKind.Efficient
    };

    private static RodEnchantmentConditions EquippedConditions => new(
        false,
        true,
        true,
        Requested,
        new HashSet<RodEnchantmentKind>(),
        new HashSet<RodEnchantmentKind>());

    [Fact]
    public void Decide_AddsOnlyMissingRequestedEnchantmentsToEquippedRod()
    {
        RodEnchantmentConditions conditions = EquippedConditions with
        {
            Existing = new HashSet<RodEnchantmentKind> { RodEnchantmentKind.AutoHook }
        };

        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(conditions);

        Assert.Equal([RodEnchantmentKind.Efficient], decision.Add);
        Assert.Empty(decision.Remove);
    }

    [Fact]
    public void Decide_PreservesEnchantmentsThatWereNotAddedByAssistant()
    {
        RodEnchantmentConditions conditions = EquippedConditions with
        {
            Requested = new HashSet<RodEnchantmentKind>(),
            Existing = new HashSet<RodEnchantmentKind> { RodEnchantmentKind.Master },
            Managed = new HashSet<RodEnchantmentKind>()
        };

        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(conditions);

        Assert.Empty(decision.Add);
        Assert.Empty(decision.Remove);
    }

    [Fact]
    public void Decide_RemovesManagedEnchantmentWhenOptionIsDisabled()
    {
        RodEnchantmentConditions conditions = EquippedConditions with
        {
            Requested = new HashSet<RodEnchantmentKind> { RodEnchantmentKind.AutoHook },
            Existing = new HashSet<RodEnchantmentKind>
            {
                RodEnchantmentKind.AutoHook,
                RodEnchantmentKind.Efficient
            },
            Managed = new HashSet<RodEnchantmentKind>
            {
                RodEnchantmentKind.AutoHook,
                RodEnchantmentKind.Efficient
            }
        };

        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(conditions);

        Assert.Equal([RodEnchantmentKind.Efficient], decision.Remove);
        Assert.Empty(decision.Add);
    }

    [Fact]
    public void Decide_RemovesManagedEnchantmentsOnUnequipWhenConfigured()
    {
        RodEnchantmentConditions conditions = EquippedConditions with
        {
            IsEquipped = false,
            Existing = Requested,
            Managed = Requested
        };

        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(conditions);

        Assert.Equal(Requested.OrderBy(kind => kind), decision.Remove);
        Assert.Empty(decision.Add);
    }

    [Fact]
    public void Decide_KeepsManagedEnchantmentsOnUnequipWhenConfigured()
    {
        RodEnchantmentConditions conditions = EquippedConditions with
        {
            IsEquipped = false,
            RemoveWhenUnequipped = false,
            Existing = Requested,
            Managed = Requested
        };

        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(conditions);

        Assert.Empty(decision.Remove);
        Assert.Empty(decision.Add);
    }

    [Fact]
    public void Decide_RemoteMultiplayerRemovesAllManagedAndAddsNothing()
    {
        RodEnchantmentConditions conditions = EquippedConditions with
        {
            HasRemotePlayers = true,
            Existing = Requested,
            Managed = Requested
        };

        RodEnchantmentDecision decision = RodEnchantmentPolicy.Decide(conditions);

        Assert.Equal(Requested.OrderBy(kind => kind), decision.Remove);
        Assert.Empty(decision.Add);
        Assert.True(decision.IsUnsupportedMultiplayer);
    }
}
