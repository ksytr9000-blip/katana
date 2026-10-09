using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SwordMastery;
using SwordMastery.Models;
using SwordMastery.Services;

namespace SwordMastery.UI;

internal sealed class SkillTreeMenu : IClickableMenu
{
    private readonly SaveData Data;
    private readonly ModConfig Config;
    private readonly ProgressionService Progression;
    private readonly SkillService Skills;

    private readonly Dictionary<string, Texture2D> Icons = new();
    private readonly Dictionary<string, ClickableComponent> SkillAreas = new();

    private string? HoveredSkillId;
    private int HoverX;
    private int HoverY;

    private readonly Dictionary<string, string> SkillNames = new()
    {
        ["BasicA"] = I18n.Get("skill.basic-a.name"),
        ["BasicB"] = I18n.Get("skill.basic-b.name"),
        ["BasicC"] = I18n.Get("skill.basic-c.name"),

        ["OhgiA"] = I18n.Get("skill.ohgi-a.name"),
        ["OhgiB"] = I18n.Get("skill.ohgi-b.name"),
        ["OhgiC"] = I18n.Get("skill.ohgi-c.name"),

        ["UltimateA"] = I18n.Get("skill.ultimate-a.name"),
        ["UltimateB"] = I18n.Get("skill.ultimate-b.name")
    };

    private readonly Dictionary<string, string[]> SkillDescriptions = new()
    {
        ["BasicA"] = new[]
        {
            I18n.Get("skill.basic-a.1"),
            I18n.Get("skill.basic-a.2"),
            I18n.Get("skill.basic-a.3")
        },
        ["BasicB"] = new[]
        {
            I18n.Get("skill.basic-b.1"),
            I18n.Get("skill.basic-b.2"),
            I18n.Get("skill.basic-b.3")
        },
        ["BasicC"] = new[]
        {
            I18n.Get("skill.basic-c.1"),
            I18n.Get("skill.basic-c.2"),
            I18n.Get("skill.basic-c.3")
        },

        ["OhgiA"] = new[]
        {
            I18n.Get("skill.ohgi-a.1"),
            I18n.Get("skill.ohgi-a.2"),
            I18n.Get("skill.ohgi-a.3")
        },
        ["OhgiB"] = new[]
        {
            I18n.Get("skill.ohgi-b.1"),
            I18n.Get("skill.ohgi-b.2"),
            I18n.Get("skill.ohgi-b.3")
        },
        ["OhgiC"] = new[]
        {
            I18n.Get("skill.ohgi-c.1"),
            I18n.Get("skill.ohgi-c.2"),
            I18n.Get("skill.ohgi-c.3")
        },

        ["UltimateA"] = new[]
        {
            I18n.Get("skill.ultimate-a.1"),
            I18n.Get("skill.ultimate-a.2"),
            I18n.Get("skill.ultimate-a.3")
        },
        ["UltimateB"] = new[]
        {
            I18n.Get("skill.ultimate-b.1"),
            I18n.Get("skill.ultimate-b.2"),
            I18n.Get("skill.ultimate-b.3")
        }
    };

    public SkillTreeMenu(
        SaveData data,
        ModConfig config,
        ProgressionService progression,
        SkillService skills,
        IModHelper helper
    )
        : base(
            x: Math.Max(12, (Game1.uiViewport.Width - Math.Min(1380, Game1.uiViewport.Width - 24)) / 2),
            y: Math.Max(12, (Game1.uiViewport.Height - Math.Min(1000, Game1.uiViewport.Height - 24)) / 2),
            width: Math.Min(1380, Game1.uiViewport.Width - 24),
            height: Math.Min(1000, Game1.uiViewport.Height - 24),
            showUpperRightCloseButton: true
        )
    {
        Data = data;
        Config = config;
        Progression = progression;
        Skills = skills;

        Icons["BasicA"] = helper.ModContent.Load<Texture2D>("assets/icons/basic_a.png");
        Icons["BasicB"] = helper.ModContent.Load<Texture2D>("assets/icons/basic_b.png");
        Icons["BasicC"] = helper.ModContent.Load<Texture2D>("assets/icons/basic_c.png");
        Icons["OhgiA"] = helper.ModContent.Load<Texture2D>("assets/icons/ohgi_a.png");
        Icons["OhgiB"] = helper.ModContent.Load<Texture2D>("assets/icons/ohgi_b.png");
        Icons["OhgiC"] = helper.ModContent.Load<Texture2D>("assets/icons/ohgi_c.png");
        Icons["UltimateA"] = helper.ModContent.Load<Texture2D>("assets/icons/ultimate_a.png");
        Icons["UltimateB"] = helper.ModContent.Load<Texture2D>("assets/icons/ultimate_b.png");

        BuildSkillAreas();
    }

    private void BuildSkillAreas()
    {
        SkillAreas.Clear();

        int tile = GetTileSize();

        int basicLabelY = yPositionOnScreen + 155;
        int basicY = basicLabelY + 62;

        int ohgiLabelY = basicY + tile + 116;
        int ohgiY = ohgiLabelY + 62;

        int ultimateLabelY = ohgiY + tile + 116;
        int ultimateY = ultimateLabelY + 62;

        int[] basicXs = ThreeColumns(tile);
        AddSkillArea("BasicA", basicXs[0], basicY, tile);
        AddSkillArea("BasicB", basicXs[1], basicY, tile);
        AddSkillArea("BasicC", basicXs[2], basicY, tile);

        int[] ohgiXs = ThreeColumns(tile);
        AddSkillArea("OhgiA", ohgiXs[0], ohgiY, tile);
        AddSkillArea("OhgiB", ohgiXs[1], ohgiY, tile);
        AddSkillArea("OhgiC", ohgiXs[2], ohgiY, tile);

        int[] ultimateXs = TwoColumns(tile);
        AddSkillArea("UltimateA", ultimateXs[0], ultimateY, tile);
        AddSkillArea("UltimateB", ultimateXs[1], ultimateY, tile);
    }

    private void AddSkillArea(string id, int x, int y, int tile)
    {
        SkillAreas[id] = new ClickableComponent(
            new Rectangle(x, y, tile, tile + 106),
            id
        );
    }

    private int GetTileSize()
    {
        if (height < 820)
            return 88;

        if (height < 940 || width < 1100)
            return 98;

        return 108;
    }

    private int[] ThreeColumns(int tile)
    {
        int gap = Math.Max(55, (width - (tile * 3) - 240) / 2);
        int total = tile * 3 + gap * 2;
        int start = xPositionOnScreen + (width - total) / 2;

        return new[]
        {
            start,
            start + tile + gap,
            start + (tile + gap) * 2
        };
    }

    private int[] TwoColumns(int tile)
    {
        int gap = Math.Max(150, width / 4);
        int total = tile * 2 + gap;
        int start = xPositionOnScreen + (width - total) / 2;

        return new[]
        {
            start,
            start + tile + gap
        };
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);

        HoverX = x;
        HoverY = y;
        HoveredSkillId = null;

        foreach (var pair in SkillAreas)
        {
            if (pair.Value.containsPoint(x, y))
            {
                HoveredSkillId = pair.Key;
                break;
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (upperRightCloseButton?.containsPoint(x, y) == true)
            return;

        foreach (var pair in SkillAreas)
        {
            if (!pair.Value.containsPoint(x, y))
                continue;

            string id = pair.Key;
            bool ok = false;
            string message;

            bool prevOhgiQuest = Data.OhgiQuestAvailable;
            bool prevUltimateQuest = Data.UltimateQuestAvailable;
            bool prevOhgiAccess = Data.OhgiAccessGranted;
            bool prevUltimateAccess = Data.UltimateAccessGranted;

            if (id.StartsWith("Basic"))
            {
                ok = Skills.TryAllocateBasic(Data, id[^1].ToString(), out message);
            }
            else if (id.StartsWith("Ohgi"))
            {
                string branch = id[^1].ToString();

                if (!Data.OhgiAccessGranted)
                {
                    message = I18n.Get("ui.msg.ohgi-locked");
                }
                else if (Data.SelectedOhgi is null)
                {
                    ok = Skills.TrySelectOhgi(Data, branch, out message);
                }
                else if (Data.SelectedOhgi == branch)
                {
                    ok = Skills.TryAllocateOhgi(Data, out message);
                }
                else
                {
                    message = I18n.Get("ui.msg.ohgi-other");
                }
            }
            else
            {
                string branch = id[^1].ToString();

                if (!Data.UltimateAccessGranted)
                {
                    message = I18n.Get("ui.msg.ultimate-locked");
                }
                else if (Data.SelectedUltimate is null)
                {
                    ok = Skills.TrySelectUltimate(Data, branch, out message);
                }
                else if (Data.SelectedUltimate == branch)
                {
                    ok = Skills.TryAllocateUltimate(Data, out message);
                }
                else
                {
                    message = I18n.Get("ui.msg.ultimate-other");
                }
            }

            if (ok)
            {
                Skills.UpdateUnlockState(Data);
                ShowUnlockNotifications(prevOhgiQuest, prevUltimateQuest, prevOhgiAccess, prevUltimateAccess);
                Game1.playSound("coin");
            }
            else
            {
                Game1.playSound("cancel");
            }

            Game1.addHUDMessage(new HUDMessage(message));
            return;
        }
    }


    private void ShowUnlockNotifications(
        bool prevOhgiQuest,
        bool prevUltimateQuest,
        bool prevOhgiAccess,
        bool prevUltimateAccess
    )
    {
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
            Game1.addHUDMessage(new HUDMessage(I18n.Get("notify.ohgi.unlocked"), HUDMessage.newQuest_type));

        if (!prevUltimateAccess && Data.UltimateAccessGranted)
            Game1.addHUDMessage(new HUDMessage(I18n.Get("notify.ultimate.unlocked"), HUDMessage.newQuest_type));
    }

    public override void draw(SpriteBatch b)
    {
        drawBackground(b);

        DrawMainPanel(b);
        DrawHeader(b);
        int tile = GetTileSize();

        int basicLabelY = yPositionOnScreen + 155;
        int basicY = basicLabelY + 62;

        int ohgiLabelY = basicY + tile + 116;
        int ohgiY = ohgiLabelY + 62;

        int ultimateLabelY = ohgiY + tile + 116;

        DrawSectionLabel(b, I18n.Get("ui.section.basic"), basicLabelY);
        DrawSectionLabel(b, I18n.Get("ui.section.ohgi"), ohgiLabelY);
        DrawSectionLabel(b, I18n.Get("ui.section.ultimate"), ultimateLabelY);

        DrawSkill(b, "BasicA", "basic");
        DrawSkill(b, "BasicB", "basic");
        DrawSkill(b, "BasicC", "basic");

        DrawSkill(b, "OhgiA", "ohgi");
        DrawSkill(b, "OhgiB", "ohgi");
        DrawSkill(b, "OhgiC", "ohgi");

        DrawSkill(b, "UltimateA", "ultimate");
        DrawSkill(b, "UltimateB", "ultimate");

        if (!string.IsNullOrEmpty(HoveredSkillId))
            DrawTooltip(b, HoveredSkillId!);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawMainPanel(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(
            b,
            xPositionOnScreen,
            yPositionOnScreen,
            width,
            height,
            Color.White
        );

        Rectangle inner = new(
            xPositionOnScreen + 18,
            yPositionOnScreen + 18,
            width - 36,
            height - 36
        );

        b.Draw(Game1.staminaRect, inner, new Color(255, 235, 186) * 0.20f);
    }

    private void DrawHeader(SpriteBatch b)
    {
        int left = xPositionOnScreen + 42;
        int right = xPositionOnScreen + width - 42;

        Utility.drawTextWithShadow(
            b,
            I18n.Get("ui.title"),
            Game1.dialogueFont,
            new Vector2(xPositionOnScreen + width / 2 - 52, yPositionOnScreen + 26),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            I18n.Get("ui.level", new { level = Data.SwordLevel }),
            Game1.smallFont,
            new Vector2(left, yPositionOnScreen + 94),
            Game1.textColor
        );

        int req = Data.SwordLevel >= Config.MaxSwordLevel
            ? 1
            : Math.Max(1, Progression.GetRequiredXp(Data.SwordLevel));

        string expText = Data.SwordLevel >= Config.MaxSwordLevel
            ? "MASTER"
            : $"EXP {Data.SwordExperience} / {req}";

        Utility.drawTextWithShadow(
            b,
            expText,
            Game1.smallFont,
            new Vector2(left + 205, yPositionOnScreen + 94),
            Game1.textColor
        );

        bool wideHeader = width >= 1100;
        int barX = wideHeader ? left + 500 : left + 365;
        int barWidth = wideHeader ? 220 : 150;

        int maxBarRight = right - 235;
        if (barX + barWidth > maxBarRight)
            barWidth = Math.Max(120, maxBarRight - barX);

        Rectangle barBack = new(barX, yPositionOnScreen + 98, barWidth, 18);
        b.Draw(Game1.staminaRect, barBack, new Color(94, 52, 28));

        float ratio = Data.SwordLevel >= Config.MaxSwordLevel
            ? 1f
            : Math.Clamp((float)Data.SwordExperience / req, 0f, 1f);

        Rectangle barFill = new(
            barBack.X + 3,
            barBack.Y + 3,
            (int)((barBack.Width - 6) * ratio),
            barBack.Height - 6
        );
        b.Draw(Game1.staminaRect, barFill, new Color(83, 176, 74));

        Utility.drawTextWithShadow(
            b,
            I18n.Get("ui.remaining-sp", new { sp = Data.UnspentSkillPoints }),
            Game1.smallFont,
            new Vector2(right - 160, yPositionOnScreen + 94),
            Game1.textColor
        );
    }

    private void DrawSectionLabel(SpriteBatch b, string text, int y)
    {
        Vector2 size = Game1.smallFont.MeasureString(text);
        int w = Math.Max(300, (int)size.X + 150);
        int h = 52;
        int x = xPositionOnScreen + (width - w) / 2;

        IClickableMenu.drawTextureBox(
            b,
            x,
            y,
            w,
            h,
            Color.White
        );

        Utility.drawTextWithShadow(
            b,
            text,
            Game1.smallFont,
            new Vector2(x + (w - size.X) / 2, y + (h - size.Y) / 2 - 1),
            new Color(89, 48, 24)
        );
    }

    private void DrawSkill(SpriteBatch b, string id, string group)
    {
        Rectangle area = SkillAreas[id].bounds;
        int tile = area.Width;
        bool selected = IsSelected(id, group);
        bool locked = IsLocked(id, group);
        bool hovered = HoveredSkillId == id;

        Rectangle glow = new(area.X - 6, area.Y - 6, tile + 12, tile + 12);

        if (selected)
        {
            IClickableMenu.drawTextureBox(
                b,
                glow.X,
                glow.Y,
                glow.Width,
                glow.Height,
                new Color(255, 191, 54)
            );
        }
        else if (hovered && !locked)
        {
            IClickableMenu.drawTextureBox(
                b,
                glow.X,
                glow.Y,
                glow.Width,
                glow.Height,
                new Color(255, 226, 144)
            );
        }

        IClickableMenu.drawTextureBox(
            b,
            area.X,
            area.Y,
            tile,
            tile,
            locked ? new Color(150, 150, 150) : Color.White
        );

        Rectangle iconRect = new(area.X + 8, area.Y + 8, tile - 16, tile - 16);
        b.Draw(Icons[id], iconRect, locked ? Color.White * 0.28f : Color.White);

        if (locked)
        {
            b.Draw(Game1.staminaRect, iconRect, Color.Black * 0.42f);
            DrawLock(b, new Rectangle(area.Right - 33, area.Bottom - 33 - 62, 24, 24));
        }

        if (Data.Skills[id].IsMastered)
        {
            Rectangle master = new(area.X + 5, area.Y + 5, 24, 18);
            b.Draw(Game1.staminaRect, master, new Color(255, 186, 39) * 0.85f);
            Utility.drawTextWithShadow(
                b,
                "M",
                Game1.tinyFont,
                new Vector2(master.X + 6, master.Y + 1),
                Color.DarkRed
            );
        }

        DrawStagePips(b, id, area.X + tile / 2, area.Y + tile + 18, locked);
    }

    private void DrawStagePips(SpriteBatch b, string id, int centerX, int y, bool locked)
    {
        SkillProgress p = Data.Skills[id];

        DrawPipRow(b, "I", p.Stage1, centerX, y, locked);
        DrawPipRow(b, "II", p.Stage2, centerX, y + 34, locked);
        DrawPipRow(b, "III", p.Stage3, centerX, y + 68, locked);
    }

    private void DrawPipRow(SpriteBatch b, string roman, int filled, int centerX, int y, bool locked)
    {
        const int pip = 9;
        const int gap = 5;
        const int count = 5;

        int totalWidth = count * pip + (count - 1) * gap;
        int startX = centerX - totalWidth / 2 + 8;

        Vector2 labelSize = Game1.tinyFont.MeasureString(roman);
        Utility.drawTextWithShadow(
            b,
            roman,
            Game1.tinyFont,
            new Vector2(startX - 30 - labelSize.X / 2, y - 5),
            Color.DimGray
        );

        for (int i = 0; i < count; i++)
        {
            Color c;
            if (locked)
                c = new Color(110, 105, 98);
            else if (i < filled)
                c = new Color(239, 145, 22);
            else
                c = new Color(176, 158, 128);

            Rectangle r = new(startX + i * (pip + gap), y, pip, pip);
            b.Draw(Game1.staminaRect, r, c);

            Rectangle inner = new(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
            b.Draw(Game1.staminaRect, inner, i < filled && !locked ? new Color(255, 211, 66) : c);
        }
    }

    private bool IsSelected(string id, string group)
    {
        if (group == "ohgi")
            return Data.SelectedOhgi == id[^1].ToString();

        if (group == "ultimate")
            return Data.SelectedUltimate == id[^1].ToString();

        return false;
    }

    private bool IsLocked(string id, string group)
    {
        if (group == "basic")
            return false;

        string branch = id[^1].ToString();

        if (group == "ohgi")
        {
            if (!Data.OhgiAccessGranted)
                return true;

            if (Data.SelectedOhgi is not null)
                return Data.SelectedOhgi != branch;

            string requiredBasic = $"Basic{branch}";
            return !Data.Skills[requiredBasic].IsMastered;
        }

        if (!Data.UltimateAccessGranted)
            return true;

        if (Data.SelectedUltimate is not null)
            return Data.SelectedUltimate != branch;

        return false;
    }

    private void DrawLock(SpriteBatch b, Rectangle r)
    {
        Color dark = new Color(91, 58, 33);
        Color gold = new Color(227, 169, 53);

        Rectangle body = new(r.X + 3, r.Y + 10, r.Width - 6, r.Height - 10);
        b.Draw(Game1.staminaRect, body, gold);

        Rectangle hole = new(r.Center.X - 2, r.Y + 15, 4, 6);
        b.Draw(Game1.staminaRect, hole, dark);

        Rectangle left = new(r.X + 6, r.Y + 4, 4, 9);
        Rectangle right = new(r.Right - 10, r.Y + 4, 4, 9);
        Rectangle top = new(r.X + 9, r.Y + 2, r.Width - 18, 4);

        b.Draw(Game1.staminaRect, left, gold);
        b.Draw(Game1.staminaRect, right, gold);
        b.Draw(Game1.staminaRect, top, gold);
    }

    private void DrawTooltip(SpriteBatch b, string id)
    {
        SkillProgress progress = Data.Skills[id];
        string title = SkillNames[id];
        string[] lines = SkillDescriptions[id];
        string state = GetStateText(id);

        int tooltipWidth = Math.Min(
            620,
            Math.Max(500, width / 2 - 36)
        );

        int contentWidth = tooltipWidth - 44;

        // Reserve clear columns for the roman stage label and [0/5] counter.
        // This prevents I / II / III from colliding with the investment text.
        int descriptionWidth = Math.Max(180, contentWidth - 210);

        string[] wrapped = new string[3];
        int[] rowHeights = new int[3];

        for (int i = 0; i < 3; i++)
        {
            string source = lines.Length > i ? lines[i] : "";
            wrapped[i] = Game1.parseText(
                source,
                Game1.smallFont,
                descriptionWidth
            );

            float textHeight = Game1.smallFont.MeasureString(wrapped[i]).Y;
            rowHeights[i] = Math.Max(50, (int)Math.Ceiling(textHeight) + 18);
        }

        int headerHeight = 128;
        int footerHeight = 108;
        int stageBlockHeight = rowHeights.Sum();
        int tooltipHeight = headerHeight + stageBlockHeight + footerHeight + 26;

        tooltipHeight = Math.Min(
            tooltipHeight,
            height - 134
        );

        bool showOnRight =
            HoverX < xPositionOnScreen + width / 2;

        int x = showOnRight
            ? xPositionOnScreen + width - tooltipWidth - 26
            : xPositionOnScreen + 26;

        int y = Math.Max(
            yPositionOnScreen + 88,
            yPositionOnScreen + (height - tooltipHeight) / 2
        );

        Rectangle card = new(
            x,
            y,
            tooltipWidth,
            tooltipHeight
        );

        IClickableMenu.drawTextureBox(
            b,
            card.X,
            card.Y,
            card.Width,
            card.Height,
            new Color(255, 246, 214)
        );

        Rectangle inner = new(
            card.X + 12,
            card.Y + 12,
            card.Width - 24,
            card.Height - 24
        );

        b.Draw(
            Game1.staminaRect,
            inner,
            new Color(250, 226, 171) * 0.90f
        );

        Rectangle iconFrame = new(
            card.X + 22,
            card.Y + 20,
            82,
            82
        );

        IClickableMenu.drawTextureBox(
            b,
            iconFrame.X,
            iconFrame.Y,
            iconFrame.Width,
            iconFrame.Height,
            Color.White
        );

        Rectangle icon = new(
            iconFrame.X + 7,
            iconFrame.Y + 7,
            iconFrame.Width - 14,
            iconFrame.Height - 14
        );

        b.Draw(
            Icons[id],
            icon,
            IsLocked(id, GetGroup(id))
                ? Color.White * 0.40f
                : Color.White
        );

        string wrappedTitle = Game1.parseText(
            title,
            Game1.smallFont,
            tooltipWidth - 150
        );

        Utility.drawTextWithShadow(
            b,
            wrappedTitle,
            Game1.smallFont,
            new Vector2(card.X + 122, card.Y + 24),
            new Color(94, 50, 24)
        );

        int currentStage = GetCurrentStage(progress);
        string stageText = currentStage <= 0
            ? I18n.Get("ui.unlearned")
            : currentStage >= 3 && progress.Stage3 >= 5
                ? "MASTER"
                : I18n.Get("ui.current-stage", new { stage = ToRoman(currentStage) });

        Utility.drawTextWithShadow(
            b,
            stageText,
            Game1.smallFont,
            new Vector2(card.X + 122, card.Y + 72),
            currentStage > 0
                ? new Color(58, 118, 53)
                : Color.DimGray
        );

        Rectangle headerLine = new(
            card.X + 22,
            card.Y + 116,
            card.Width - 44,
            3
        );

        b.Draw(
            Game1.staminaRect,
            headerLine,
            new Color(154, 100, 52) * 0.75f
        );

        int rowY = card.Y + 130;

        for (int i = 0; i < 3; i++)
        {
            int invested = i switch
            {
                0 => progress.Stage1,
                1 => progress.Stage2,
                _ => progress.Stage3
            };

            DrawTooltipStageRow(
                b,
                card.X + 24,
                rowY,
                i switch
                {
                    0 => "I",
                    1 => "II",
                    _ => "III"
                },
                invested,
                wrapped[i],
                tooltipWidth - 48,
                rowHeights[i]
            );

            rowY += rowHeights[i];
        }

        int footerY = Math.Min(
            rowY + 8,
            card.Bottom - footerHeight
        );

        Rectangle footerLine = new(
            card.X + 22,
            footerY,
            card.Width - 44,
            3
        );

        b.Draw(
            Game1.staminaRect,
            footerLine,
            new Color(154, 100, 52) * 0.60f
        );

        string totalText = I18n.Get("ui.total-invested", new { points = progress.TotalPoints });

        // Footer is now two separate rows so "총 투자" and the state/action
        // text never overlap, even with longer Korean labels.
        Utility.drawTextWithShadow(
            b,
            totalText,
            Game1.smallFont,
            new Vector2(card.X + 28, footerY + 14),
            new Color(96, 58, 28)
        );

        string wrappedState = Game1.parseText(
            state,
            Game1.smallFont,
            tooltipWidth - 68
        );

        Utility.drawTextWithShadow(
            b,
            wrappedState,
            Game1.smallFont,
            new Vector2(card.X + 28, footerY + 52),
            GetReadableStateColor(id)
        );
    }

    private void DrawTooltipStageRow(
        SpriteBatch b,
        int x,
        int y,
        string roman,
        int invested,
        string wrappedDescription,
        int rowWidth,
        int rowHeight
    )
    {
        Color rowColor;

        if (invested >= 5)
            rowColor = new Color(210, 226, 182);
        else if (invested > 0)
            rowColor = new Color(244, 216, 150);
        else
            rowColor = new Color(222, 209, 180);

        Rectangle row = new(
            x,
            y,
            rowWidth,
            rowHeight - 4
        );

        b.Draw(
            Game1.staminaRect,
            row,
            rowColor * 0.72f
        );

        Utility.drawTextWithShadow(
            b,
            roman,
            Game1.smallFont,
            new Vector2(row.X + 14, row.Y + 8),
            invested > 0
                ? new Color(106, 63, 28)
                : Color.DimGray
        );

        Utility.drawTextWithShadow(
            b,
            $"{invested}/5",
            Game1.smallFont,
            new Vector2(row.X + 104, row.Y + 8),
            invested >= 5
                ? new Color(45, 110, 46)
                : new Color(109, 77, 48)
        );

        Utility.drawTextWithShadow(
            b,
            wrappedDescription,
            Game1.smallFont,
            new Vector2(row.X + 184, row.Y + 8),
            Game1.textColor
        );
    }

    private static int GetCurrentStage(SkillProgress progress)
    {
        if (progress.Stage3 > 0)
            return 3;

        if (progress.Stage2 > 0)
            return 2;

        if (progress.Stage1 > 0)
            return 1;

        return 0;
    }

    private static string ToRoman(int stage)
    {
        return stage switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => "-"
        };
    }

    private Color GetReadableStateColor(string id)
    {
        string state = GetStateText(id);

        if (IsLocked(id, GetGroup(id)))
            return new Color(115, 92, 75);

        if (Data.Skills[id].IsMastered
            || id == $"Ohgi{Data.SelectedOhgi}"
            || id == $"Ultimate{Data.SelectedUltimate}")
        {
            return new Color(49, 112, 52);
        }

        return new Color(57, 72, 125);
    }

    private string GetGroup(string id)
    {
        if (id.StartsWith("Ohgi"))
            return "ohgi";

        if (id.StartsWith("Ultimate"))
            return "ultimate";

        return "basic";
    }

    private string GetStateText(string id)
    {
        string group = GetGroup(id);

        if (group == "basic")
            return Data.Skills[id].IsMastered ? I18n.Get("ui.state.basic.master") : I18n.Get("ui.state.basic.invest");

        string branch = id[^1].ToString();

        if (group == "ohgi")
        {
            if (!Data.OhgiAccessGranted)
                return I18n.Get("ui.state.ohgi.locked");

            if (Data.SelectedOhgi is null)
            {
                if (!Data.Skills[$"Basic{branch}"].IsMastered)
                    return I18n.Get("ui.state.ohgi.require-basic", new { branch });

                return I18n.Get("ui.state.ohgi.choose");
            }

            if (Data.SelectedOhgi == branch)
                return Data.Skills[id].IsMastered ? I18n.Get("ui.state.selected.master") : I18n.Get("ui.state.selected.invest");

            return I18n.Get("ui.state.ohgi.other");
        }

        if (!Data.UltimateAccessGranted)
            return I18n.Get("ui.state.ultimate.locked");

        if (Data.SelectedUltimate is null)
            return I18n.Get("ui.state.ultimate.choose");

        if (Data.SelectedUltimate == branch)
            return Data.Skills[id].IsMastered ? I18n.Get("ui.state.selected.master") : I18n.Get("ui.state.selected.invest");

        return I18n.Get("ui.state.ultimate.other");
    }

}
