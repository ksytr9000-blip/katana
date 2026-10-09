using System.Collections.Generic;

namespace SwordMastery.Models;

internal sealed class SaveData
{
    public int SwordLevel { get; set; }
    public int SwordExperience { get; set; }
    public int UnspentSkillPoints { get; set; }

    // Remember the last equipped melee weapon's average damage so active sword skills
    // can still be used after switching to another tool or empty hand.
    public int LastSwordReferenceDamage { get; set; } = 20;

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

    // Quest availability flags. Unlock items consume these by granting permanent access.
    public bool OhgiQuestAvailable { get; set; }
    public bool UltimateQuestAvailable { get; set; }

    // Unlock quest state/progress.
    public bool OhgiQuestStarted { get; set; }
    public bool OhgiQuestCompleted { get; set; }
    public int OhgiSkullKills { get; set; }

    // Journal reward state.
    // RewardReady = objectives are complete and the player must claim the reward
    // from the vanilla journal reward box.
    // RewardDeliveryPending = the journal reward was claimed, but the inventory
    // was full; the item will be inserted automatically once a slot is available.
    public bool OhgiRewardReady { get; set; }
    public bool OhgiRewardDeliveryPending { get; set; }

    public bool UltimateQuestStarted { get; set; }
    public bool UltimateQuestCompleted { get; set; }
    public int UltimateDragonKills { get; set; }

    public bool UltimateRewardReady { get; set; }
    public bool UltimateRewardDeliveryPending { get; set; }

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
