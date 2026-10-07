using StardewModdingAPI;

namespace SwordMastery;

internal sealed class ModConfig
{
    public bool ShowHud { get; set; } = true;
    public SButton OpenMenuKey { get; set; } = SButton.K;

    public int MaxSwordLevel { get; set; } = 75;
    public int ExpPerMonsterKill { get; set; } = 10;

    // Required XP for the next level:
    // BaseXpRequirement + (currentLevel * XpGrowthPerLevel)
    public int BaseXpRequirement { get; set; } = 40;
    public int XpGrowthPerLevel { get; set; } = 4;

    public int ResetCost { get; set; } = 1_000_000;
}
