using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Monsters;
using SwordMastery.Models;
using SwordMastery.Services;
using SwordMastery.UI;
using SwordMastery.Integrations;

namespace SwordMastery;

internal sealed class ModEntry : Mod
{
    private const string SaveKey = "SwordMastery.SaveData";

    private string OhgiSecretBookId => $"{ModManifest.UniqueID}_OhgiSecretBook";
    private string InsightDropId => $"{ModManifest.UniqueID}_InsightDrop";
    private string DragonOrbId => $"{ModManifest.UniqueID}_DragonOrb";

    private string OhgiQuestId => $"{ModManifest.UniqueID}_OhgiUnlockQuest";
    private string UltimateQuestId => $"{ModManifest.UniqueID}_UltimateUnlockQuest";

    private string UnlockItemTextureAsset => $"Mods/{ModManifest.UniqueID}/UnlockItems";

    private ModConfig Config = null!;
    private SaveData Data = new();
    private ProgressionService Progression = null!;
    private SkillService Skills = null!;
    private CombatService Combat = null!;
    private readonly Dictionary<string, Texture2D> CooldownIcons = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper);
        Config = helper.ReadConfig<ModConfig>();
        Progression = new ProgressionService(Config);
        Skills = new SkillService();
        Combat = new CombatService(Monitor);
        LoadCooldownIcons();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.World.NpcListChanged += OnNpcListChanged;

        helper.ConsoleCommands.Add("sm_status", "Show Sword Mastery status.", CommandStatus);
        helper.ConsoleCommands.Add("sm_add", "Allocate a point. Usage: sm_add basic A | sm_add ohgi | sm_add ultimate", CommandAdd);
        helper.ConsoleCommands.Add("sm_choose_ohgi", "Choose Ohgi branch A/B/C.", CommandChooseOhgi);
        helper.ConsoleCommands.Add("sm_choose_ultimate", "Choose Ultimate branch A/B.", CommandChooseUltimate);
        helper.ConsoleCommands.Add("sm_grant_ohgi", "DEBUG: mark Ohgi unlock quest completed.", CommandGrantOhgi);
        helper.ConsoleCommands.Add("sm_grant_ultimate", "DEBUG: mark Ultimate unlock quest completed.", CommandGrantUltimate);
        helper.ConsoleCommands.Add("sm_reset", "Pay the configured gold cost and schedule respec for next morning.", CommandReset);

        Monitor.Log("Sword Mastery prototype loaded.", LogLevel.Info);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(UnlockItemTextureAsset))
        {
            e.LoadFromModFile<Texture2D>(
                "assets/unlock_items.png",
                AssetLoadPriority.Exclusive
            );
            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;

                objects[OhgiSecretBookId] = new ObjectData
                {
                    Name = OhgiSecretBookId,
                    DisplayName = I18n.Get("item.ohgi-book.name"),
                    Description = I18n.Get("item.ohgi-book.description"),
                    Type = "Crafting",
                    Category = 0,
                    Price = 0,
                    Texture = UnlockItemTextureAsset,
                    SpriteIndex = 0,
                    Edibility = -300,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromFishingCollection = true,
                    ExcludeFromShippingCollection = true,
                    ExcludeFromRandomSale = true
                };

                objects[InsightDropId] = new ObjectData
                {
                    Name = InsightDropId,
                    DisplayName = I18n.Get("item.insight-drop.name"),
                    Description = I18n.Get("item.insight-drop.description"),
                    Type = "Crafting",
                    Category = 0,
                    Price = 0,
                    Texture = UnlockItemTextureAsset,
                    SpriteIndex = 1,
                    Edibility = -300,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromFishingCollection = true,
                    ExcludeFromShippingCollection = true,
                    ExcludeFromRandomSale = true
                };

                // New quest material. Reuse a vanilla purple gem sprite so this
                // remains a new custom item without adding another texture dependency.
                objects[DragonOrbId] = new ObjectData
                {
                    Name = DragonOrbId,
                    DisplayName = I18n.Get("item.dragon-orb.name"),
                    Description = I18n.Get("item.dragon-orb.description"),
                    Type = "Crafting",
                    Category = 0,
                    Price = 0,
                    Texture = UnlockItemTextureAsset,
                    SpriteIndex = 2,
                    Edibility = -300,
                    CanBeGivenAsGift = false,
                    CanBeTrashed = true,
                    ExcludeFromFishingCollection = true,
                    ExcludeFromShippingCollection = true,
                    ExcludeFromRandomSale = true
                };
            });

            return;
        }

        if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
        {
            e.Edit(asset =>
            {
                var quests = asset.AsDictionary<string, string>().Data;

                // Basic quests are completed manually by this mod so we can track
                // multiple custom objectives while still displaying them in the vanilla journal.
                quests[OhgiQuestId] =
                    "Basic/" + I18n.Get("quest.ohgi.title")
                    + "/" + I18n.Get("quest.ohgi.description")
                    + "/" + I18n.Get("quest.ohgi.objective.initial")
                    + "/null/-1/1/" + I18n.Get("quest.ohgi.reward") + "/false";

                quests[UltimateQuestId] =
                    "Basic/" + I18n.Get("quest.ultimate.title")
                    + "/" + I18n.Get("quest.ultimate.description")
                    + "/" + I18n.Get("quest.ultimate.objective.initial")
                    + "/null/-1/1/" + I18n.Get("quest.ultimate.reward") + "/false";
            });
        }
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
            text: () => I18n.Get("gmcm.section.main")
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.OpenMenuKey,
            setValue: value => Config.OpenMenuKey = value,
            name: () => I18n.Get("gmcm.open-menu.name"),
            tooltip: () => I18n.Get("gmcm.open-menu.tip"),
            fieldId: "OpenMenuKey"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.section.basic-keys")
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.BasicSkillAKey,
            setValue: value => Config.BasicSkillAKey = value,
            name: () => I18n.Get("skill.basic-a.name"),
            tooltip: () => I18n.Get("gmcm.basic-a.tip"),
            fieldId: "BasicSkillAKey"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.BasicSkillBKey,
            setValue: value => Config.BasicSkillBKey = value,
            name: () => I18n.Get("skill.basic-b.name"),
            tooltip: () => I18n.Get("gmcm.basic-b.tip"),
            fieldId: "BasicSkillBKey"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.BasicSkillCKey,
            setValue: value => Config.BasicSkillCKey = value,
            name: () => I18n.Get("skill.basic-c.name"),
            tooltip: () => I18n.Get("gmcm.basic-c.tip"),
            fieldId: "BasicSkillCKey"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.section.special-keys")
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.OhgiSkillKey,
            setValue: value => Config.OhgiSkillKey = value,
            name: () => I18n.Get("gmcm.ohgi-key.name"),
            tooltip: () => I18n.Get("gmcm.ohgi-key.tip"),
            fieldId: "OhgiSkillKey"
        );

        gmcm.AddKeybind(
            mod: ModManifest,
            getValue: () => Config.UltimateSkillKey,
            setValue: value => Config.UltimateSkillKey = value,
            name: () => I18n.Get("gmcm.ultimate-key.name"),
            tooltip: () => I18n.Get("gmcm.ultimate-key.tip"),
            fieldId: "UltimateSkillKey"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.section.hud")
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => Config.ShowHud,
            setValue: value => Config.ShowHud = value,
            name: () => I18n.Get("gmcm.hud.show.name"),
            tooltip: () => I18n.Get("gmcm.hud.show.tip"),
            fieldId: "ShowHud"
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Config.HudX,
            setValue: value => Config.HudX = value,
            name: () => I18n.Get("gmcm.hud.x.name"),
            tooltip: () => I18n.Get("gmcm.hud.x.tip"),
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
            name: () => I18n.Get("gmcm.hud.y.name"),
            tooltip: () => I18n.Get("gmcm.hud.y.tip"),
            min: 0,
            max: 2400,
            interval: 8,
            formatValue: value => $"{value}px",
            fieldId: "HudY"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.section.cooldown")
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => Config.ShowCooldownHud,
            setValue: value => Config.ShowCooldownHud = value,
            name: () => I18n.Get("gmcm.cooldown.show.name"),
            tooltip: () => I18n.Get("gmcm.cooldown.show.tip"),
            fieldId: "ShowCooldownHud"
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Config.CooldownHudX,
            setValue: value => Config.CooldownHudX = value,
            name: () => I18n.Get("gmcm.cooldown.x.name"),
            tooltip: () => I18n.Get("gmcm.cooldown.x.tip"),
            min: 0,
            max: 4000,
            interval: 8,
            formatValue: value => $"{value}px",
            fieldId: "CooldownHudX"
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Config.CooldownHudY,
            setValue: value => Config.CooldownHudY = value,
            name: () => I18n.Get("gmcm.cooldown.y.name"),
            tooltip: () => I18n.Get("gmcm.cooldown.y.tip"),
            min: 0,
            max: 2400,
            interval: 8,
            formatValue: value => $"{value}px",
            fieldId: "CooldownHudY"
        );

        gmcm.AddPageLink(
            mod: ModManifest,
            pageId: "debug",
            text: () => I18n.Get("gmcm.debug.link"),
            tooltip: () => I18n.Get("gmcm.debug.link.tip")
        );

        gmcm.AddPage(
            mod: ModManifest,
            pageId: "debug",
            pageTitle: () => I18n.Get("gmcm.debug.title")
        );

        gmcm.AddParagraph(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.debug.warning")
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            getValue: () => Context.IsWorldReady ? Data.SwordLevel : 0,
            setValue: value => ApplyDebugSwordLevel(value),
            name: () => I18n.Get("gmcm.debug.level.name"),
            tooltip: () => I18n.Get("gmcm.debug.level.tip"),
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
            name: () => I18n.Get("gmcm.debug.sp.name"),
            tooltip: () => I18n.Get("gmcm.debug.sp.tip"),
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
            name: () => I18n.Get("gmcm.debug.ohgi.name"),
            tooltip: () => I18n.Get("gmcm.debug.ohgi.tip"),
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
            name: () => I18n.Get("gmcm.debug.ultimate.name"),
            tooltip: () => I18n.Get("gmcm.debug.ultimate.tip"),
            fieldId: "DebugUltimateAccess"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.debug.section.actions")
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugActivateAllSkills();
            },
            name: () => I18n.Get("gmcm.debug.all-skills.name"),
            tooltip: () => I18n.Get("gmcm.debug.all-skills.tip"),
            fieldId: "DebugActivateAllSkills"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugGiveUnlockItems();
            },
            name: () => I18n.Get("gmcm.debug.unlock-items.name"),
            tooltip: () => I18n.Get("gmcm.debug.unlock-items.tip"),
            fieldId: "DebugGiveUnlockItems"
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("gmcm.debug.section.quests")
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugStartOhgiQuest();
            },
            name: () => I18n.Get("gmcm.debug.ohgi-start.name"),
            tooltip: () => I18n.Get("gmcm.debug.ohgi-start.tip"),
            fieldId: "DebugStartOhgiQuest"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugCompleteOhgiQuest();
            },
            name: () => I18n.Get("gmcm.debug.ohgi-complete.name"),
            tooltip: () => I18n.Get("gmcm.debug.ohgi-complete.tip"),
            fieldId: "DebugCompleteOhgiQuest"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugStartUltimateQuest();
            },
            name: () => I18n.Get("gmcm.debug.ultimate-start.name"),
            tooltip: () => I18n.Get("gmcm.debug.ultimate-start.tip"),
            fieldId: "DebugStartUltimateQuest"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugSetUltimateKills();
            },
            name: () => I18n.Get("gmcm.debug.dragon-kills.name"),
            tooltip: () => I18n.Get("gmcm.debug.dragon-kills.tip"),
            fieldId: "DebugSetUltimateKills"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugGiveDragonOrbs(10);
            },
            name: () => I18n.Get("gmcm.debug.orbs.name"),
            tooltip: () => I18n.Get("gmcm.debug.orbs.tip"),
            fieldId: "DebugGiveDragonOrbs"
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            getValue: () => false,
            setValue: value =>
            {
                if (value)
                    DebugCompleteUltimateQuest();
            },
            name: () => I18n.Get("gmcm.debug.ultimate-complete.name"),
            tooltip: () => I18n.Get("gmcm.debug.ultimate-complete.tip"),
            fieldId: "DebugCompleteUltimateQuest"
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

    private void DebugActivateAllSkills()
    {
        if (!Context.IsWorldReady)
            return;

        Data.EnsureSkillKeys();

        foreach (SkillProgress progress in Data.Skills.Values)
        {
            progress.Stage1 = 5;
            progress.Stage2 = 5;
            progress.Stage3 = 5;
        }

        Data.OhgiAccessGranted = true;
        Data.UltimateAccessGranted = true;
        Data.OhgiQuestAvailable = false;
        Data.UltimateQuestAvailable = false;

        // Keep branch exclusivity intact, but let the player choose any branch from the skill menu.
        Data.SelectedOhgi = null;
        Data.SelectedUltimate = null;

        Data.UnspentSkillPoints = Math.Max(Data.UnspentSkillPoints, 200);

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("debug.all-skills"),
            HUDMessage.newQuest_type
        ));
        Game1.playSound("achievement");
    }

    private void DebugGiveUnlockItems()
    {
        if (!Context.IsWorldReady)
            return;

        bool book = Game1.player.addItemToInventoryBool(
            ItemRegistry.Create($"(O){OhgiSecretBookId}")
        );

        bool drop = Game1.player.addItemToInventoryBool(
            ItemRegistry.Create($"(O){InsightDropId}")
        );

        if (book && drop)
        {
            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("debug.unlock-items.ok"),
                HUDMessage.newQuest_type
            ));
            Game1.playSound("getNewSpecialItem");
        }
        else
        {
            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("debug.inventory-full")
            ));
            Game1.playSound("cancel");
        }
    }

    // ---------------------------------------------------------------------
    // Unlock quests
    // ---------------------------------------------------------------------

    private void DebugStartOhgiQuest()
    {
        if (!Context.IsWorldReady)
            return;

        Data.SwordLevel = Math.Max(Data.SwordLevel, 30);
        Data.OhgiAccessGranted = false;
        Data.SelectedOhgi = null;
        Data.OhgiQuestStarted = false;
        Data.OhgiQuestCompleted = false;
        Data.OhgiSkullKills = 0;
        Data.OhgiQuestAvailable = true;

        RemoveQuestFromJournal(OhgiQuestId);
        StartOhgiUnlockQuest();

        bool inserted = FindQuestInJournal(OhgiQuestId) is not null;

        Game1.addHUDMessage(new HUDMessage(
            inserted
                ? I18n.Get("debug.ohgi-start.ok")
                : I18n.Get("debug.ohgi-start.fail"),
            inserted ? HUDMessage.newQuest_type : HUDMessage.error_type
        ));
    }

    private void DebugCompleteOhgiQuest()
    {
        if (!Context.IsWorldReady)
            return;

        if (!Data.OhgiQuestStarted || Data.OhgiQuestCompleted)
            DebugStartOhgiQuest();

        Data.OhgiSkullKills = 30;
        UpdateOhgiQuestObjective();
        CompleteOhgiUnlockQuest();
    }

    private void DebugStartUltimateQuest()
    {
        if (!Context.IsWorldReady)
            return;

        Data.SwordLevel = Math.Max(Data.SwordLevel, 50);
        Data.UltimateAccessGranted = false;
        Data.SelectedUltimate = null;
        Data.UltimateQuestStarted = false;
        Data.UltimateQuestCompleted = false;
        Data.UltimateDragonKills = 0;
        Data.UltimateQuestAvailable = true;

        RemoveQuestFromJournal(UltimateQuestId);
        StartUltimateUnlockQuest();

        bool inserted = FindQuestInJournal(UltimateQuestId) is not null;

        Game1.addHUDMessage(new HUDMessage(
            inserted
                ? I18n.Get("debug.ultimate-start.ok")
                : I18n.Get("debug.ultimate-start.fail"),
            inserted ? HUDMessage.newQuest_type : HUDMessage.error_type
        ));
    }

    private void DebugSetUltimateKills()
    {
        if (!Context.IsWorldReady)
            return;

        if (!Data.UltimateQuestStarted || Data.UltimateQuestCompleted)
            DebugStartUltimateQuest();

        Data.UltimateDragonKills = 50;
        UpdateUltimateQuestObjective();

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("debug.dragon-kills")
        ));

        CheckUltimateQuestCompletion();
    }

    private void DebugGiveDragonOrbs(int count)
    {
        if (!Context.IsWorldReady || count <= 0)
            return;

        GiveItemOrDrop($"(O){DragonOrbId}", count);

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("debug.orbs", new { count }),
            HUDMessage.newQuest_type
        ));

        UpdateUltimateQuestObjective();
        CheckUltimateQuestCompletion();
    }

    private void DebugCompleteUltimateQuest()
    {
        if (!Context.IsWorldReady)
            return;

        if (!Data.UltimateQuestStarted || Data.UltimateQuestCompleted)
            DebugStartUltimateQuest();

        Data.UltimateDragonKills = 50;

        int have = CountInventoryItem($"(O){DragonOrbId}");
        if (have < 10)
            GiveItemOrDrop($"(O){DragonOrbId}", 10 - have);

        UpdateUltimateQuestObjective();

        // DEBUG completion is allowed even if the inventory was full and some
        // test orbs had to drop on the ground.
        CompleteUltimateUnlockQuest(force: true);
    }

    private void EnsureUnlockQuestJournals()
    {
        if (!Context.IsWorldReady)
            return;

        if (Data.OhgiRewardReady)
        {
            EnsureQuestInJournal(OhgiQuestId);
            MarkQuestReadyForJournalReward(
                OhgiQuestId,
                I18n.Get("item.ohgi-book.name")
            );
        }
        else if (Data.OhgiQuestAvailable && !Data.OhgiQuestCompleted)
        {
            if (!Data.OhgiQuestStarted)
                StartOhgiUnlockQuest();
            else
            {
                EnsureQuestInJournal(OhgiQuestId);
                UpdateOhgiQuestObjective();
            }
        }

        if (Data.UltimateRewardReady)
        {
            EnsureQuestInJournal(UltimateQuestId);
            MarkQuestReadyForJournalReward(
                UltimateQuestId,
                I18n.Get("item.insight-drop.name")
            );
        }
        else if (Data.UltimateQuestAvailable && !Data.UltimateQuestCompleted)
        {
            if (!Data.UltimateQuestStarted)
                StartUltimateUnlockQuest();
            else
            {
                EnsureQuestInJournal(UltimateQuestId);
                UpdateUltimateQuestObjective();
            }
        }
    }

    private void StartOhgiUnlockQuest()
    {
        if (Data.OhgiQuestCompleted)
            return;

        Data.OhgiQuestStarted = true;
        Data.OhgiQuestAvailable = true;
        Data.OhgiSkullKills = Math.Clamp(Data.OhgiSkullKills, 0, 30);
        EnsureQuestInJournal(OhgiQuestId);
        UpdateOhgiQuestObjective();
    }

    private void StartUltimateUnlockQuest()
    {
        if (Data.UltimateQuestCompleted)
            return;

        Data.UltimateQuestStarted = true;
        Data.UltimateQuestAvailable = true;
        Data.UltimateDragonKills = Math.Clamp(Data.UltimateDragonKills, 0, 50);
        EnsureQuestInJournal(UltimateQuestId);
        UpdateUltimateQuestObjective();
    }

    private void UpdateUnlockQuestProgress()
    {
        // Kill progress is updated immediately through World.NpcListChanged so it
        // works for Haunted Skulls and both Serpent variants without relying on
        // the game's monster-slayer stat buckets. This periodic pass keeps the
        // journal's item objective synchronized when Dragon Orbs are picked up,
        // moved, or consumed.
        if (Data.OhgiQuestStarted && !Data.OhgiQuestCompleted)
            UpdateOhgiQuestObjective();

        if (Data.UltimateQuestStarted && !Data.UltimateQuestCompleted)
        {
            UpdateUltimateQuestObjective();
            CheckUltimateQuestCompletion();
        }
    }

    private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        foreach (NPC npc in e.Removed)
        {
            if (npc is not Monster monster)
                continue;

            string monsterName = monster.Name ?? string.Empty;

            if (Data.OhgiQuestStarted
                && !Data.OhgiQuestCompleted
                && IsHauntedSkull(monster, e.Location))
            {
                Data.OhgiSkullKills = Math.Min(
                    30,
                    Data.OhgiSkullKills + 1
                );

                UpdateOhgiQuestObjective();

                Game1.addHUDMessage(new HUDMessage(
                    I18n.Get("quest.ohgi.kill-progress", new { count = Data.OhgiSkullKills }),
                    HUDMessage.newQuest_type
                ));

                if (Data.OhgiSkullKills >= 30)
                    CompleteOhgiUnlockQuest();
            }

            if (Data.UltimateQuestStarted
                && !Data.UltimateQuestCompleted
                && (string.Equals(monsterName, "Serpent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(monsterName, "Royal Serpent", StringComparison.OrdinalIgnoreCase)))
            {
                Data.UltimateDragonKills = Math.Min(50, Data.UltimateDragonKills + 1);

                TryDropDragonOrb(
                    e.Location,
                    monster.Position + new Vector2(32f, 32f)
                );

                UpdateUltimateQuestObjective();
                CheckUltimateQuestCompletion();
            }
        }
    }

    private static bool IsHauntedSkull(
        Monster monster,
        GameLocation location
    )
    {
        // Quarry Mine Haunted Skulls are created by the game as:
        // new Bat(position, 77377)
        //
        // Therefore the most reliable identification is:
        // Bat + quarry mine level 77377.
        if (monster is Bat && IsQuarryMineLocation(location))
            return true;

        // Some builds/modded locations may expose a dedicated display/internal name.
        string name = monster.Name ?? string.Empty;

        if (string.Equals(
            name,
            "Haunted Skull",
            StringComparison.OrdinalIgnoreCase
        ))
        {
            return true;
        }

        if (monster is not Bat)
            return false;

        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        Type type = monster.GetType();

        // Secondary fallback: inspect known haunted-skull boolean flags.
        string[] boolMemberNames =
        {
            "hauntedSkull",
            "HauntedSkull",
            "isHauntedSkull",
            "IsHauntedSkull"
        };

        foreach (string memberName in boolMemberNames)
        {
            object? raw =
                type.GetField(memberName, flags)?.GetValue(monster)
                ?? type.GetProperty(memberName, flags)?.GetValue(monster);

            if (TryReadBoolLikeValue(raw, out bool value) && value)
                return true;
        }

        // Last fallback: inspect the sprite texture name.
        object? sprite = monster.Sprite;

        if (sprite is not null)
        {
            Type spriteType = sprite.GetType();

            string[] textureMemberNames =
            {
                "TextureName",
                "textureName",
                "textureNameValue"
            };

            foreach (string memberName in textureMemberNames)
            {
                object? raw =
                    spriteType.GetField(memberName, flags)?.GetValue(sprite)
                    ?? spriteType.GetProperty(memberName, flags)?.GetValue(sprite);

                string? textureName = ReadStringLikeValue(raw);

                if (!string.IsNullOrWhiteSpace(textureName)
                    && (
                        textureName.Contains(
                            "Haunted Skull",
                            StringComparison.OrdinalIgnoreCase
                        )
                        || textureName.Contains(
                            "HauntedSkull",
                            StringComparison.OrdinalIgnoreCase
                        )
                    ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsQuarryMineLocation(GameLocation location)
    {
        const int QuarryMineLevel = 77377;

        // Fast name fallback for mine locations whose unique name embeds the floor.
        string locationName =
            location.NameOrUniqueName
            ?? location.Name
            ?? string.Empty;

        if (locationName.Contains(
            QuarryMineLevel.ToString(),
            StringComparison.OrdinalIgnoreCase
        ))
        {
            return true;
        }

        // MineShaft exposes mineLevel in game builds, but use reflection so this
        // remains tolerant of member-shape changes.
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        Type locationType = location.GetType();

        string[] memberNames =
        {
            "mineLevel",
            "MineLevel",
            "netMineLevel"
        };

        foreach (string memberName in memberNames)
        {
            object? raw =
                locationType.GetField(memberName, flags)?.GetValue(location)
                ?? locationType.GetProperty(memberName, flags)?.GetValue(location);

            if (TryReadIntLikeValue(raw, out int level)
                && level == QuarryMineLevel)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadIntLikeValue(
        object? raw,
        out int value
    )
    {
        value = 0;

        if (raw is null)
            return false;

        if (raw is int direct)
        {
            value = direct;
            return true;
        }

        PropertyInfo? valueProperty = raw
            .GetType()
            .GetProperty(
                "Value",
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
            );

        object? nested = valueProperty?.GetValue(raw);

        if (nested is int nestedInt)
        {
            value = nestedInt;
            return true;
        }

        return int.TryParse(raw.ToString(), out value);
    }

    private static bool TryReadBoolLikeValue(object? raw, out bool value)
    {
        if (raw is bool direct)
        {
            value = direct;
            return true;
        }

        if (raw is not null)
        {
            PropertyInfo? valueProperty = raw.GetType().GetProperty(
                "Value",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            object? nested = valueProperty?.GetValue(raw);
            if (nested is bool nestedBool)
            {
                value = nestedBool;
                return true;
            }

            if (bool.TryParse(nested?.ToString() ?? raw.ToString(), out bool parsed))
            {
                value = parsed;
                return true;
            }
        }

        value = false;
        return false;
    }

    private void CheckUltimateQuestCompletion()
    {
        if (!Data.UltimateQuestStarted
            || Data.UltimateQuestCompleted
            || Data.UltimateRewardReady)
        {
            return;
        }

        if (Data.UltimateDragonKills < 50)
            return;

        if (CountInventoryItem($"(O){DragonOrbId}") < 10)
            return;

        CompleteUltimateUnlockQuest(force: false);
    }

    private void CompleteOhgiUnlockQuest()
    {
        if (Data.OhgiQuestCompleted || Data.OhgiRewardReady)
            return;

        Data.OhgiSkullKills = 30;
        Data.OhgiRewardReady = true;

        MarkQuestReadyForJournalReward(
            OhgiQuestId,
            I18n.Get("item.ohgi-book.name")
        );

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("quest.ohgi.complete"),
            HUDMessage.newQuest_type
        ));
        Game1.playSound("questcomplete");
    }

    private void CompleteUltimateUnlockQuest(bool force)
    {
        if (Data.UltimateQuestCompleted || Data.UltimateRewardReady)
            return;

        if (!force)
        {
            if (Data.UltimateDragonKills < 50)
                return;

            if (CountInventoryItem($"(O){DragonOrbId}") < 10)
                return;
        }

        // Consume the 10 Dragon Orbs at objective completion.
        // The unlock item itself is claimed later through the journal reward box.
        RemoveInventoryItem($"(O){DragonOrbId}", 10);

        Data.UltimateDragonKills = 50;
        Data.UltimateRewardReady = true;

        PlayDragonOrbAbsorptionEffect();

        MarkQuestReadyForJournalReward(
            UltimateQuestId,
            I18n.Get("item.insight-drop.name")
        );

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("quest.ultimate.complete"),
            HUDMessage.newQuest_type
        ));
    }

    private void MarkQuestReadyForJournalReward(
        string questId,
        string rewardName
    )
    {
        EnsureQuestInJournal(questId);

        object? quest = FindQuestInJournal(questId);
        if (quest is null)
            return;

        // A 1g token makes the vanilla reward chest clickable.
        // We immediately subtract that 1g when the claim is detected; the actual
        // reward is the custom unlock item named in rewardDescription.
        TrySetIntLikeMember(quest, "moneyReward", 1);
        TrySetStringLikeMember(quest, "rewardDescription", rewardName);

        MethodInfo? questComplete =
            quest.GetType().GetMethod(
                "questComplete",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            )
            ?? quest.GetType().GetMethod(
                "QuestComplete",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            );

        if (questComplete is not null)
        {
            try
            {
                questComplete.Invoke(quest, null);
            }
            catch (Exception ex)
            {
                Monitor.Log(
                    $"Could not mark quest '{questId}' complete for journal reward: {ex.Message}",
                    LogLevel.Error
                );

                TrySetBoolLikeMember(quest, "completed", true);
            }
        }
        else
        {
            TrySetBoolLikeMember(quest, "completed", true);
        }

        Game1.dayTimeMoneyBox.questsDirty = true;
    }

    private void HandleJournalRewardClaims()
    {
        if (Data.OhgiRewardReady
            && WasJournalRewardClaimed(OhgiQuestId))
        {
            RemoveJournalRewardTokenGold();

            Data.OhgiRewardReady = false;
            Data.OhgiQuestCompleted = true;
            Data.OhgiQuestAvailable = false;
            Data.OhgiRewardDeliveryPending = true;

            if (TryDeliverPendingQuestReward(
                    $"(O){OhgiSecretBookId}",
                    I18n.Get("item.ohgi-book.name")
                ))
            {
                Data.OhgiRewardDeliveryPending = false;
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage(
                    I18n.Get("reward.pending.ohgi"),
                    HUDMessage.error_type
                ));
            }
        }

        if (Data.UltimateRewardReady
            && WasJournalRewardClaimed(UltimateQuestId))
        {
            RemoveJournalRewardTokenGold();

            Data.UltimateRewardReady = false;
            Data.UltimateQuestCompleted = true;
            Data.UltimateQuestAvailable = false;
            Data.UltimateRewardDeliveryPending = true;

            if (TryDeliverPendingQuestReward(
                    $"(O){InsightDropId}",
                    I18n.Get("item.insight-drop.name")
                ))
            {
                Data.UltimateRewardDeliveryPending = false;
            }
            else
            {
                Game1.addHUDMessage(new HUDMessage(
                    I18n.Get("reward.pending.ultimate"),
                    HUDMessage.error_type
                ));
            }
        }

        if (Data.OhgiRewardDeliveryPending
            && TryDeliverPendingQuestReward(
                $"(O){OhgiSecretBookId}",
                I18n.Get("item.ohgi-book.name")
            ))
        {
            Data.OhgiRewardDeliveryPending = false;
        }

        if (Data.UltimateRewardDeliveryPending
            && TryDeliverPendingQuestReward(
                $"(O){InsightDropId}",
                I18n.Get("item.insight-drop.name")
            ))
        {
            Data.UltimateRewardDeliveryPending = false;
        }
    }

    private bool WasJournalRewardClaimed(string questId)
    {
        object? quest = FindQuestInJournal(questId);

        // Vanilla removes a completed quest after the reward is claimed and the
        // player leaves its journal page.
        if (quest is null)
            return true;

        if (TryReadIntLikeMember(
                quest,
                "moneyReward",
                out int reward
            )
            && reward <= 0)
        {
            return true;
        }

        if (TryReadBoolLikeMember(
                quest,
                "destroy",
                out bool destroy
            )
            && destroy)
        {
            return true;
        }

        return false;
    }

    private static void RemoveJournalRewardTokenGold()
    {
        if (Game1.player.Money > 0)
            Game1.player.Money -= 1;
    }

    private static bool TryDeliverPendingQuestReward(
        string qualifiedItemId,
        string displayName
    )
    {
        Item item = ItemRegistry.Create(qualifiedItemId);

        if (!Game1.player.addItemToInventoryBool(item))
            return false;

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("reward.received", new { name = displayName }),
            HUDMessage.newQuest_type
        ));
        Game1.playSound("coin");

        return true;
    }

    private void TryDropDragonOrb(GameLocation location, Vector2 position)
    {
        // Base 12%. Positive daily luck contributes up to 2%, and Luck Level
        // contributes up to 1%. The final chance is hard-capped at 15%.
        double dailyLuckBonus =
            Math.Clamp(Math.Max(0d, Game1.player.DailyLuck) * 0.20d, 0d, 0.02d);

        double luckLevelBonus =
            Math.Clamp(Math.Max(0, Game1.player.LuckLevel) * 0.001d, 0d, 0.01d);

        double chance =
            Math.Min(0.15d, 0.12d + dailyLuckBonus + luckLevelBonus);

        if (Game1.random.NextDouble() >= chance)
            return;

        Item orb = ItemRegistry.Create($"(O){DragonOrbId}");

        Game1.createItemDebris(
            orb,
            position,
            -1,
            location
        );

        Game1.addHUDMessage(new HUDMessage(
            I18n.Get("drop.dragon-orb"),
            HUDMessage.newQuest_type
        ));
        Game1.playSound("discoverMineral");
    }

    private void UpdateOhgiQuestObjective()
    {
        if (!Data.OhgiQuestStarted
            || Data.OhgiQuestCompleted
            || Data.OhgiRewardReady)
        {
            return;
        }

        string objective =
            I18n.Get("quest.ohgi.objective.progress", new { count = Math.Min(30, Data.OhgiSkullKills) });

        SetQuestObjective(
            OhgiQuestId,
            objective
        );

        SetQuestProgressDescription(
            OhgiQuestId,
            I18n.Get("quest.ohgi.description"),
            objective
        );
    }

    private void UpdateUltimateQuestObjective()
    {
        if (!Data.UltimateQuestStarted
            || Data.UltimateQuestCompleted
            || Data.UltimateRewardReady)
        {
            return;
        }

        int orbs = Math.Min(
            10,
            CountInventoryItem($"(O){DragonOrbId}")
        );

        string objective =
            I18n.Get("quest.ultimate.objective.progress", new { kills = Math.Min(50, Data.UltimateDragonKills), orbs });

        SetQuestObjective(
            UltimateQuestId,
            objective
        );

        SetQuestProgressDescription(
            UltimateQuestId,
            I18n.Get("quest.ultimate.description"),
            objective
        );
    }

    private void EnsureQuestInJournal(string questId)
    {
        if (FindQuestInJournal(questId) is not null)
            return;

        try
        {
            // Stardew 1.6 supports string quest IDs directly.
            // Refresh the data asset first so our custom entry is definitely present.
            Helper.GameContent.InvalidateCache("Data/Quests");

            Dictionary<string, string> questData =
                Game1.content.Load<Dictionary<string, string>>("Data\\Quests");

            if (!questData.ContainsKey(questId))
            {
                Monitor.Log(
                    $"Custom quest data '{questId}' was not found in Data/Quests after cache refresh.",
                    LogLevel.Error
                );
                return;
            }

            Game1.player.addQuest(questId);
        }
        catch (Exception ex)
        {
            Monitor.Log(
                $"Direct addQuest failed for '{questId}': {ex}",
                LogLevel.Error
            );
        }

        if (FindQuestInJournal(questId) is not null)
        {
            Monitor.Log(
                $"Added custom quest '{questId}' to the journal.",
                LogLevel.Debug
            );
            return;
        }

        // Fallback for unusual game builds: create the Quest object through the
        // game's own Quest.getQuestFromId(string) and add it to questLog.
        try
        {
            Type? questType = typeof(Game1).Assembly.GetType(
                "StardewValley.Quests.Quest"
            );

            MethodInfo? getQuest = questType?.GetMethod(
                "getQuestFromId",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null
            );

            object? quest = getQuest?.Invoke(
                null,
                new object[] { questId }
            );

            if (quest is not null)
            {
                MethodInfo? addMethod = Game1.player.questLog
                    .GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(method =>
                    {
                        if (!string.Equals(method.Name, "Add", StringComparison.Ordinal))
                            return false;

                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 1
                            && parameters[0].ParameterType.IsAssignableFrom(quest.GetType());
                    });

                addMethod?.Invoke(
                    Game1.player.questLog,
                    new[] { quest }
                );
            }
        }
        catch (Exception ex)
        {
            Monitor.Log(
                $"Fallback quest creation failed for '{questId}': {ex}",
                LogLevel.Error
            );
        }

        if (FindQuestInJournal(questId) is null)
        {
            Monitor.Log(
                $"Quest '{questId}' still could not be inserted into the journal.",
                LogLevel.Error
            );
        }
        else
        {
            Monitor.Log(
                $"Added custom quest '{questId}' to the journal through fallback creation.",
                LogLevel.Debug
            );
        }
    }

    private void CompleteQuestInJournal(string questId)
    {
        MethodInfo? completeQuest = Game1.player.GetType().GetMethod(
            "completeQuest",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null
        );

        if (completeQuest is not null)
        {
            try
            {
                completeQuest.Invoke(Game1.player, new object[] { questId });
                return;
            }
            catch (Exception ex)
            {
                Monitor.Log(
                    $"Could not complete quest '{questId}' through Farmer.completeQuest: {ex.Message}",
                    LogLevel.Trace
                );
            }
        }

        // Fallback for game builds where completion is handled differently:
        // remove it from the journal; SaveData remains the authoritative completion flag.
        RemoveQuestFromJournal(questId);
    }

    private void RemoveQuestFromJournal(string questId)
    {
        MethodInfo? removeQuest = Game1.player.GetType().GetMethod(
            "removeQuest",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null
        );

        if (removeQuest is null)
            return;

        try
        {
            removeQuest.Invoke(Game1.player, new object[] { questId });
        }
        catch
        {
            // DEBUG/reset helper only; safe to ignore if the quest wasn't present.
        }
    }

    private object? FindQuestInJournal(string questId)
    {
        foreach (object quest in Game1.player.questLog)
        {
            if (string.Equals(
                GetQuestIdByReflection(quest),
                questId,
                StringComparison.Ordinal
            ))
            {
                return quest;
            }
        }

        return null;
    }

    private static string? GetQuestIdByReflection(object quest)
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        FieldInfo? field = quest.GetType().GetField("id", flags);
        if (field is not null)
            return ReadStringLikeValue(field.GetValue(quest));

        PropertyInfo? property = quest.GetType().GetProperty("id", flags);
        if (property is not null)
            return ReadStringLikeValue(property.GetValue(quest));

        PropertyInfo? idProperty = quest.GetType().GetProperty("Id", flags);
        return idProperty is null
            ? null
            : ReadStringLikeValue(idProperty.GetValue(quest));
    }

    private void SetQuestObjective(string questId, string objective)
    {
        object? quest = FindQuestInJournal(questId);
        if (quest is null)
            return;

        if (!TrySetStringLikeMember(
                quest,
                "currentObjective",
                objective
            )
            && !TrySetStringLikeMember(
                quest,
                "_currentObjective",
                objective
            ))
        {
            Monitor.Log(
                $"Could not update journal objective for '{questId}'.",
                LogLevel.Trace
            );
        }
    }

    private void SetQuestProgressDescription(
        string questId,
        string baseDescription,
        string objective
    )
    {
        object? quest = FindQuestInJournal(questId);
        if (quest is null)
            return;

        string progressDescription =
            $"{baseDescription}\n\n{objective}";

        // Different 1.6 builds expose the description under slightly
        // different member names; update whichever exists.
        TrySetStringLikeMember(
            quest,
            "questDescription",
            progressDescription
        );

        TrySetStringLikeMember(
            quest,
            "_questDescription",
            progressDescription
        );

        TrySetStringLikeMember(
            quest,
            "description",
            progressDescription
        );
    }

    private static bool TryReadIntLikeMember(
        object target,
        string memberName,
        out int value
    )
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        object? raw =
            target.GetType().GetField(memberName, flags)?.GetValue(target)
            ?? target.GetType().GetProperty(memberName, flags)?.GetValue(target);

        return TryReadIntLikeValue(raw, out value);
    }

    private static bool TryReadBoolLikeMember(
        object target,
        string memberName,
        out bool value
    )
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        object? raw =
            target.GetType().GetField(memberName, flags)?.GetValue(target)
            ?? target.GetType().GetProperty(memberName, flags)?.GetValue(target);

        return TryReadBoolLikeValue(raw, out value);
    }

    private static bool TrySetIntLikeMember(
        object target,
        string memberName,
        int value
    )
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        FieldInfo? field = target.GetType().GetField(memberName, flags);
        if (field is not null)
        {
            if (field.FieldType == typeof(int))
            {
                field.SetValue(target, value);
                return true;
            }

            if (TrySetScalarNetValue(field.GetValue(target), value))
                return true;
        }

        PropertyInfo? property = target.GetType().GetProperty(memberName, flags);
        if (property is not null)
        {
            if (property.PropertyType == typeof(int) && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            if (TrySetScalarNetValue(property.GetValue(target), value))
                return true;
        }

        return false;
    }

    private static bool TrySetBoolLikeMember(
        object target,
        string memberName,
        bool value
    )
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        FieldInfo? field = target.GetType().GetField(memberName, flags);
        if (field is not null)
        {
            if (field.FieldType == typeof(bool))
            {
                field.SetValue(target, value);
                return true;
            }

            if (TrySetScalarNetValue(field.GetValue(target), value))
                return true;
        }

        PropertyInfo? property = target.GetType().GetProperty(memberName, flags);
        if (property is not null)
        {
            if (property.PropertyType == typeof(bool) && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            if (TrySetScalarNetValue(property.GetValue(target), value))
                return true;
        }

        return false;
    }

    private static bool TrySetScalarNetValue<T>(
        object? target,
        T value
    )
    {
        if (target is null)
            return false;

        PropertyInfo? valueProperty = target.GetType().GetProperty(
            "Value",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (valueProperty is null
            || !valueProperty.CanWrite
            || valueProperty.PropertyType != typeof(T))
        {
            return false;
        }

        valueProperty.SetValue(target, value);
        return true;
    }

    private static string? ReadStringLikeValue(object? value)
    {
        if (value is null)
            return null;

        if (value is string text)
            return text;

        PropertyInfo? property = value.GetType().GetProperty(
            "Value",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        return property?.GetValue(value)?.ToString();
    }

    private static bool TrySetStringLikeMember(
        object target,
        string memberName,
        string value
    )
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;

        FieldInfo? field = target.GetType().GetField(memberName, flags);

        if (field is not null)
        {
            if (field.FieldType == typeof(string))
            {
                field.SetValue(target, value);
                return true;
            }

            object? raw = field.GetValue(target);
            if (TrySetNetValue(raw, value))
                return true;
        }

        PropertyInfo? property =
            target.GetType().GetProperty(memberName, flags)
            ?? target.GetType().GetProperty(
                char.ToUpperInvariant(memberName[0]) + memberName[1..],
                flags
            );

        if (property is not null)
        {
            if (property.PropertyType == typeof(string) && property.CanWrite)
            {
                property.SetValue(target, value);
                return true;
            }

            object? raw = property.GetValue(target);
            if (TrySetNetValue(raw, value))
                return true;
        }

        return false;
    }

    private static bool TrySetNetValue(object? target, string value)
    {
        if (target is null)
            return false;

        PropertyInfo? valueProperty = target.GetType().GetProperty(
            "Value",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (valueProperty is null
            || !valueProperty.CanWrite
            || valueProperty.PropertyType != typeof(string))
        {
            return false;
        }

        valueProperty.SetValue(target, value);
        return true;
    }

    private static int CountInventoryItem(string qualifiedItemId)
    {
        int total = 0;

        foreach (Item? item in Game1.player.Items)
        {
            if (item?.QualifiedItemId == qualifiedItemId)
                total += item.Stack;
        }

        return total;
    }

    private static int RemoveInventoryItem(
        string qualifiedItemId,
        int amount
    )
    {
        int remaining = Math.Max(0, amount);

        for (int i = 0; i < Game1.player.Items.Count && remaining > 0; i++)
        {
            Item? item = Game1.player.Items[i];

            if (item?.QualifiedItemId != qualifiedItemId)
                continue;

            int take = Math.Min(item.Stack, remaining);
            item.Stack -= take;
            remaining -= take;

            if (item.Stack <= 0)
                Game1.player.Items[i] = null;
        }

        return amount - remaining;
    }

    private static void GiveItemOrDrop(
        string qualifiedItemId,
        int stack
    )
    {
        Item item = ItemRegistry.Create(
            qualifiedItemId,
            Math.Max(1, stack)
        );

        if (!Game1.player.addItemToInventoryBool(item))
        {
            Game1.createItemDebris(
                item,
                Game1.player.getStandingPosition(),
                -1,
                Game1.currentLocation
            );
        }
    }

    private void PlayDragonOrbAbsorptionEffect()
    {
        Vector2 center = Game1.player.getStandingPosition();
        Color glow = new(128, 76, 255);

        Game1.screenGlowOnce(glow, hold: false);
        Game1.playSound("stardrop");

        // Purple-blue particles converge from a ring into the player.
        for (int i = 0; i < 18; i++)
        {
            float angle = MathF.PI * 2f * i / 18f;
            float radius = 120f + (i % 4) * 20f;

            Vector2 start = center + new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle)
            ) * radius;

            Vector2 motion = (center - start) / 26f;

            TemporaryAnimatedSprite particle = new(
                10,
                start,
                i % 2 == 0
                    ? new Color(148, 88, 255)
                    : new Color(88, 178, 255)
            )
            {
                motion = motion,
                scale = 0.7f + (i % 3) * 0.12f,
                alphaFade = 0.018f,
                delayBeforeAnimationStart = i * 28,
                layerDepth = 1f
            };

            Game1.currentLocation.temporarySprites.Add(particle);
        }
    }

    private bool TryUseUnlockItem(SButton button)
    {
        if (!button.IsActionButton())
            return false;

        StardewValley.Object? held = Game1.player.ActiveObject;
        if (held is null)
            return false;

        if (held.QualifiedItemId == $"(O){OhgiSecretBookId}")
        {
            if (Data.OhgiAccessGranted)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Get("unlock.ohgi.already")));
                Game1.playSound("cancel");
                return true;
            }

            Skills.GrantOhgiAccess(Data);
            ConsumeActiveObject(held);

            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("unlock.ohgi.done"),
                HUDMessage.newQuest_type
            ));
            Game1.playSound("getNewSpecialItem");
            return true;
        }

        if (held.QualifiedItemId == $"(O){InsightDropId}")
        {
            if (Data.UltimateAccessGranted)
            {
                Game1.addHUDMessage(new HUDMessage(I18n.Get("unlock.ultimate.already")));
                Game1.playSound("cancel");
                return true;
            }

            Skills.GrantUltimateAccess(Data);
            ConsumeActiveObject(held);

            Game1.addHUDMessage(new HUDMessage(
                I18n.Get("unlock.ultimate.done"),
                HUDMessage.newQuest_type
            ));
            Game1.playSound("stardrop");
            return true;
        }

        return false;
    }

    private static void ConsumeActiveObject(StardewValley.Object held)
    {
        held.Stack--;

        if (held.Stack <= 0
            && Game1.player.CurrentToolIndex >= 0
            && Game1.player.CurrentToolIndex < Game1.player.Items.Count)
        {
            Game1.player.Items[Game1.player.CurrentToolIndex] = null;
        }
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

        // Quest unlock items are used directly from the active inventory slot.
        // Handle them before combat input so the action button isn't passed through.
        if (Game1.activeClickableMenu is null && TryUseUnlockItem(e.Button))
        {
            Helper.Input.Suppress(e.Button);
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

        // Force Data/Quests to reload through our AssetRequested edit before
        // attempting to create the custom quest objects.
        Helper.GameContent.InvalidateCache("Data/Quests");

        // Also restores/adds quest journal entries for existing saves already past
        // the level 30 / 50 thresholds.
        RefreshUnlockState(showMessages: false);
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
            I18n.Get("reset.completed"),
            HUDMessage.newQuest_type
        ));
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Combat.Update(Data);
        HandleJournalRewardClaims();

        if (!e.IsMultipleOf(15))
            return;

        // Quest progress is sampled at the same 15-tick cadence as mastery XP.
        UpdateUnlockQuestProgress();

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
                    I18n.Get("level.up", new { level = Data.SwordLevel }),
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
        EnsureUnlockQuestJournals();

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
            Game1.addHUDMessage(new HUDMessage(I18n.Get("notify.ohgi.quest"), HUDMessage.newQuest_type));
            Game1.playSound("questcomplete");
        }

        if (!prevUltimateQuest && Data.UltimateQuestAvailable)
        {
            Game1.addHUDMessage(new HUDMessage(I18n.Get("notify.ultimate.quest"), HUDMessage.newQuest_type));
            Game1.playSound("questcomplete");
        }

        if (!prevOhgiAccess && Data.OhgiAccessGranted)
            Game1.addHUDMessage(new HUDMessage(I18n.Get("unlock.ohgi.done"), HUDMessage.newQuest_type));

        if (!prevUltimateAccess && Data.UltimateAccessGranted)
            Game1.addHUDMessage(new HUDMessage(I18n.Get("unlock.ultimate.done"), HUDMessage.newQuest_type));
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        Combat.Draw(e.SpriteBatch);
    }

    private void LoadCooldownIcons()
    {
        CooldownIcons["BasicA"] = Helper.ModContent.Load<Texture2D>("assets/icons/basic_a.png");
        CooldownIcons["BasicB"] = Helper.ModContent.Load<Texture2D>("assets/icons/basic_b.png");
        CooldownIcons["BasicC"] = Helper.ModContent.Load<Texture2D>("assets/icons/basic_c.png");
        CooldownIcons["OhgiA"] = Helper.ModContent.Load<Texture2D>("assets/icons/ohgi_a.png");
        CooldownIcons["OhgiC"] = Helper.ModContent.Load<Texture2D>("assets/icons/ohgi_c.png");
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null)
            return;

        if (Config.ShowHud)
            DrawProgressHud(e.SpriteBatch);

        if (Config.ShowCooldownHud)
            DrawCooldownHud(e.SpriteBatch);
    }

    private void DrawProgressHud(SpriteBatch b)
    {
        string text;

        if (Data.SwordLevel >= Config.MaxSwordLevel)
        {
            text = I18n.Get("hud.master", new { level = Data.SwordLevel, sp = Data.UnspentSkillPoints });
        }
        else
        {
            int req = Progression.GetRequiredXp(Data.SwordLevel);
            text = I18n.Get("hud.progress", new { level = Data.SwordLevel, exp = Data.SwordExperience, req, sp = Data.UnspentSkillPoints });
        }

        float hudX = Math.Clamp(Config.HudX, 0, Math.Max(0, Game1.uiViewport.Width - 220));
        float hudY = Math.Clamp(Config.HudY, 0, Math.Max(0, Game1.uiViewport.Height - 80));

        Vector2 pos = new(hudX, hudY);

        b.DrawString(
            Game1.smallFont,
            text,
            pos + new Vector2(2f, 2f),
            Color.Black * 0.7f
        );
        b.DrawString(Game1.smallFont, text, pos, Color.White);

        string prompt = I18n.Get("hud.open-menu", new { key = Config.OpenMenuKey });
        Vector2 promptPos = new(hudX, hudY + 28f);

        b.DrawString(
            Game1.smallFont,
            prompt,
            promptPos + new Vector2(2f, 2f),
            Color.Black * 0.7f
        );
        b.DrawString(Game1.smallFont, prompt, promptPos, Color.White);
    }

    private void DrawCooldownHud(SpriteBatch b)
    {
        IReadOnlyList<CooldownStatus> statuses = Combat.GetCooldownStatuses(Data);

        if (statuses.Count == 0)
            return;

        const int iconSize = 46;
        const int gap = 8;

        int totalWidth = statuses.Count * iconSize + Math.Max(0, statuses.Count - 1) * gap;
        float baseX = Math.Clamp(
            Config.CooldownHudX,
            0,
            Math.Max(0, Game1.uiViewport.Width - totalWidth)
        );
        float baseY = Math.Clamp(
            Config.CooldownHudY,
            0,
            Math.Max(0, Game1.uiViewport.Height - iconSize - 4)
        );

        for (int i = 0; i < statuses.Count; i++)
        {
            CooldownStatus status = statuses[i];

            if (!CooldownIcons.TryGetValue(status.SkillId, out Texture2D? icon))
                continue;

            int x = (int)baseX + i * (iconSize + gap);
            int y = (int)baseY;

            Rectangle border = new(x - 2, y - 2, iconSize + 4, iconSize + 4);
            b.Draw(Game1.staminaRect, border, new Color(77, 45, 25) * 0.90f);

            Rectangle iconRect = new(x, y, iconSize, iconSize);
            Color iconColor = status.Available ? Color.White : Color.White * 0.28f;
            b.Draw(icon, iconRect, iconColor);

            if (!status.Available)
            {
                b.Draw(Game1.staminaRect, iconRect, Color.Black * 0.48f);
                continue;
            }

            if (status.RemainingTicks <= 0 || status.MaxTicks <= 0)
            {
                Rectangle readyBorder = new(x, y, iconSize, 3);
                b.Draw(Game1.staminaRect, readyBorder, new Color(255, 210, 70) * 0.95f);
                continue;
            }

            float ratio = Math.Clamp(
                status.RemainingTicks / (float)status.MaxTicks,
                0f,
                1f
            );

            int overlayHeight = (int)Math.Ceiling(iconSize * ratio);
            Rectangle overlay = new(
                x,
                y,
                iconSize,
                overlayHeight
            );

            b.Draw(Game1.staminaRect, overlay, Color.Black * 0.62f);

            string seconds = (status.RemainingTicks / 60f).ToString("0.0");
            Vector2 textSize = Game1.tinyFont.MeasureString(seconds);
            Vector2 textPos = new(
                x + (iconSize - textSize.X) / 2f,
                y + (iconSize - textSize.Y) / 2f
            );

            b.DrawString(
                Game1.tinyFont,
                seconds,
                textPos + new Vector2(1f, 1f),
                Color.Black
            );
            b.DrawString(Game1.tinyFont, seconds, textPos, Color.White);
        }
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
            I18n.Get("reset.scheduled", new { cost = Config.ResetCost }),
            HUDMessage.newQuest_type
        ));
    }
}
