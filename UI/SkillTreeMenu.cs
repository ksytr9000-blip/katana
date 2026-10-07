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
        ["BasicA"] = "기본 스킬 A",
        ["BasicB"] = "기본 스킬 B",
        ["BasicC"] = "기본 스킬 C",

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
            x: Math.Max(20, (Game1.uiViewport.Width - Math.Min(1120, Game1.uiViewport.Width - 40)) / 2),
            y: Math.Max(20, (Game1.uiViewport.Height - Math.Min(800, Game1.uiViewport.Height - 40)) / 2),
            width: Math.Min(1120, Game1.uiViewport.Width - 40),
            height: Math.Min(800, Game1.uiViewport.Height - 40),
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
        int basicY = yPositionOnScreen + 190;
        int ohgiY = yPositionOnScreen + 410;
        int ultimateY = yPositionOnScreen + 625;

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
            new Rectangle(x, y, tile, tile + 62),
            id
        );
    }

    private int GetTileSize()
    {
        if (width < 900 || height < 720)
            return 86;

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

    public override void draw(SpriteBatch b)
    {
        drawBackground(b);

        DrawMainPanel(b);
        DrawHeader(b);
        DrawSectionLabel(b, "기본 스킬", yPositionOnScreen + 150);
        DrawSectionLabel(b, "오의", yPositionOnScreen + 370);
        DrawSectionLabel(b, "극의", yPositionOnScreen + 585);

        DrawBranchNote(
            b,
            Data.SelectedOhgi is null
                ? "3가지 중 1가지만 선택할 수 있습니다."
                : "선택한 오의 외의 분기는 잠겨 있습니다.",
            yPositionOnScreen + 392
        );

        DrawBranchNote(
            b,
            Data.SelectedUltimate is null
                ? "2가지 중 1가지만 선택할 수 있습니다."
                : "선택한 극의 외의 분기는 잠겨 있습니다.",
            yPositionOnScreen + 607
        );

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
            new Vector2(left + 190, yPositionOnScreen + 94),
            Game1.textColor
        );

        Rectangle barBack = new(left + 350, yPositionOnScreen + 98, Math.Max(120, width - 650), 18);
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
        int w = (int)size.X + 90;
        int x = xPositionOnScreen + (width - w) / 2;

        IClickableMenu.drawTextureBox(
            b,
            x,
            y,
            w,
            38,
            Color.White
        );

        Utility.drawTextWithShadow(
            b,
            text,
            Game1.smallFont,
            new Vector2(x + (w - size.X) / 2, y + 8),
            new Color(89, 48, 24)
        );
    }

    private void DrawBranchNote(SpriteBatch b, string text, int y)
    {
        Vector2 size = Game1.smallFont.MeasureString(text);
        Utility.drawTextWithShadow(
            b,
            text,
            Game1.smallFont,
            new Vector2(xPositionOnScreen + (width - size.X) / 2, y),
            Color.DimGray
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

        DrawStagePips(b, id, area.X + tile / 2, area.Y + tile + 10, locked);
    }

    private void DrawStagePips(SpriteBatch b, string id, int centerX, int y, bool locked)
    {
        SkillProgress p = Data.Skills[id];

        DrawPipRow(b, "I", p.Stage1, centerX, y, locked);
        DrawPipRow(b, "II", p.Stage2, centerX, y + 17, locked);
        DrawPipRow(b, "III", p.Stage3, centerX, y + 34, locked);
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
            new Vector2(startX - 28 - labelSize.X / 2, y - 4),
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

        int tooltipWidth = 390;
        int tooltipHeight = 150 + lines.Length * 27;

        int x = HoverX + 26;
        int y = HoverY + 24;

        if (x + tooltipWidth > Game1.uiViewport.Width - 16)
            x = HoverX - tooltipWidth - 22;

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
            new Color(255, 245, 220)
        );

        Rectangle icon = new(x + 18, y + 18, 64, 64);
        b.Draw(Icons[id], icon, IsLocked(id, GetGroup(id)) ? Color.White * 0.35f : Color.White);

        Utility.drawTextWithShadow(
            b,
            title,
            Game1.dialogueFont,
            new Vector2(x + 96, y + 13),
            new Color(112, 68, 28)
        );

        Utility.drawTextWithShadow(
            b,
            type,
            Game1.smallFont,
            new Vector2(x + 98, y + 58),
            Color.DimGray
        );

        int lineY = y + 92;
        foreach (string line in lines)
        {
            Utility.drawTextWithShadow(
                b,
                line,
                Game1.smallFont,
                new Vector2(x + 18, lineY),
                Game1.textColor
            );
            lineY += 27;
        }

        Utility.drawTextWithShadow(
            b,
            $"현재 투자: {Data.Skills[id].TotalPoints} / 15",
            Game1.smallFont,
            new Vector2(x + 18, tooltipHeight + y - 49),
            new Color(126, 76, 31)
        );

        Utility.drawTextWithShadow(
            b,
            state,
            Game1.smallFont,
            new Vector2(x + 18, tooltipHeight + y - 25),
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
