using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
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
        ["BasicA"] = "검사의 발걸음",
        ["BasicB"] = "참격",
        ["BasicC"] = "칼리코류 검술",

        ["OhgiA"] = "일섬",
        ["OhgiB"] = "검기",
        ["OhgiC"] = "검술의 정점",

        ["UltimateA"] = "검술의 극",
        ["UltimateB"] = "보법의 극"
    };

    private readonly Dictionary<string, string> SkillTypes = new()
    {
        ["BasicA"] = "기초검술 A",
        ["BasicB"] = "기초검술 B",
        ["BasicC"] = "기초검술 C",

        ["OhgiA"] = "오의 A",
        ["OhgiB"] = "오의 B",
        ["OhgiC"] = "오의 C",

        ["UltimateA"] = "극의 A",
        ["UltimateB"] = "극의 B"
    };

    private readonly Dictionary<string, string[]> SkillDescriptions = new()
    {
        ["BasicA"] = new[]
        {
            "1단계: 전방으로 대쉬",
            "2단계: 대쉬 경로에 1회 베기",
            "3단계: 대쉬 경로에 다회 베기"
        },
        ["BasicB"] = new[]
        {
            "1단계: 전방 베기",
            "2단계: 넓은 범위의 전방 베기",
            "3단계: 직선으로 날아가는 검기"
        },
        ["BasicC"] = new[]
        {
            "1단계: 전방 3회 베기",
            "2단계: 전방 180도 4회 베기",
            "3단계: 전방 180도 6회 베기"
        },

        ["OhgiA"] = new[]
        {
            "전방 대쉬 후 경로상의 적을 강하게 베기",
            "2단계: 경로 폭 증가",
            "3단계: 경로 폭 대폭 증가"
        },
        ["OhgiB"] = new[]
        {
            "기본 검 공격에 원거리 검기 부여",
            "2단계: 사거리 증가 + 2마리 관통",
            "3단계: 무제한 사거리 + 전 관통"
        },
        ["OhgiC"] = new[]
        {
            "전방위 다중 검격",
            "2단계: 범위 증가 + 6회 타격",
            "3단계: 범위 증가 + 10회 타격"
        },

        ["UltimateA"] = new[]
        {
            "범위 안의 적에게 상시 자동 검격",
            "2단계: 범위와 피해 증가",
            "3단계: 더 넓은 범위와 강한 피해"
        },
        ["UltimateB"] = new[]
        {
            "쿨다운 없는 이동기 + 주변 검격",
            "2단계: 이동거리와 피해 증가",
            "3단계: 긴 이동거리와 강한 피해"
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
                    message = "오의가 아직 해방되지 않았습니다.";
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
                    message = "이미 다른 오의를 선택했습니다.";
                }
            }
            else
            {
                string branch = id[^1].ToString();

                if (!Data.UltimateAccessGranted)
                {
                    message = "극의가 아직 해방되지 않았습니다.";
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
                    message = "이미 다른 극의를 선택했습니다.";
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

        DrawSectionLabel(b, "기초검술", basicLabelY);
        DrawSectionLabel(b, "오의", ohgiLabelY);
        DrawSectionLabel(b, "극의", ultimateLabelY);

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
            "검술",
            Game1.dialogueFont,
            new Vector2(xPositionOnScreen + width / 2 - 52, yPositionOnScreen + 26),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            $"검술 Lv.{Data.SwordLevel}",
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
            $"남은 SP: {Data.UnspentSkillPoints}",
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
        string title = SkillNames[id];
        string type = SkillTypes[id];
        string[] lines = SkillDescriptions[id];
        string state = GetStateText(id);

        int tooltipWidth = 470;
        int tooltipHeight = 174 + lines.Length * 32;

        int x = HoverX + 28;
        int y = HoverY + 26;

        if (x + tooltipWidth > Game1.uiViewport.Width - 16)
            x = HoverX - tooltipWidth - 24;

        if (y + tooltipHeight > Game1.uiViewport.Height - 16)
            y = Game1.uiViewport.Height - tooltipHeight - 16;

        x = Math.Max(16, x);
        y = Math.Max(16, y);

        IClickableMenu.drawTextureBox(
            b,
            x,
            y,
            tooltipWidth,
            tooltipHeight,
            Color.White
        );

        Rectangle inner = new(x + 12, y + 12, tooltipWidth - 24, tooltipHeight - 24);
        b.Draw(Game1.staminaRect, inner, new Color(74, 42, 27) * 0.96f);

        Rectangle iconFrame = new(x + 22, y + 22, 82, 82);
        IClickableMenu.drawTextureBox(
            b,
            iconFrame.X,
            iconFrame.Y,
            iconFrame.Width,
            iconFrame.Height,
            new Color(255, 220, 145)
        );

        Rectangle icon = new(iconFrame.X + 7, iconFrame.Y + 7, 68, 68);
        b.Draw(
            Icons[id],
            icon,
            IsLocked(id, GetGroup(id)) ? Color.White * 0.35f : Color.White
        );

        Utility.drawTextWithShadow(
            b,
            title,
            Game1.dialogueFont,
            new Vector2(x + 122, y + 24),
            new Color(255, 205, 92)
        );

        Utility.drawTextWithShadow(
            b,
            type,
            Game1.smallFont,
            new Vector2(x + 124, y + 70),
            new Color(221, 189, 145)
        );

        Rectangle divider = new(x + 22, y + 116, tooltipWidth - 44, 2);
        b.Draw(Game1.staminaRect, divider, new Color(183, 123, 63) * 0.8f);

        int lineY = y + 136;
        foreach (string line in lines)
        {
            Utility.drawTextWithShadow(
                b,
                line,
                Game1.smallFont,
                new Vector2(x + 28, lineY),
                Color.White
            );
            lineY += 32;
        }

        Rectangle footer = new(x + 22, y + tooltipHeight - 66, tooltipWidth - 44, 2);
        b.Draw(Game1.staminaRect, footer, new Color(183, 123, 63) * 0.65f);

        Utility.drawTextWithShadow(
            b,
            $"현재 투자: {Data.Skills[id].TotalPoints} / 15",
            Game1.smallFont,
            new Vector2(x + 28, y + tooltipHeight - 54),
            new Color(255, 205, 92)
        );

        Utility.drawTextWithShadow(
            b,
            state,
            Game1.smallFont,
            new Vector2(x + 230, y + tooltipHeight - 54),
            GetStateColor(id)
        );
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
            return Data.Skills[id].IsMastered ? "MASTER" : "클릭: SP 1 투자";

        string branch = id[^1].ToString();

        if (group == "ohgi")
        {
            if (!Data.OhgiAccessGranted)
                return "잠김: 오의 해방 필요";

            if (Data.SelectedOhgi is null)
            {
                if (!Data.Skills[$"Basic{branch}"].IsMastered)
                    return $"잠김: 기본 스킬 {branch} MASTER 필요";

                return "클릭: 이 오의를 선택";
            }

            if (Data.SelectedOhgi == branch)
                return Data.Skills[id].IsMastered ? "선택됨 · MASTER" : "선택됨 · 클릭: SP 1 투자";

            return "선택 불가: 다른 오의가 확정됨";
        }

        if (!Data.UltimateAccessGranted)
            return "잠김: 극의 해방 필요";

        if (Data.SelectedUltimate is null)
            return "클릭: 이 극의를 선택";

        if (Data.SelectedUltimate == branch)
            return Data.Skills[id].IsMastered ? "선택됨 · MASTER" : "선택됨 · 클릭: SP 1 투자";

        return "선택 불가: 다른 극의가 확정됨";
    }

    private Color GetStateColor(string id)
    {
        string state = GetStateText(id);

        if (state.StartsWith("잠김") || state.StartsWith("선택 불가"))
            return Color.Gray;

        if (state.Contains("선택됨") || state.Contains("MASTER"))
            return Color.DarkGreen;

        return Color.DarkSlateBlue;
    }
}
