using SwordMastery.Models;

namespace SwordMastery.Services;

internal sealed class ProgressionService
{
    private readonly ModConfig Config;

    public ProgressionService(ModConfig config)
    {
        Config = config;
    }

    public int GetRequiredXp(int currentLevel)
    {
        return Config.BaseXpRequirement + (currentLevel * Config.XpGrowthPerLevel);
    }

    public int AddMonsterKills(SaveData data, int killCount)
    {
        if (killCount <= 0 || data.SwordLevel >= Config.MaxSwordLevel)
            return 0;

        data.SwordExperience += killCount * Config.ExpPerMonsterKill;

        int levelsGained = 0;

        while (data.SwordLevel < Config.MaxSwordLevel)
        {
            int required = GetRequiredXp(data.SwordLevel);
            if (data.SwordExperience < required)
                break;

            data.SwordExperience -= required;
            data.SwordLevel++;
            data.UnspentSkillPoints++;
            levelsGained++;
        }

        if (data.SwordLevel >= Config.MaxSwordLevel)
            data.SwordExperience = 0;

        return levelsGained;
    }
}
