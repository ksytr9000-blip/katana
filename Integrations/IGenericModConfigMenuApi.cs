using StardewModdingAPI;

namespace SwordMastery.Integrations;

/// <summary>
/// Minimal Generic Mod Config Menu API used by Sword Mastery.
/// This is copied as an interface only, so GMCM remains an optional dependency.
/// </summary>
internal interface IGenericModConfigMenuApi
{
    void Register(
        IManifest mod,
        Action reset,
        Action save,
        bool titleScreenOnly = false
    );

    void AddSectionTitle(
        IManifest mod,
        Func<string> text,
        Func<string>? tooltip = null
    );

    void AddBoolOption(
        IManifest mod,
        Func<bool> getValue,
        Action<bool> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null
    );

    void AddKeybind(
        IManifest mod,
        Func<SButton> getValue,
        Action<SButton> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null
    );
}
