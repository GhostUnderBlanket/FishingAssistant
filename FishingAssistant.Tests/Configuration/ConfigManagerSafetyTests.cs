using FishingAssistant.Configuration;
using StardewModdingAPI;
using System.Reflection;

namespace FishingAssistant.Tests.Configuration;

public sealed class ConfigManagerSafetyTests
{
    [Fact]
    public void Load_MalformedFileUsesDefaultsWithoutOverwritingOriginal()
    {
        using TemporaryDirectory directory = new();
        string configPath = Path.Combine(directory.Path, "config.json");
        const string malformedJson = "{ definitely not valid JSON";
        File.WriteAllText(configPath, malformedJson);
        FakeModHelper helper = FakeModHelper.Create(directory.Path, readException: new InvalidDataException());
        IMonitor monitor = NoOpMonitor.Create();

        ConfigManager manager = new(helper.Instance, monitor);
        ConfigValidationReport report = manager.Load();

        Assert.Equal(ModConfig.CurrentVersion, manager.Active.ConfigVersion);
        Assert.Single(report.Warnings);
        Assert.Equal("config.json", report.Warnings[0].Property);
        Assert.Equal(0, helper.WriteCount);
        Assert.Equal(malformedJson, File.ReadAllText(configPath));
    }

    [Fact]
    public void Load_FutureSchemaRemainsReadOnlyAndApplyCannotOverwriteIt()
    {
        using TemporaryDirectory directory = new();
        string configPath = Path.Combine(directory.Path, "config.json");
        int futureVersion = ModConfig.CurrentVersion + 1;
        string originalJson = $$"""{"ConfigVersion":{{futureVersion}},"FutureOption":"keep me"}""";
        File.WriteAllText(configPath, originalJson);
        FakeModHelper helper = FakeModHelper.Create(directory.Path, new ModConfig { ConfigVersion = futureVersion });
        IMonitor monitor = NoOpMonitor.Create();

        ConfigManager manager = new(helper.Instance, monitor);
        ConfigValidationReport report = manager.Load();
        ConfigEditSession session = manager.CreateEditSession();

        Assert.Equal(futureVersion, manager.Active.ConfigVersion);
        Assert.Contains(report.Warnings, warning => warning.Property == nameof(ModConfig.ConfigVersion));
        Assert.Throws<InvalidOperationException>(() => manager.Apply(session));
        Assert.Equal(0, helper.WriteCount);
        Assert.Equal(originalJson, File.ReadAllText(configPath));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            this.Path = Directory.CreateTempSubdirectory("FishingAssistant.Tests-").FullName;
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(this.Path, recursive: true);
        }
    }

    public class FakeModHelper : DispatchProxy
    {
        private string directoryPath = "";
        private ModConfig? config;
        private Exception? readException;

        public IModHelper Instance { get; private set; } = null!;

        public int WriteCount { get; private set; }

        internal static FakeModHelper Create(
            string directoryPath,
            ModConfig? config = null,
            Exception? readException = null)
        {
            IModHelper instance = Create<IModHelper, FakeModHelper>();
            FakeModHelper proxy = (FakeModHelper)(object)instance;
            proxy.Instance = instance;
            proxy.directoryPath = directoryPath;
            proxy.config = config;
            proxy.readException = readException;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_DirectoryPath" => this.directoryPath,
                "ReadConfig" => this.ReadConfig(),
                "WriteConfig" => this.RecordWrite(),
                _ => GetDefault(targetMethod?.ReturnType)
            };
        }

        private ModConfig ReadConfig()
        {
            if (this.readException is not null)
                throw this.readException;
            return this.config ?? new ModConfig();
        }

        private object? RecordWrite()
        {
            this.WriteCount++;
            return null;
        }
    }

    public class NoOpMonitor : DispatchProxy
    {
        public static IMonitor Create()
        {
            return Create<IMonitor, NoOpMonitor>();
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return GetDefault(targetMethod?.ReturnType);
        }
    }

    private static object? GetDefault(Type? type)
    {
        return type is null || type == typeof(void) || !type.IsValueType
            ? null
            : Activator.CreateInstance(type);
    }
}
