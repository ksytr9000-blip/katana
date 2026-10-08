using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SwordMastery.Models;
using SwordMastery.Services;
using SwordMastery.UI;
using SwordMastery.Integrations;

namespace SwordMastery;

internal sealed class ModEntry : Mod
{
    private const string SaveKey = "SwordMastery.SaveData";

    private ModConfig Config = null!;
    private SaveData Data = new();
    private ProgressionService Progression = null!;
    private SkillService Skills = null!;
    private CombatService Combat = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Progression = new ProgressionService(Config);
        Skills = new SkillService();
        Combat = new CombatService(Monitor);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add("sm_status", "Show Sword Mastery status.", CommandStatus);
        helper.ConsoleCommands.Add("sm_add", "Allocate a point. Usage: sm_add basic A | sm_add ohgi | sm_add ultimate", CommandAdd);
        helper.ConsoleCommands.Add("sm_choose_ohgi", "Choose Ohgi branch A/B/C.", CommandChooseOhgi);
        helper.ConsoleCommands.Add("sm_choose_ultimate", "Choose Ultimate branch A/B.", CommandChooseUltimate);
        helper.ConsoleCommands.Add("sm_grant_ohgi", "DEBUG: mark Ohgi unlock quest completed.", CommandGrantOhgi);
        helper.ConsoleCommands.Add("sm_grant_ultimate", "DEBUG: mark Ultimate unlock quest completed.", CommandGrantUltimate);
        helper.ConsoleCommands.Add("sm_reset", "Pay the configured gold cost and schedule respec for next morning.", CommandReset);

        Monitor.Log("Sword Mastery prototype loaded.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? gmcm =
            Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

        if (gmcm is null)
        {
            Monitor.Log(
                "Generic Mod Config Menu not found. In-game config menu integration is disabled.",
                LogLevel.Trace
            );
            return;
        }

        gmcm.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () => Helper.WriteConfig(Config)
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => "검술 모드 설정"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.OpenMenuKey,
            setValue: value => Config.OpenMenuKey = value,
            name: () => "검술창 열기 키",
            tooltip: () => "검술 레벨과 스킬 배분창을 열고 닫는 키입니다.",
            fieldId: "OpenMenuKey"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => "기초검술 단축키"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.BasicSkillAKey,
            setValue: value => Config.BasicSkillAKey = value,
            name: () => "검사의 발걸음",
            tooltip: () => "기초검술 A: 전방 대쉬 계열 기술입니다.",
            fieldId: "BasicSkillAKey"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.BasicSkillBKey,
            setValue: value => Config.BasicSkillBKey = value,
            name: () => "참격",
            tooltip: () => "기초검술 B: 전방 베기 / 검기 계열 기술입니다.",
            fieldId: "BasicSkillBKey"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.BasicSkillCKey,
            setValue: value => Config.BasicSkillCKey = value,
            name: () => "칼리코류 검술",
            tooltip: () => "기초검술 C: 다단 검격 계열 기술입니다.",
            fieldId: "BasicSkillCKey"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => "오의 / 극의 단축키"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.OhgiSkillKey,
            setValue: value => Config.OhgiSkillKey = value,
            name: () => "오의 발동 키",
            tooltip: () => "일섬/검술의 정점 발동용입니다. 검기 오의는 기본 검 공격에 자동 발동됩니다.",
            fieldId: "OhgiSkillKey"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.UltimateSkillKey,
            setValue: value => Config.UltimateSkillKey = value,
            name: () => "극의 발동 키",
            tooltip: () => "보법의 극 발동용입니다. 검술의 극은 상시 자동 발동됩니다.",
            fieldId: "UltimateSkillKey"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => "HUD 설정"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => Config.ShowHud,
            setValue: value => Config.ShowHud = value,
            name: () => "검술 HUD 표시",
            tooltip: () => "검술 레벨 / EXP / SP 표시를 켜거나 끕니다.",
            fieldId: "ShowHud"
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Config.HudX,
            setValue: value => Config.HudX = value,
            name: () => "HUD X 위치",
            tooltip: () => "화면 왼쪽을 기준으로 HUD의 가로 위치를 조절합니다.",
            min: 0,
            max: 4000,
            interval: 8,
            formatValue: value => $"{value}px",
            fieldId: "HudX"
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Config.HudY,
            setValue: value => Config.HudY = value,
            name: () => "HUD Y 위치",
            tooltip: () => "화면 위쪽을 기준으로 HUD의 세로 위치를 조절합니다.",
            min: 0,
            max: 2400,
            interval: 8,
            formatValue: value => $"{value}px",
            fieldId: "HudY"
        );

        gmcm.AddPageLink(
            mod: ModManifest,
            pageId: "debug",
            text: () => "DEBUG 테스트 도구",
            tooltip: () => "개발/테스트용 수치를 직접 변경합니다."
        );

        gmcm.AddPage(
            mod: ModManifest,
            pageId: "debug",
            pageTitle: () => "Sword Mastery DEBUG"
        );

        gmcm.AddParagraph(
            mod: ModManifest,
            text: () => "※ 세이브를 불러온 상태에서만 적용됩니다. 테스트용 설정입니다."
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Context.IsWorldReady ? Data.SwordLevel : 0,
            setValue: value => ApplyDebugSwordLevel(value),
            name: () => "검술 레벨",
            tooltip: () => "검술 레벨을 즉시 변경합니다. 변경 시 현재 EXP는 0이 되고, 투자된 SP를 제외한 만큼 남은 SP를 자동 계산합니다.",
            min: 0,
            max: Config.MaxSwordLevel,
            interval: 1,
            fieldId: "DebugSwordLevel"
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Context.IsWorldReady ? Data.UnspentSkillPoints : 0,
            setValue: value =>
            {
                if (Context.IsWorldReady)
                    Data.UnspentSkillPoints = Math.Max(0, value);
            },
            name: () => "남은 SP",
            tooltip: () => "테스트를 위해 남은 스킬 포인트를 직접 설정합니다.",
            min: 0,
            max: 200,
            interval: 1,
            fieldId: "DebugSkillPoints"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => Context.IsWorldReady && Data.OhgiAccessGranted,
            setValue: value =>
            {
                if (!Context.IsWorldReady)
                    return;

                Data.OhgiAccessGranted = value;
                Data.OhgiQuestAvailable = false;

                if (!value)
                    Data.SelectedOhgi = null;
            },
            name: () => "오의 강제 해방",
            tooltip: () => "오의 선택/강화 UI 테스트용입니다. 끄면 선택한 오의 분기도 해제됩니다.",
            fieldId: "DebugOhgiAccess"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => Context.IsWorldReady && Data.UltimateAccessGranted,
            setValue: value =>
            {
                if (!Context.IsWorldReady)
                    return;

                Data.UltimateAccessGranted = value;
                Data.UltimateQuestAvailable = false;

                if (!value)
                    Data.SelectedUltimate = null;
            },
            name: () => "극의 강제 해방",
            tooltip: () => "극의 선택/강화 UI 테스트용입니다. 끄면 선택한 극의 분기도 해제됩니다.",
            fieldId: "DebugUltimateAccess"
        );

        Monitor.Log("Generic Mod Config Menu integration registered.", LogLevel.Info);
    }

    private void ApplyDebugSwordLevel(int value)
    {
        if (!Context.IsWorldReady)
            return;

        int level = Math.Clamp(value, 0, Config.MaxSwordLevel);
        int spentPoints = Data.Skills.Values.Sum(skill => skill.TotalPoints);

        Data.SwordLevel = level;
        Data.SwordExperience = 0;
        Data.UnspentSkillPoints = Math.Max(0, level - spentPoints);

        RefreshUnlockState(showMessages: false);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // Sword Mastery menu.
        if (e.Button == Config.OpenMenuKey)
        {
            if (Game1.activeClickableMenu is SkillTreeMenu)
            {
                Game1.exitActiveMenu();
                return;
            }

            if (Game1.activeClickableMenu is not null)
                return;

            RefreshUnlockState(showMessages: false);
            Game1.activeClickableMenu = new SkillTreeMenu(Data, Config, Progression, Skills, Helper);
            Game1.playSound("bigSelect");
            return;
        }

        // Don't fire combat skills while another menu/cutscene/input lock is active.
        if (Game1.activeClickableMenu is not null || !Context.IsPlayerFree)
            return;

        // Ohgi B "검기" is passive: normal sword attacks launch the wave.
        if (e.Button == SButton.MouseLeft || e.Button == SButton.C || e.Button == SButton.ControllerX)
            Combat.OnVanillaSwordAttack(Data);

        string? branch = null;

        if (e.Button == Config.BasicSkillAKey)
            branch = "A";
        else if (e.Button == Config.BasicSkillBKey)
            branch = "B";
        else if (e.Button == Config.BasicSkillCKey)
            branch = "C";

        if (branch is not null)
        {
            if (Combat.TryUseBasicSkill(branch, Data, out string basicMessage))
            {
                Helper.Input.Suppress(e.Button);
            }
            else if (!string.IsNullOrWhiteSpace(basicMessage))
            {
                Game1.addHUDMessage(new HUDMessage(basicMessage));
                Game1.playSound("cancel");
            }

            return;
        }

        if (e.Button == Config.OhgiSkillKey)
        {
            if (Combat.TryUseOhgi(Data, out string ohgiMessage))
            {
                Helper.Input.Suppress(e.Button);
            }
            else if (!string.IsNullOrWhiteSpace(ohgiMessage))
            {
                Game1.addHUDMessage(new HUDMessage(ohgiMessage));
                Game1.playSound("cancel");
            }

            return;
        }

        if (e.Button == Config.UltimateSkillKey)
        {
            if (Combat.TryUseUltimate(Data, out string ultimateMessage))
            {
                Helper.Input.Suppress(e.Button);
            }
            else if (!string.IsNullOrWhiteSpace(ultimateMessage))
            {
                Game1.addHUDMessage(new HUDMessage(ultimateMessage));
                Game1.playSound("cancel");
            }
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Data = Helper.Data.ReadSaveData<SaveData>(SaveKey) ?? new SaveData();
        Data.EnsureSkillKeys();

        int currentKills = Convert.ToInt32(Game1.player.stats.MonstersKilled);

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
        if (!Context.IsWorldReady)
            return;

        Combat.Update(Data);

        if (!e.IsMultipleOf(15))
            return;

        int currentKills = Convert.ToInt32(Game1.player.stats.MonstersKilled);
        int gainedKills = currentKills - Data.LastObservedMonsterKills;

        if (gainedKills > 0)
        {
            int levels = Progression.AddMonsterKills(Data, gainedKills);
            Data.LastObservedMonsterKills = currentKills;

            if (levels > 0)
            {
                RefreshUnlockState(showMessages: true);

                Game1.addHUDMessage(new HUDMessage(
                    Helper.Translation.Get("level.up", new { level = Data.SwordLevel }),
                    HUDMessage.newQuest_type
                ));
            }
        }
        else if (gainedKills < 0)
        {
            Data.LastObservedMonsterKills = currentKills;
        }
    }

    private void RefreshUnlockState(bool showMessages)
    {
        bool prevOhgiQuest = Data.OhgiQuestAvailable;
        bool prevUltimateQuest = Data.UltimateQuestAvailable;
        bool prevOhgiAccess = Data.OhgiAccessGranted;
        bool prevUltimateAccess = Data.UltimateAccessGranted;

        Skills.UpdateUnlockState(Data);

        if (showMessages)
            NotifyUnlockChanges(prevOhgiQuest, prevUltimateQuest, prevOhgiAccess, prevUltimateAccess);
    }

    private void NotifyUnlockChanges(
        bool prevOhgiQuest,
        bool prevUltimateQuest,
        bool prevOhgiAccess,
        bool prevUltimateAccess
    )
    {
        if (!Context.IsWorldReady)
            return;

        if (!prevOhgiQuest && Data.OhgiQuestAvailable)
        {
            Game1.addHUDMessage(new HUDMessage("오의 해방 퀘스트가 생겼습니다.", HUDMessage.newQuest_type));
            Game1.playSound("questcomplete");
        }

        if (!prevUltimateQuest && Data.UltimateQuestAvailable)
        {
            Game1.addHUDMessage(new HUDMessage("극의 해방 퀘스트가 생겼습니다.", HUDMessage.newQuest_type));
            Game1.playSound("questcomplete");
        }

        if (!prevOhgiAccess && Data.OhgiAccessGranted)
            Game1.addHUDMessage(new HUDMessage("오의가 해방되었습니다.", HUDMessage.newQuest_type));

        if (!prevUltimateAccess && Data.UltimateAccessGranted)
            Game1.addHUDMessage(new HUDMessage("극의가 해방되었습니다.", HUDMessage.newQuest_type));
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Combat.Draw(e.SpriteBatch);
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Config.ShowHud || !Context.IsWorldReady || Game1.activeClickableMenu is not null)
            return;

        string text;
        if (Data.SwordLevel >= Config.MaxSwordLevel)
        {
            text = $"검술 Lv.{Data.SwordLevel}  MASTER  SP:{Data.UnspentSkillPoints}";
        }
        else
        {
            int req = Progression.GetRequiredXp(Data.SwordLevel);
            text = $"검술 Lv.{Data.SwordLevel}  EXP {Data.SwordExperience}/{req}  SP:{Data.UnspentSkillPoints}";
        }

        float hudX = Math.Clamp(Config.HudX, 0, Math.Max(0, Game1.uiViewport.Width - 220));
        float hudY = Math.Clamp(Config.HudY, 0, Math.Max(0, Game1.uiViewport.Height - 80));

        Vector2 pos = new(hudX, hudY);
        e.SpriteBatch.DrawString(Game1.smallFont, text, pos + new Vector2(2f, 2f), Color.Black * 0.7f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, pos, Color.White);

        string prompt = $"[{Config.OpenMenuKey}] 검술창";
        Vector2 promptPos = new(hudX, hudY + 28f);
        e.SpriteBatch.DrawString(Game1.smallFont, prompt, promptPos + new Vector2(2f, 2f), Color.Black * 0.7f);
        e.SpriteBatch.DrawString(Game1.smallFont, prompt, promptPos, Color.White);
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
            RefreshUnlockState(showMessages: true);

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
        bool prevOhgiQuest = Data.OhgiQuestAvailable;
        bool prevUltimateQuest = Data.UltimateQuestAvailable;
        bool prevOhgiAccess = Data.OhgiAccessGranted;
        bool prevUltimateAccess = Data.UltimateAccessGranted;

        Skills.GrantOhgiAccess(Data);
        NotifyUnlockChanges(prevOhgiQuest, prevUltimateQuest, prevOhgiAccess, prevUltimateAccess);
        Monitor.Log("DEBUG: Ohgi access granted.", LogLevel.Info);
    }

    private void CommandGrantUltimate(string command, string[] args)
    {
        bool prevOhgiQuest = Data.OhgiQuestAvailable;
        bool prevUltimateQuest = Data.UltimateQuestAvailable;
        bool prevOhgiAccess = Data.OhgiAccessGranted;
        bool prevUltimateAccess = Data.UltimateAccessGranted;

        Skills.GrantUltimateAccess(Data);
        NotifyUnlockChanges(prevOhgiQuest, prevUltimateQuest, prevOhgiAccess, prevUltimateAccess);
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
