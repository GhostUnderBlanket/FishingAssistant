using FishingAssistant.Configuration;

namespace FishingAssistant.Tests.Configuration;

public sealed class ConfigProfileIsolationTests
{
    [Fact]
    public void Apply_ChangesOnlyCurrentPlayerProfile()
    {
        string? currentProfile = null;
        InMemoryProfileStore profiles = new();
        ConfigManager manager = CreateManager(() => currentProfile, profiles);
        manager.Load();

        currentProfile = "player-100";
        ConfigEditSession firstSession = manager.CreateEditSession();
        firstSession.Draft.AutoCastFishingRod = false;
        manager.Apply(firstSession);

        currentProfile = "player-200";
        Assert.True(manager.Active.AutoCastFishingRod);
        ConfigEditSession secondSession = manager.CreateEditSession();
        secondSession.Draft.AutoHookFish = false;
        manager.Apply(secondSession);

        currentProfile = "player-100";
        Assert.False(manager.Active.AutoCastFishingRod);
        Assert.True(manager.Active.AutoHookFish);

        currentProfile = "player-200";
        Assert.True(manager.Active.AutoCastFishingRod);
        Assert.False(manager.Active.AutoHookFish);
        Assert.Equal(2, profiles.Writes.Count);
    }

    [Fact]
    public void Apply_RevisionChangesDoNotInvalidateAnotherPlayerDraft()
    {
        string? currentProfile = "player-100";
        ConfigManager manager = CreateManager(() => currentProfile, new InMemoryProfileStore());
        manager.Load();
        ConfigEditSession firstSession = manager.CreateEditSession();

        currentProfile = "player-200";
        ConfigEditSession secondSession = manager.CreateEditSession();
        secondSession.Draft.AutoHookFish = false;
        manager.Apply(secondSession);

        currentProfile = "player-100";
        firstSession.Draft.AutoCastFishingRod = false;
        manager.Apply(firstSession);

        Assert.False(manager.Active.AutoCastFishingRod);
    }

    [Fact]
    public void Apply_RejectsDraftAfterCurrentLocalPlayerChanges()
    {
        string? currentProfile = "player-100";
        ConfigManager manager = CreateManager(() => currentProfile, new InMemoryProfileStore());
        manager.Load();
        ConfigEditSession session = manager.CreateEditSession();

        currentProfile = "player-200";

        Assert.Throws<InvalidOperationException>(() => manager.Apply(session));
    }

    [Fact]
    public void Active_LoadsExistingProfileInsteadOfBaseTemplate()
    {
        string? currentProfile = "player-100";
        InMemoryProfileStore profiles = new();
        profiles.Seed("player-100", new ModConfig { AutoCastFishingRod = false });
        ConfigManager manager = CreateManager(() => currentProfile, profiles);
        manager.Load();

        Assert.False(manager.Active.AutoCastFishingRod);
    }

    [Fact]
    public void Apply_DoesNotOverwriteFutureProfileSchema()
    {
        string? currentProfile = "player-100";
        InMemoryProfileStore profiles = new();
        profiles.Seed("player-100", new ModConfig { ConfigVersion = ModConfig.CurrentVersion + 1 });
        ConfigManager manager = CreateManager(() => currentProfile, profiles);
        manager.Load();
        ConfigEditSession session = manager.CreateEditSession();

        Assert.Throws<InvalidOperationException>(() => manager.Apply(session));
        Assert.Empty(profiles.Writes);
    }

    [Fact]
    public void Apply_DoesNotOverwriteUnreadableProfile()
    {
        string? currentProfile = "player-100";
        InMemoryProfileStore profiles = new() { ReadException = new InvalidDataException() };
        ConfigManager manager = CreateManager(() => currentProfile, profiles);
        manager.Load();
        ConfigEditSession session = manager.CreateEditSession();

        Assert.Throws<InvalidOperationException>(() => manager.Apply(session));
        Assert.Empty(profiles.Writes);
    }

    private static ConfigManager CreateManager(
        Func<string?> profileProvider,
        IConfigProfileStore profileStore)
    {
        return new ConfigManager(
            ConfigManagerSafetyTests.FakeModHelper.Create(".").Instance,
            ConfigManagerSafetyTests.NoOpMonitor.Create(),
            profileProvider,
            profileStore);
    }

    private sealed class InMemoryProfileStore : IConfigProfileStore
    {
        private readonly Dictionary<string, ModConfig> values = [];

        public List<string> Writes { get; } = [];

        public Exception? ReadException { get; init; }

        public ModConfig? Read(string profileKey)
        {
            if (this.ReadException is not null)
                throw this.ReadException;

            return this.values.GetValueOrDefault(profileKey)?.CreateDraft();
        }

        public void Write(string profileKey, ModConfig config)
        {
            this.values[profileKey] = config.CreateDraft();
            this.Writes.Add(profileKey);
        }

        public void Seed(string profileKey, ModConfig config)
        {
            this.values[profileKey] = config.CreateDraft();
        }
    }
}
