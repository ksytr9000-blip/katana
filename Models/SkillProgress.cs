namespace SwordMastery.Models;

internal sealed class SkillProgress
{
    public int Stage1 { get; set; }
    public int Stage2 { get; set; }
    public int Stage3 { get; set; }

    public int TotalPoints => Stage1 + Stage2 + Stage3;
    public bool IsMastered => Stage1 >= 5 && Stage2 >= 5 && Stage3 >= 5;

    public int CurrentStage
    {
        get
        {
            if (Stage1 < 5) return 1;
            if (Stage2 < 5) return 2;
            return 3;
        }
    }

    public bool TryAddPoint()
    {
        if (Stage1 < 5)
        {
            Stage1++;
            return true;
        }

        if (Stage2 < 5)
        {
            Stage2++;
            return true;
        }

        if (Stage3 < 5)
        {
            Stage3++;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        Stage1 = 0;
        Stage2 = 0;
        Stage3 = 0;
    }
}
