using StardewModdingAPI;

namespace SwordMastery;

internal sealed class ModConfig
{
    // Mod display language. This is intentionally independent from the
    // Stardew Valley game language.
    public string Language { get; set; } = "English";
    public bool ShowHud { get; set; } = true;
    public int HudX { get; set; } = 24;
    public int HudY { get; set; } = 24;

    public bool ShowCooldownHud { get; set; } = true;
    public int CooldownHudX { get; set; } = 24;
    public int CooldownHudY { get; set; } = 88;
    public SButton OpenMenuKey { get; set; } = SButton.K;

    // Combat skill hotkeys (PC test defaults; configurable through GMCM).
    public SButton BasicSkillAKey { get; set; } = SButton.F6;
    public SButton BasicSkillBKey { get; set; } = SButton.F7;
    public SButton BasicSkillCKey { get; set; } = SButton.F8;

    // Selected branch activation keys.
    // Ohgi B and Ultimate A are passive, so these keys are used by the active branches.
    public SButton OhgiSkillKey { get; set; } = SButton.F9;
    public SButton UltimateSkillKey { get; set; } = SButton.F10;

    public int MaxSwordLevel { get; set; } = 75;
    public int ExpPerMonsterKill { get; set; } = 10;

    // Required XP for the next level:
    // BaseXpRequirement + (currentLevel * XpGrowthPerLevel)
    public int BaseXpRequirement { get; set; } = 40;
    public int XpGrowthPerLevel { get; set; } = 4;

    public int ResetCost { get; set; } = 1_000_000;
}
