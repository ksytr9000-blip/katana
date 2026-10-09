using System.Reflection;
using System.Text.Json;
using StardewModdingAPI;

namespace SwordMastery;

internal static class I18n
{
    private static Dictionary<string, string> English = new();
    private static Dictionary<string, string> Korean = new();
    private static Func<string> CurrentLanguage = () => "English";

    public static void Init(
        IModHelper helper,
        Func<string> currentLanguage
    )
    {
        CurrentLanguage = currentLanguage;

        English = LoadDictionary(
            Path.Combine(
                helper.DirectoryPath,
                "i18n",
                "default.json"
            )
        );

        Korean = LoadDictionary(
            Path.Combine(
                helper.DirectoryPath,
                "i18n",
                "ko.json"
            )
        );
    }

    private static Dictionary<string, string> LoadDictionary(
        string path
    )
    {
        try
        {
            string json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<
                    Dictionary<string, string>
                >(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    public static string Get(
        string key,
        object? tokens = null
    )
    {
        Dictionary<string, string> selected =
            string.Equals(
                CurrentLanguage(),
                "Korean",
                StringComparison.OrdinalIgnoreCase
            )
                ? Korean
                : English;

        if (!selected.TryGetValue(key, out string? value)
            && !English.TryGetValue(key, out value))
        {
            return key;
        }

        if (tokens is null)
            return value;

        foreach (
            PropertyInfo property
            in tokens.GetType().GetProperties(
                BindingFlags.Instance
                | BindingFlags.Public
            )
        )
        {
            string token =
                "{{" + property.Name + "}}";

            string replacement =
                property.GetValue(tokens)?.ToString()
                ?? string.Empty;

            value = value.Replace(
                token,
                replacement,
                StringComparison.Ordinal
            );
        }

        return value;
    }
}
