using StardewModdingAPI;

namespace SwordMastery;

internal sealed class ModConfig
{
    public bool ShowHud { get; set; } = true;
    public int HudX { get; set; } = 24;
    public int HudY { get; set; } = 24;
    public SButton OpenMenuKey { get; set; } = SButton.K;

    // Combat skill hotkeys (PC test defaults; configurable through GMCM).
    public SButton BasicSkillAKey { get; set; } = SButton.F6;
    public SButton BasicSkillBKey { get; set; } = SButton.F7;
    public SButton BasicSkillCKey { get; set; } = SButton.F8;

    public int MaxSwordLevel { get; set; } = 75;
    public int ExpPerMonsterKill { get; set; } = 10;

    // Required XP for the next level:
    // BaseXpRequirement + (currentLevel * XpGrowthPerLevel)
    public int BaseXpRequirement { get; set; } = 40;
    public int XpGrowthPerLevel { get; set; } = 4;

    public int ResetCost { get; set; } = 1_000_000;
}
