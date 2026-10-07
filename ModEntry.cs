using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SwordMastery.Models;
using SwordMastery.Services;

namespace SwordMastery;

internal sealed class ModEntry : Mod
{
    private const string SaveKey = "SwordMastery.SaveData";

    private ModConfig Config = null!;
    private SaveData Data = new();
    private ProgressionService Progression = null!;
    private SkillService Skills = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Progression = new ProgressionService(Config);
        Skills = new SkillService();

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;

        helper.ConsoleCommands.Add("sm_status", "Show Sword Mastery status.", CommandStatus);
        helper.ConsoleCommands.Add("sm_add", "Allocate a point. Usage: sm_add basic A | sm_add ohgi | sm_add ultimate", CommandAdd);
        helper.ConsoleCommands.Add("sm_choose_ohgi", "Choose Ohgi branch A/B/C.", CommandChooseOhgi);
        helper.ConsoleCommands.Add("sm_choose_ultimate", "Choose Ultimate branch A/B.", CommandChooseUltimate);
        helper.ConsoleCommands.Add("sm_grant_ohgi", "DEBUG: mark Ohgi unlock quest completed.", CommandGrantOhgi);
        helper.ConsoleCommands.Add("sm_grant_ultimate", "DEBUG: mark Ultimate unlock quest completed.", CommandGrantUltimate);
        helper.ConsoleCommands.Add("sm_reset", "Pay the configured gold cost and schedule respec for next morning.", CommandReset);

        Monitor.Log("Sword Mastery prototype loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Data = Helper.Data.ReadSaveData<SaveData>(SaveKey) ?? new SaveData();
        Data.EnsureSkillKeys();

        int currentKills = Convert.ToInt32(Game1.player.stats.Get(StatKeys.MonstersKilled));

        // Installing this mod into an old save should not retroactively grant XP
        // for every monster killed before the mod was installed.
        if (!Data.KillCounterInitialized)
        {
            Data.LastObservedMonsterKills = currentKills;
            Data.KillCounterInitialized = true;
        }

        Skills.UpdateUnlockState(Data);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        Helper.Data.WriteSaveData(SaveKey, Data);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!Data.PendingNightReset)
            return;

        Skills.ExecuteRespec(Data);
        Game1.addHUDMessage(new HUDMessage(
            Helper.Translation.Get("reset.completed"),
            HUDMessage.newQuest_type
        ));
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(15))
            return;

        int currentKills = Convert.ToInt32(Game1.player.stats.Get(StatKeys.MonstersKilled));
        int gainedKills = currentKills - Data.LastObservedMonsterKills;

        if (gainedKills > 0)
        {
            int levels = Progression.AddMonsterKills(Data, gainedKills);
            Data.LastObservedMonsterKills = currentKills;

            if (levels > 0)
            {
                Skills.UpdateUnlockState(Data);

                Game1.addHUDMessage(new HUDMessage(
                    Helper.Translation.Get("level.up", new { level = Data.SwordLevel }),
                    HUDMessage.newQuest_type
                ));
            }
        }
        else if (gainedKills < 0)
        {
            // Safety for unusual save/stat changes.
            Data.LastObservedMonsterKills = currentKills;
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Config.ShowHud || !Context.IsWorldReady || Game1.activeClickableMenu is not null)
            return;

        string text;
        if (Data.SwordLevel >= Config.MaxSwordLevel)
        {
            text = $"Sword Lv.{Data.SwordLevel}  MASTER  SP:{Data.UnspentSkillPoints}";
        }
        else
        {
            int req = Progression.GetRequiredXp(Data.SwordLevel);
            text = $"Sword Lv.{Data.SwordLevel}  EXP {Data.SwordExperience}/{req}  SP:{Data.UnspentSkillPoints}";
        }

        Vector2 pos = new(24f, 24f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, pos + new Vector2(2f, 2f), Color.Black * 0.7f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, pos, Color.White);
    }

    private void CommandStatus(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }

        Skills.UpdateUnlockState(Data);

        Monitor.Log(
            $"Sword Lv.{Data.SwordLevel}/{Config.MaxSwordLevel}, " +
            $"XP={Data.SwordExperience}, SP={Data.UnspentSkillPoints}, " +
            $"Ohgi={Data.SelectedOhgi ?? "none"} access={Data.OhgiAccessGranted} quest={Data.OhgiQuestAvailable}, " +
            $"Ultimate={Data.SelectedUltimate ?? "none"} access={Data.UltimateAccessGranted} quest={Data.UltimateQuestAvailable}",
            LogLevel.Info
        );

        foreach (var pair in Data.Skills)
        {
            SkillProgress s = pair.Value;
            Monitor.Log($"{pair.Key}: {s.Stage1}/5 -> {s.Stage2}/5 -> {s.Stage3}/5", LogLevel.Info);
        }
    }

    private void CommandAdd(string command, string[] args)
    {
        if (!Context.IsWorldReady || args.Length == 0)
            return;

        bool ok;
        string message;

        switch (args[0].ToLowerInvariant())
        {
            case "basic":
                if (args.Length < 2)
                {
                    Monitor.Log("Usage: sm_add basic A|B|C", LogLevel.Warn);
                    return;
                }

                ok = Skills.TryAllocateBasic(Data, args[1], out message);
                break;

            case "ohgi":
                ok = Skills.TryAllocateOhgi(Data, out message);
                break;

            case "ultimate":
                ok = Skills.TryAllocateUltimate(Data, out message);
                break;

            default:
                Monitor.Log("Usage: sm_add basic A|B|C | sm_add ohgi | sm_add ultimate", LogLevel.Warn);
                return;
        }

        if (ok)
            Skills.UpdateUnlockState(Data);

        Monitor.Log(message, ok ? LogLevel.Info : LogLevel.Warn);
    }

    private void CommandChooseOhgi(string command, string[] args)
    {
        if (!Context.IsWorldReady || args.Length < 1)
            return;

        bool ok = Skills.TrySelectOhgi(Data, args[0], out string message);
        Monitor.Log(message, ok ? LogLevel.Info : LogLevel.Warn);
    }

    private void CommandChooseUltimate(string command, string[] args)
    {
        if (!Context.IsWorldReady || args.Length < 1)
            return;

        bool ok = Skills.TrySelectUltimate(Data, args[0], out string message);
        Monitor.Log(message, ok ? LogLevel.Info : LogLevel.Warn);
    }

    private void CommandGrantOhgi(string command, string[] args)
    {
        Skills.GrantOhgiAccess(Data);
        Monitor.Log("DEBUG: Ohgi access granted.", LogLevel.Info);
    }

    private void CommandGrantUltimate(string command, string[] args)
    {
        Skills.GrantUltimateAccess(Data);
        Monitor.Log("DEBUG: Ultimate access granted.", LogLevel.Info);
    }

    private void CommandReset(string command, string[] args)
    {
        if (!Context.IsWorldReady)
            return;

        if (Data.PendingNightReset)
        {
            Monitor.Log("A sword-skill reset is already scheduled for the next morning.", LogLevel.Warn);
            return;
        }

        if (Game1.player.Money < Config.ResetCost)
        {
            Monitor.Log($"Need {Config.ResetCost:N0}g.", LogLevel.Warn);
            return;
        }

        Game1.player.Money -= Config.ResetCost;
        Data.PendingNightReset = true;

        Game1.addHUDMessage(new HUDMessage(
            Helper.Translation.Get("reset.scheduled", new { cost = Config.ResetCost }),
            HUDMessage.newQuest_type
        ));
    }
}
