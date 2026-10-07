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

    private readonly Dictionary<string, string> SkillNames = new()
    {
        ["BasicA"] = "A. 검사의 발걸음",
        ["BasicB"] = "B. 참격",
        ["BasicC"] = "C. 칼리코류 검술",

        ["OhgiA"] = "A. 일섬",
        ["OhgiB"] = "B. 검기",
        ["OhgiC"] = "C. 검술의 정점",

        ["UltimateA"] = "A. 검술의 극",
        ["UltimateB"] = "B. 보법의 극"
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

        int rowY = yPositionOnScreen + 126;

        AddPlus("BasicA", rowY);
        rowY += 52;
        AddPlus("BasicB", rowY);
        rowY += 52;
        AddPlus("BasicC", rowY);

        rowY += 78;

        AddSelect("OhgiA", rowY);
        AddPlus("OhgiA", rowY);
        rowY += 52;
        AddSelect("OhgiB", rowY);
        AddPlus("OhgiB", rowY);
        rowY += 52;
        AddSelect("OhgiC", rowY);
        AddPlus("OhgiC", rowY);

        rowY += 78;

        AddSelect("UltimateA", rowY);
        AddPlus("UltimateA", rowY);
        rowY += 52;
        AddSelect("UltimateB", rowY);
        AddPlus("UltimateB", rowY);
    }

    private void AddPlus(string id, int y)
    {
        PlusButtons[id] = new ClickableComponent(
            new Rectangle(xPositionOnScreen + width - 86, y + 4, 46, 40),
            id
        );
    }

    private void AddSelect(string id, int y)
    {
        SelectButtons[id] = new ClickableComponent(
            new Rectangle(xPositionOnScreen + width - 210, y + 4, 112, 40),
            id
        );
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
        DrawSkillRow(b, "BasicA", rowY, SkillNames["BasicA"], "basic");
        rowY += 52;
        DrawSkillRow(b, "BasicB", rowY, SkillNames["BasicB"], "basic");
        rowY += 52;
        DrawSkillRow(b, "BasicC", rowY, SkillNames["BasicC"], "basic");

        rowY += 58;
        string ohgiState = Data.OhgiAccessGranted
            ? "해방됨"
            : Data.OhgiQuestAvailable ? "해방 퀘스트 가능" : "잠김";
        DrawSectionTitle(b, $"오의  —  {ohgiState}", rowY);
        rowY += 26;
        DrawSkillRow(b, "OhgiA", rowY, SkillNames["OhgiA"], "ohgi");
        rowY += 52;
        DrawSkillRow(b, "OhgiB", rowY, SkillNames["OhgiB"], "ohgi");
        rowY += 52;
        DrawSkillRow(b, "OhgiC", rowY, SkillNames["OhgiC"], "ohgi");

        rowY += 58;
        string ultimateState = Data.UltimateAccessGranted
            ? "해방됨"
            : Data.UltimateQuestAvailable ? "해방 퀘스트 가능" : "잠김";
        DrawSectionTitle(b, $"극의  —  {ultimateState}", rowY);
        rowY += 26;
        DrawSkillRow(b, "UltimateA", rowY, SkillNames["UltimateA"], "ultimate");
        rowY += 52;
        DrawSkillRow(b, "UltimateB", rowY, SkillNames["UltimateB"], "ultimate");

        Utility.drawTextWithShadow(
            b,
            $"[{Config.OpenMenuKey}] 닫기  ·  각 단계 5P  ·  단계 완료 시 다음 단계 활성화",
            Game1.smallFont,
            new Vector2(left, yPositionOnScreen + height - 40),
            Color.DimGray
        );

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

    private void DrawSkillRow(SpriteBatch b, string id, int y, string title, string group)
    {
        var skill = Data.Skills[id];

        Rectangle row = new(
            xPositionOnScreen + 30,
            y,
            width - 60,
            46
        );

        b.Draw(Game1.staminaRect, row, Color.Black * 0.08f);

        Utility.drawTextWithShadow(
            b,
            title,
            Game1.smallFont,
            new Vector2(row.X + 12, row.Y + 12),
            Game1.textColor
        );

        string progress = $"I {skill.Stage1}/5   II {skill.Stage2}/5   III {skill.Stage3}/5";
        Utility.drawTextWithShadow(
            b,
            progress,
            Game1.smallFont,
            new Vector2(row.X + 245, row.Y + 12),
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
