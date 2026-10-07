using SwordMastery.Models;

namespace SwordMastery.Services;

internal sealed class SkillService
{
    private static readonly Dictionary<string, string> OhgiRequirements = new()
    {
        ["A"] = "BasicA",
        ["B"] = "BasicB",
        ["C"] = "BasicC"
    };

    private static readonly Dictionary<string, string> OhgiSkillIds = new()
    {
        ["A"] = "OhgiA",
        ["B"] = "OhgiB",
        ["C"] = "OhgiC"
    };

    private static readonly Dictionary<string, string> UltimateSkillIds = new()
    {
        ["A"] = "UltimateA",
        ["B"] = "UltimateB"
    };

    public bool TryAllocateBasic(SaveData data, string branch, out string message)
    {
        string id = $"Basic{branch.ToUpperInvariant()}";

        if (!data.Skills.TryGetValue(id, out SkillProgress? skill))
        {
            message = "Unknown basic branch.";
            return false;
        }

        return TrySpendPoint(data, skill, out message);
    }

    public bool TrySelectOhgi(SaveData data, string branch, out string message)
    {
        branch = branch.ToUpperInvariant();

        if (!data.OhgiAccessGranted)
        {
            message = "Ohgi access has not been granted yet.";
            return false;
        }

        if (data.SelectedOhgi is not null)
        {
            message = $"Ohgi branch is already locked to {data.SelectedOhgi}.";
            return false;
        }

        if (!OhgiRequirements.TryGetValue(branch, out string? requiredBasic))
        {
            message = "Unknown Ohgi branch.";
            return false;
        }

        if (!data.Skills[requiredBasic].IsMastered)
        {
            message = $"{requiredBasic} must be mastered first.";
            return false;
        }

        data.SelectedOhgi = branch;
        message = $"Ohgi branch {branch} selected.";
        return true;
    }

    public bool TryAllocateOhgi(SaveData data, out string message)
    {
        if (data.SelectedOhgi is null)
        {
            message = "Select an Ohgi branch first.";
            return false;
        }

        SkillProgress skill = data.Skills[OhgiSkillIds[data.SelectedOhgi]];
        return TrySpendPoint(data, skill, out message);
    }

    public bool TrySelectUltimate(SaveData data, string branch, out string message)
    {
        branch = branch.ToUpperInvariant();

        if (!data.UltimateAccessGranted)
        {
            message = "Ultimate access has not been granted yet.";
            return false;
        }

        if (data.SelectedUltimate is not null)
        {
            message = $"Ultimate branch is already locked to {data.SelectedUltimate}.";
            return false;
        }

        if (!UltimateSkillIds.ContainsKey(branch))
        {
            message = "Unknown Ultimate branch.";
            return false;
        }

        data.SelectedUltimate = branch;
        message = $"Ultimate branch {branch} selected.";
        return true;
    }

    public bool TryAllocateUltimate(SaveData data, out string message)
    {
        if (data.SelectedUltimate is null)
        {
            message = "Select an Ultimate branch first.";
            return false;
        }

        SkillProgress skill = data.Skills[UltimateSkillIds[data.SelectedUltimate]];
        return TrySpendPoint(data, skill, out message);
    }

    public void UpdateUnlockState(SaveData data)
    {
        int basicMasterCount = 0;
        foreach (string id in new[] { "BasicA", "BasicB", "BasicC" })
        {
            if (data.Skills[id].IsMastered)
                basicMasterCount++;
        }

        if (!data.OhgiAccessGranted && basicMasterCount >= 2)
            data.OhgiQuestAvailable = true;

        bool allBasicMastered = basicMasterCount == 3;
        bool selectedOhgiMastered =
            data.SelectedOhgi is not null
            && data.Skills[OhgiSkillIds[data.SelectedOhgi]].IsMastered;

        if (!data.UltimateAccessGranted && allBasicMastered && selectedOhgiMastered)
            data.UltimateQuestAvailable = true;
    }

    public void GrantOhgiAccess(SaveData data)
    {
        data.OhgiAccessGranted = true;
        data.OhgiQuestAvailable = false;
    }

    public void GrantUltimateAccess(SaveData data)
    {
        data.UltimateAccessGranted = true;
        data.UltimateQuestAvailable = false;
    }

    public void ExecuteRespec(SaveData data)
    {
        foreach (SkillProgress progress in data.Skills.Values)
            progress.Reset();

        data.SelectedOhgi = null;
        data.SelectedUltimate = null;

        // Refund exactly one point per sword level.
        data.UnspentSkillPoints = data.SwordLevel;

        // Quest access is intentionally preserved.
        data.OhgiQuestAvailable = false;
        data.UltimateQuestAvailable = false;
        data.PendingNightReset = false;
    }

    private static bool TrySpendPoint(SaveData data, SkillProgress skill, out string message)
    {
        if (data.UnspentSkillPoints <= 0)
        {
            message = "No unspent sword skill points.";
            return false;
        }

        if (skill.IsMastered)
        {
            message = "Skill is already mastered.";
            return false;
        }

        if (!skill.TryAddPoint())
        {
            message = "Could not allocate point.";
            return false;
        }

        data.UnspentSkillPoints--;
        message = "Point allocated.";
        return true;
    }
}
