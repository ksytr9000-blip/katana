using StardewModdingAPI;

namespace SwordMastery;

internal static class I18n
{
    private static ITranslationHelper Translation = null!;

    public static void Init(IModHelper helper)
    {
        Translation = helper.Translation;
    }

    public static string Get(string key, object? tokens = null)
    {
        return Translation.Get(key, tokens).ToString();
    }
}
