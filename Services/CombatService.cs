using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SwordMastery.Models;

namespace SwordMastery.Services;

/// <summary>
/// Runtime combat implementation for the three basic sword skills.
/// Kept input-agnostic so PC hotkeys and future Android touch controls can call the same methods.
/// </summary>
internal sealed class CombatService
{
    private readonly IMonitor Monitor;

    private readonly Dictionary<string, int> Cooldowns = new()
    {
        ["A"] = 0,
        ["B"] = 0,
        ["C"] = 0
    };

    private readonly List<PendingHit> PendingHits = new();
    private readonly List<SwordWave> Projectiles = new();
    private readonly List<SlashFx> Effects = new();

    public CombatService(IMonitor monitor)
    {
        Monitor = monitor;
    }

    public bool TryUseBasicSkill(string branch, SaveData data, out string message)
    {
        branch = branch.ToUpperInvariant();

        if (Game1.player.CurrentTool is not MeleeWeapon)
        {
            message = "검을 장비해야 사용할 수 있습니다.";
            return false;
        }

        string id = $"Basic{branch}";
        if (!data.Skills.TryGetValue(id, out SkillProgress? progress))
        {
            message = "알 수 없는 기초검술입니다.";
            return false;
        }

        int stage = GetUnlockedStage(progress);
        if (stage <= 0)
        {
            message = "이 기술에 먼저 SP를 투자해야 합니다.";
            return false;
        }

        if (Cooldowns.TryGetValue(branch, out int cooldown) && cooldown > 0)
        {
            message = $"재사용 대기 중입니다. ({Math.Max(1, cooldown / 6) / 10f:0.0}초)";
            return false;
        }

        bool used = branch switch
        {
            "A" => UseSwordsmanStep(progress, stage),
            "B" => UseSlash(progress, stage),
            "C" => UseCalicoStyle(progress, stage),
            _ => false
        };

        if (!used)
        {
            message = "기술을 사용할 수 없습니다.";
            return false;
        }

        message = "";
        return true;
    }

    public void Update()
    {
        foreach (string key in Cooldowns.Keys.ToArray())
        {
            if (Cooldowns[key] > 0)
                Cooldowns[key]--;
        }

        for (int i = PendingHits.Count - 1; i >= 0; i--)
        {
            PendingHit hit = PendingHits[i];

            if (Game1.currentLocation != hit.Location)
            {
                PendingHits.RemoveAt(i);
                continue;
            }

            hit.Ticks--;
            if (hit.Ticks > 0)
                continue;

            DamageArea(hit.Location, hit.Area, hit.Damage, hit.Knockback);
            AddAreaFlash(hit.Area, hit.FacingDirection, 9);
            PendingHits.RemoveAt(i);
        }

        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            SwordWave wave = Projectiles[i];

            if (Game1.currentLocation != wave.Location)
            {
                Projectiles.RemoveAt(i);
                continue;
            }

            wave.Position += wave.Direction * wave.Speed;
            wave.RemainingTicks--;

            Rectangle hitbox = new(
                (int)wave.Position.X - wave.HitboxRadius,
                (int)wave.Position.Y - wave.HitboxRadius,
                wave.HitboxRadius * 2,
                wave.HitboxRadius * 2
            );

            bool hit = wave.Location.damageMonster(
                hitbox,
                wave.Damage,
                wave.Damage,
                isBomb: false,
                knockBackModifier: 0.2f,
                addedPrecision: 0,
                critChance: 0f,
                critMultiplier: 1f,
                triggerMonsterInvincibleTimer: false,
                who: Game1.player
            );

            if (hit || wave.RemainingTicks <= 0)
                Projectiles.RemoveAt(i);
        }

        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            Effects[i].Ticks--;
            if (Effects[i].Ticks <= 0)
                Effects.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch b)
    {
        foreach (SlashFx fx in Effects)
        {
            float alpha = Math.Clamp(fx.Ticks / (float)fx.MaxTicks, 0f, 1f);
            Vector2 screenStart = WorldToScreen(fx.Start);
            Vector2 screenEnd = WorldToScreen(fx.End);

            DrawLine(
                b,
                screenStart,
                screenEnd,
                fx.Width,
                new Color(90, 220, 255) * alpha
            );

            DrawLine(
                b,
                screenStart,
                screenEnd,
                Math.Max(2f, fx.Width * 0.35f),
                Color.White * Math.Min(1f, alpha + 0.15f)
            );
        }

        foreach (SwordWave wave in Projectiles)
        {
            Vector2 center = WorldToScreen(wave.Position);
            float rotation = (float)Math.Atan2(wave.Direction.Y, wave.Direction.X);

            b.Draw(
                Game1.staminaRect,
                center,
                sourceRectangle: null,
                color: new Color(60, 195, 255) * 0.9f,
                rotation: rotation,
                origin: new Vector2(0.5f, 0.5f),
                scale: new Vector2(72f, 16f),
                effects: SpriteEffects.None,
                layerDepth: 1f
            );

            b.Draw(
                Game1.staminaRect,
                center,
                sourceRectangle: null,
                color: Color.White * 0.9f,
                rotation: rotation,
                origin: new Vector2(0.5f, 0.5f),
                scale: new Vector2(54f, 5f),
                effects: SpriteEffects.None,
                layerDepth: 1f
            );
        }
    }

    private bool UseSwordsmanStep(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;

        Vector2 start = player.Position;
        int facing = player.FacingDirection;

        // Use vanilla movement collision logic repeatedly, so the dash stops at walls/objects.
        // This is intentionally done as a burst so the core logic remains Android-safe.
        for (int i = 0; i < 72; i++)
        {
            Vector2 before = player.Position;
            player.tryToMoveInDirection(facing, isFarmer: true, damagesFarmer: 0, glider: false);

            if (player.Position == before)
                break;
        }

        Vector2 end = player.Position;
        Rectangle path = BuildPathRectangle(start, end, 58);

        int damage = 8 + progress.TotalPoints * 2;

        if (stage == 2)
        {
            DamageArea(location, path, damage, 0.15f);
            AddPathFx(start, end, 14, 12);
        }
        else if (stage >= 3)
        {
            int perHit = Math.Max(1, (int)Math.Round(damage * 0.60f));

            // Four slashes along the dash route.
            DamageArea(location, path, perHit, 0.05f);
            AddPathFx(start, end, 16, 10);

            for (int i = 1; i < 4; i++)
            {
                PendingHits.Add(new PendingHit
                {
                    Location = location,
                    Area = path,
                    Damage = perHit,
                    Knockback = 0.05f,
                    Ticks = i * 4,
                    FacingDirection = facing
                });
            }
        }
        else
        {
            AddPathFx(start, end, 10, 8);
        }

        location.localSound("swordswipe");
        Cooldowns["A"] = 36; // ~0.6 sec
        return true;
    }

    private bool UseSlash(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;

        int damage = 10 + progress.TotalPoints * 2;

        if (stage == 1)
        {
            Rectangle area = BuildFrontArea(player, range: 92, width: 84);
            DamageArea(location, area, damage, 0.35f);
            AddAreaFlash(area, facing, 12);
        }
        else if (stage == 2)
        {
            Rectangle area = BuildFrontArea(player, range: 150, width: 148);
            DamageArea(location, area, damage + 4, 0.40f);
            AddAreaFlash(area, facing, 14);
        }
        else
        {
            Vector2 direction = DirectionVector(facing);
            Vector2 origin = CenterOf(player.GetBoundingBox()) + direction * 42f;

            Projectiles.Add(new SwordWave
            {
                Location = location,
                Position = origin,
                Direction = direction,
                Speed = 22f,
                RemainingTicks = 28,
                Damage = damage + 8,
                HitboxRadius = 24
            });

            AddPathFx(origin - direction * 24f, origin + direction * 54f, 13, 10);
        }

        location.localSound("swordswipe");
        Cooldowns["B"] = 30; // ~0.5 sec
        return true;
    }

    private bool UseCalicoStyle(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;

        int hitCount;
        Rectangle area;

        if (stage == 1)
        {
            hitCount = 3;
            area = BuildFrontArea(player, range: 105, width: 100);
        }
        else if (stage == 2)
        {
            hitCount = 4;
            area = BuildFrontArea(player, range: 140, width: 220);
        }
        else
        {
            hitCount = 6;
            area = BuildFrontArea(player, range: 168, width: 252);
        }

        int perHitDamage = 6 + progress.TotalPoints;

        DamageArea(location, area, perHitDamage, 0.08f);
        AddAreaFlash(area, facing, 9);

        for (int i = 1; i < hitCount; i++)
        {
            PendingHits.Add(new PendingHit
            {
                Location = location,
                Area = area,
                Damage = perHitDamage,
                Knockback = 0.08f,
                Ticks = i * 5,
                FacingDirection = facing
            });
        }

        location.localSound("swordswipe");
        Cooldowns["C"] = 54; // ~0.9 sec
        return true;
    }

    private static int GetUnlockedStage(SkillProgress progress)
    {
        if (progress.Stage3 > 0)
            return 3;

        if (progress.Stage2 > 0)
            return 2;

        if (progress.Stage1 > 0)
            return 1;

        return 0;
    }

    private static Rectangle BuildFrontArea(Farmer player, int range, int width)
    {
        Rectangle box = player.GetBoundingBox();
        int cx = box.Center.X;
        int cy = box.Center.Y;

        return player.FacingDirection switch
        {
            0 => new Rectangle(cx - width / 2, box.Top - range, width, range + box.Height / 3),
            1 => new Rectangle(box.Right - box.Width / 3, cy - width / 2, range + box.Width / 3, width),
            2 => new Rectangle(cx - width / 2, box.Bottom - box.Height / 3, width, range + box.Height / 3),
            3 => new Rectangle(box.Left - range, cy - width / 2, range + box.Width / 3, width),
            _ => box
        };
    }

    private static Rectangle BuildPathRectangle(Vector2 start, Vector2 end, int thickness)
    {
        float sx = start.X + 32f;
        float sy = start.Y + 32f;
        float ex = end.X + 32f;
        float ey = end.Y + 32f;

        int left = (int)Math.Min(sx, ex) - thickness / 2;
        int top = (int)Math.Min(sy, ey) - thickness / 2;
        int width = (int)Math.Abs(ex - sx) + thickness;
        int height = (int)Math.Abs(ey - sy) + thickness;

        return new Rectangle(left, top, Math.Max(thickness, width), Math.Max(thickness, height));
    }

    private static void DamageArea(GameLocation location, Rectangle area, int damage, float knockback)
    {
        location.damageMonster(
            area,
            damage,
            damage,
            isBomb: false,
            knockBackModifier: knockback,
            addedPrecision: 0,
            critChance: 0f,
            critMultiplier: 1f,
            triggerMonsterInvincibleTimer: false,
            who: Game1.player
        );
    }

    private void AddPathFx(Vector2 start, Vector2 end, float width, int ticks)
    {
        Effects.Add(new SlashFx
        {
            Start = start + new Vector2(32f, 32f),
            End = end + new Vector2(32f, 32f),
            Width = width,
            Ticks = ticks,
            MaxTicks = ticks
        });
    }

    private void AddAreaFlash(Rectangle area, int facingDirection, int ticks)
    {
        Vector2 center = new(area.Center.X, area.Center.Y);
        Vector2 dir = DirectionVector(facingDirection);
        Vector2 perpendicular = new(-dir.Y, dir.X);

        float half = Math.Max(area.Width, area.Height) * 0.45f;

        Effects.Add(new SlashFx
        {
            Start = center - perpendicular * half,
            End = center + perpendicular * half,
            Width = 13f,
            Ticks = ticks,
            MaxTicks = ticks
        });
    }

    private static Vector2 DirectionVector(int facingDirection)
    {
        return facingDirection switch
        {
            0 => new Vector2(0f, -1f),
            1 => new Vector2(1f, 0f),
            2 => new Vector2(0f, 1f),
            3 => new Vector2(-1f, 0f),
            _ => Vector2.Zero
        };
    }

    private static Vector2 CenterOf(Rectangle rectangle)
    {
        return new Vector2(rectangle.Center.X, rectangle.Center.Y);
    }

    private static Vector2 WorldToScreen(Vector2 world)
    {
        return world - new Vector2(Game1.viewport.X, Game1.viewport.Y);
    }

    private static void DrawLine(SpriteBatch b, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();

        if (length <= 0.01f)
            return;

        float rotation = (float)Math.Atan2(delta.Y, delta.X);

        b.Draw(
            Game1.staminaRect,
            start,
            sourceRectangle: null,
            color: color,
            rotation: rotation,
            origin: new Vector2(0f, 0.5f),
            scale: new Vector2(length, width),
            effects: SpriteEffects.None,
            layerDepth: 1f
        );
    }

    private sealed class PendingHit
    {
        public GameLocation Location { get; set; } = null!;
        public Rectangle Area { get; set; }
        public int Damage { get; set; }
        public float Knockback { get; set; }
        public int Ticks { get; set; }
        public int FacingDirection { get; set; }
    }

    private sealed class SwordWave
    {
        public GameLocation Location { get; set; } = null!;
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; set; }
        public float Speed { get; set; }
        public int RemainingTicks { get; set; }
        public int Damage { get; set; }
        public int HitboxRadius { get; set; }
    }

    private sealed class SlashFx
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        public float Width { get; set; }
        public int Ticks { get; set; }
        public int MaxTicks { get; set; }
    }
}
