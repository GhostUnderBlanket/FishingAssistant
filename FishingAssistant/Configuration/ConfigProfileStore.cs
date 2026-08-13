using StardewModdingAPI;

namespace FishingAssistant.Configuration;

internal interface IConfigProfileStore
{
    ModConfig? Read(string profileKey);

    void Write(string profileKey, ModConfig config);
}

internal sealed class ConfigProfileStore(IDataHelper data) : IConfigProfileStore
{
    private const string DirectoryName = "config.players";

    public ModConfig? Read(string profileKey)
    {
        return data.ReadJsonFile<ModConfig>(GetPath(profileKey));
    }

    public void Write(string profileKey, ModConfig config)
    {
        data.WriteJsonFile(GetPath(profileKey), config);
    }

    private static string GetPath(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey)
            || profileKey.Any(character => !IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException("The configuration profile key contains unsupported characters.",
                nameof(profileKey));
        }

        return $"{DirectoryName}/{profileKey}.json";
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return character is >= '0' and <= '9'
            or >= 'A' and <= 'Z'
            or >= 'a' and <= 'z';
    }
}
