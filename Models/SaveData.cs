using System.Collections.Generic;

namespace SwordMastery.Models;

internal sealed class SaveData
{
    public int SwordLevel { get; set; }
    public int SwordExperience { get; set; }
    public int UnspentSkillPoints { get; set; }

    public int LastObservedMonsterKills { get; set; }
    public bool KillCounterInitialized { get; set; }

    public Dictionary<string, SkillProgress> Skills { get; set; } = CreateDefaultSkills();

    // Branch selection.
    // Ohgi: A = Issen, B = Sword Wave, C = Pinnacle of Swordsmanship
    // Ultimate: A = Ultimate of Swordsmanship, B = Ultimate Footwork
    public string? SelectedOhgi { get; set; }
    public string? SelectedUltimate { get; set; }

    // Quest completion/access is permanent even after a paid respec.
    public bool OhgiAccessGranted { get; set; }
    public bool UltimateAccessGranted { get; set; }

    // Quest availability flags. Actual quest/item implementation comes next.
    public bool OhgiQuestAvailable { get; set; }
    public bool UltimateQuestAvailable { get; set; }

    public bool PendingNightReset { get; set; }

    public static Dictionary<string, SkillProgress> CreateDefaultSkills()
    {
        return new()
        {
            ["BasicA"] = new SkillProgress(),
            ["BasicB"] = new SkillProgress(),
            ["BasicC"] = new SkillProgress(),

            ["OhgiA"] = new SkillProgress(),
            ["OhgiB"] = new SkillProgress(),
            ["OhgiC"] = new SkillProgress(),

            ["UltimateA"] = new SkillProgress(),
            ["UltimateB"] = new SkillProgress()
        };
    }

    public void EnsureSkillKeys()
    {
        foreach (string key in CreateDefaultSkills().Keys)
        {
            if (!Skills.ContainsKey(key))
                Skills[key] = new SkillProgress();
        }
    }
}
