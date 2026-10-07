using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    private readonly Dictionary<string, ClickableComponent> PlusButtons = new();
    private readonly Dictionary<string, ClickableComponent> SelectButtons = new();
    private readonly Dictionary<string, ClickableComponent> RowAreas = new();

    private string? HoveredSkillId;

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

    private readonly Dictionary<string, string> SkillSubtitles = new()
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
            "2단계: 전방 넓은 범위 베기",
            "3단계: 직선 검기 발생"
        },
        ["BasicC"] = new[]
        {
            "1단계: 전방 3회 베기",
            "2단계: 전방 180도 4회 베기",
            "3단계: 전방 180도 6회 베기"
        },
        ["OhgiA"] = new[]
        {
            "기본스킬 A 마스터 필요",
            "1단계: 경로상 모든 적 강타",
            "2단계: 경로 폭 증가",
            "3단계: 경로 폭 대폭 증가"
        },
        ["OhgiB"] = new[]
        {
            "기본스킬 B 마스터 필요",
            "1단계: 기본 공격 검기화",
            "2단계: 사거리/폭 증가",
            "3단계: 무한 사거리·전 관통"
        },
        ["OhgiC"] = new[]
        {
            "기본스킬 C 마스터 필요",
            "1단계: 전방위 3회 타격",
            "2단계: 범위 증가 + 6회",
            "3단계: 범위 증가 + 10회"
        },
        ["UltimateA"] = new[]
        {
            "주변 적 자동 참격 패시브",
            "1단계: 작은 범위·약한 피해",
            "2단계: 중간 범위·보통 피해",
            "3단계: 큰 범위·강한 피해"
        },
        ["UltimateB"] = new[]
        {
            "무제한 이동기 + 주변 검격",
            "1단계: 짧은 거리·약한 피해",
            "2단계: 중간 거리·보통 피해",
            "3단계: 긴 거리·강한 피해"
        }
    };

    public SkillTreeMenu(
        SaveData data,
        ModConfig config,
        ProgressionService progression,
        SkillService skills
    )
        : base(
            x: Math.Max(32, (Game1.uiViewport.Width - Math.Min(1040, Game1.uiViewport.Width - 64)) / 2),
            y: Math.Max(32, (Game1.uiViewport.Height - Math.Min(720, Game1.uiViewport.Height - 64)) / 2),
            width: Math.Min(1040, Game1.uiViewport.Width - 64),
            height: Math.Min(720, Game1.uiViewport.Height - 64),
            showUpperRightCloseButton: true
        )
    {
        Data = data;
        Config = config;
        Progression = progression;
        Skills = skills;

        BuildButtons();
    }

    private void BuildButtons()
    {
        PlusButtons.Clear();
        SelectButtons.Clear();
        RowAreas.Clear();

        int rowY = yPositionOnScreen + 126;

        AddRow("BasicA", rowY, false);
        rowY += 52;
        AddRow("BasicB", rowY, false);
        rowY += 52;
        AddRow("BasicC", rowY, false);

        rowY += 78;

        AddRow("OhgiA", rowY, true);
        rowY += 52;
        AddRow("OhgiB", rowY, true);
        rowY += 52;
        AddRow("OhgiC", rowY, true);

        rowY += 78;

        AddRow("UltimateA", rowY, true);
        rowY += 52;
        AddRow("UltimateB", rowY, true);
    }

    private void AddRow(string id, int y, bool hasSelect)
    {
        RowAreas[id] = new ClickableComponent(
            new Rectangle(xPositionOnScreen + 30, y, width - 60, 46),
            id
        );

        if (hasSelect)
        {
            SelectButtons[id] = new ClickableComponent(
                new Rectangle(xPositionOnScreen + width - 210, y + 4, 112, 40),
                id
            );
        }

        PlusButtons[id] = new ClickableComponent(
            new Rectangle(xPositionOnScreen + width - 86, y + 4, 46, 40),
            id
        );
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);

        HoveredSkillId = null;

        foreach (var pair in RowAreas)
        {
            if (pair.Value.containsPoint(x, y))
            {
                HoveredSkillId = pair.Key;
                return;
            }
        }

        foreach (var pair in SelectButtons)
        {
            if (pair.Value.containsPoint(x, y))
            {
                HoveredSkillId = pair.Key;
                return;
            }
        }

        foreach (var pair in PlusButtons)
        {
            if (pair.Value.containsPoint(x, y))
            {
                HoveredSkillId = pair.Key;
                return;
            }
        }
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (upperRightCloseButton?.containsPoint(x, y) == true)
            return;

        foreach (var pair in SelectButtons)
        {
            if (!pair.Value.containsPoint(x, y))
                continue;

            string id = pair.Key;
            bool ok = false;
            string message = "";

            if (id.StartsWith("Ohgi"))
                ok = Skills.TrySelectOhgi(Data, id[^1].ToString(), out message);
            else if (id.StartsWith("Ultimate"))
                ok = Skills.TrySelectUltimate(Data, id[^1].ToString(), out message);

            if (ok)
            {
                Game1.playSound("coin");
                Skills.UpdateUnlockState(Data);
            }
            else
            {
                Game1.playSound("cancel");
            }

            Game1.addHUDMessage(new HUDMessage(message));
            return;
        }

        foreach (var pair in PlusButtons)
        {
            if (!pair.Value.containsPoint(x, y))
                continue;

            string id = pair.Key;
            bool ok = false;
            string message = "";

            if (id.StartsWith("Basic"))
                ok = Skills.TryAllocateBasic(Data, id[^1].ToString(), out message);
            else if (id.StartsWith("Ohgi"))
            {
                if (Data.SelectedOhgi == id[^1].ToString())
                    ok = Skills.TryAllocateOhgi(Data, out message);
                else
                    message = "선택한 오의만 강화할 수 있습니다.";
            }
            else if (id.StartsWith("Ultimate"))
            {
                if (Data.SelectedUltimate == id[^1].ToString())
                    ok = Skills.TryAllocateUltimate(Data, out message);
                else
                    message = "선택한 극의만 강화할 수 있습니다.";
            }

            if (ok)
            {
                Game1.playSound("coin");
                Skills.UpdateUnlockState(Data);
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

        IClickableMenu.drawTextureBox(
            b,
            xPositionOnScreen,
            yPositionOnScreen,
            width,
            height,
            Color.White
        );

        int left = xPositionOnScreen + 38;
        int right = xPositionOnScreen + width - 38;

        Utility.drawTextWithShadow(
            b,
            "검술",
            Game1.dialogueFont,
            new Vector2(left, yPositionOnScreen + 28),
            Game1.textColor
        );

        string expText = Data.SwordLevel >= Config.MaxSwordLevel
            ? $"Lv.{Data.SwordLevel}  MASTER"
            : $"Lv.{Data.SwordLevel}   EXP {Data.SwordExperience}/{Progression.GetRequiredXp(Data.SwordLevel)}";

        Utility.drawTextWithShadow(
            b,
            expText,
            Game1.smallFont,
            new Vector2(left + 150, yPositionOnScreen + 42),
            Game1.textColor
        );

        Utility.drawTextWithShadow(
            b,
            $"남은 SP: {Data.UnspentSkillPoints}",
            Game1.smallFont,
            new Vector2(right - 180, yPositionOnScreen + 42),
            Game1.textColor
        );

        int rowY = yPositionOnScreen + 100;

        DrawSectionTitle(b, "기본 스킬", rowY);
        rowY += 26;
        DrawSkillRow(b, "BasicA", rowY, "A", "basic");
        rowY += 52;
        DrawSkillRow(b, "BasicB", rowY, "B", "basic");
        rowY += 52;
        DrawSkillRow(b, "BasicC", rowY, "C", "basic");

        rowY += 58;
        string ohgiState = Data.OhgiAccessGranted
            ? "해방됨"
            : Data.OhgiQuestAvailable ? "해방 퀘스트 가능" : "잠김";
        DrawSectionTitle(b, $"오의  —  {ohgiState}", rowY);
        rowY += 26;
        DrawSkillRow(b, "OhgiA", rowY, "A", "ohgi");
        rowY += 52;
        DrawSkillRow(b, "OhgiB", rowY, "B", "ohgi");
        rowY += 52;
        DrawSkillRow(b, "OhgiC", rowY, "C", "ohgi");

        rowY += 58;
        string ultimateState = Data.UltimateAccessGranted
            ? "해방됨"
            : Data.UltimateQuestAvailable ? "해방 퀘스트 가능" : "잠김";
        DrawSectionTitle(b, $"극의  —  {ultimateState}", rowY);
        rowY += 26;
        DrawSkillRow(b, "UltimateA", rowY, "A", "ultimate");
        rowY += 52;
        DrawSkillRow(b, "UltimateB", rowY, "B", "ultimate");

        Utility.drawTextWithShadow(
            b,
            $"[{Config.OpenMenuKey}] 닫기  ·  기술 이름은 마우스를 올리면 표시",
            Game1.smallFont,
            new Vector2(left, yPositionOnScreen + height - 40),
            Color.DimGray
        );

        if (!string.IsNullOrEmpty(HoveredSkillId))
            DrawTooltip(b, HoveredSkillId!);

        upperRightCloseButton?.draw(b);
        drawMouse(b);
    }

    private void DrawSectionTitle(SpriteBatch b, string title, int y)
    {
        Utility.drawTextWithShadow(
            b,
            title,
            Game1.smallFont,
            new Vector2(xPositionOnScreen + 38, y),
            Color.DarkSlateBlue
        );
    }

    private void DrawSkillRow(SpriteBatch b, string id, int y, string badgeText, string group)
    {
        var skill = Data.Skills[id];

        Rectangle row = RowAreas[id].bounds;
        bool hovered = HoveredSkillId == id;

        b.Draw(Game1.staminaRect, row, hovered ? Color.Goldenrod * 0.14f : Color.Black * 0.08f);

        Rectangle badge = new(row.X + 8, row.Y + 5, 36, 36);
        IClickableMenu.drawTextureBox(b, badge.X, badge.Y, badge.Width, badge.Height, Color.White);

        Vector2 badgeSize = Game1.smallFont.MeasureString(badgeText);
        Utility.drawTextWithShadow(
            b,
            badgeText,
            Game1.smallFont,
            new Vector2(badge.Center.X - badgeSize.X / 2f, badge.Center.Y - badgeSize.Y / 2f),
            Game1.textColor
        );

        string progress = $"I {skill.Stage1}/5   II {skill.Stage2}/5   III {skill.Stage3}/5";
        Utility.drawTextWithShadow(
            b,
            progress,
            Game1.smallFont,
            new Vector2(row.X + 64, row.Y + 12),
            skill.IsMastered ? Color.DarkGreen : Game1.textColor
        );

        bool selected = group switch
        {
            "ohgi" => Data.SelectedOhgi == id[^1].ToString(),
            "ultimate" => Data.SelectedUltimate == id[^1].ToString(),
            _ => true
        };

        if (group != "basic")
        {
            bool access = group == "ohgi" ? Data.OhgiAccessGranted : Data.UltimateAccessGranted;
            string selectedBranch = group == "ohgi" ? Data.SelectedOhgi ?? "" : Data.SelectedUltimate ?? "";

            string label;
            Color labelColor;

            if (!access)
            {
                label = "잠김";
                labelColor = Color.Gray;
            }
            else if (selected)
            {
                label = "선택됨";
                labelColor = Color.DarkGreen;
            }
            else if (!string.IsNullOrEmpty(selectedBranch))
            {
                label = "선택불가";
                labelColor = Color.Gray;
            }
            else
            {
                label = "선택";
                labelColor = Color.DarkSlateBlue;
            }

            DrawButton(b, SelectButtons[id].bounds, label, labelColor);
        }

        bool canPlus = group == "basic" || selected;
        DrawButton(
            b,
            PlusButtons[id].bounds,
            "+",
            canPlus && !skill.IsMastered ? Color.DarkGreen : Color.Gray
        );
    }

    private void DrawTooltip(SpriteBatch b, string id)
    {
        string title = SkillNames[id];
        string subtitle = SkillSubtitles[id];
        string[] desc = SkillDescriptions[id];
        int current = Data.Skills[id].TotalPoints;
        int max = 15;

        int x = xPositionOnScreen - 320;
        int y = yPositionOnScreen + 110;
        int w = 300;
        int h = 210;

        if (x < 16)
            x = xPositionOnScreen + width + 16;

        IClickableMenu.drawTextureBox(b, x, y, w, h, Color.White);

        Utility.drawTextWithShadow(
            b,
            title,
            Game1.dialogueFont,
            new Vector2(x + 18, y + 16),
            Color.Goldenrod
        );

        Utility.drawTextWithShadow(
            b,
            subtitle,
            Game1.smallFont,
            new Vector2(x + 18, y + 58),
            Color.BurlyWood
        );

        int lineY = y + 90;
        foreach (string line in desc)
        {
            Utility.drawTextWithShadow(
                b,
                line,
                Game1.smallFont,
                new Vector2(x + 18, lineY),
                Game1.textColor
            );
            lineY += 26;
        }

        Utility.drawTextWithShadow(
            b,
            $"현재 투자: {current} / {max}",
            Game1.smallFont,
            new Vector2(x + 18, y + h - 34),
            Color.Goldenrod
        );
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string text, Color color)
    {
        IClickableMenu.drawTextureBox(
            b,
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            Color.White
        );

        Vector2 size = Game1.smallFont.MeasureString(text);
        Vector2 pos = new(
            bounds.Center.X - size.X / 2f,
            bounds.Center.Y - size.Y / 2f
        );

        Utility.drawTextWithShadow(b, text, Game1.smallFont, pos, color);
    }
}
