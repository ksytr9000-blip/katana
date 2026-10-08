using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using SwordMastery.Models;

namespace SwordMastery.Services;

/// <summary>
/// Sword Mastery combat runtime.
/// Input-agnostic so PC hotkeys and future Android touch controls can share the same combat methods.
/// </summary>
internal sealed class CombatService
{
    private readonly Dictionary<string, int> Cooldowns = new()
    {
        ["A"] = 0,
        ["B"] = 0,
        ["C"] = 0,
        ["OHGI"] = 0
    };

    private readonly List<PendingHit> PendingHits = new();
    private readonly List<SwordWave> Projectiles = new();
    private readonly List<SlashFx> Effects = new();

    private int UltimateAuraTick;

    public CombatService(IMonitor monitor)
    {
        // Monitor kept in constructor for future combat debug logging.
    }

    public bool TryUseBasicSkill(string branch, SaveData data, out string message)
    {
        branch = branch.ToUpperInvariant();

        if (!HasSwordEquipped())
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

    public bool TryUseOhgi(SaveData data, out string message)
    {
        if (!data.OhgiAccessGranted || data.SelectedOhgi is null)
        {
            message = "선택된 오의가 없습니다.";
            return false;
        }

        string branch = data.SelectedOhgi;
        SkillProgress progress = data.Skills[$"Ohgi{branch}"];
        int stage = GetUnlockedStage(progress);

        if (stage <= 0)
        {
            message = "선택한 오의에 먼저 SP를 투자해야 합니다.";
            return false;
        }

        if (branch == "B")
        {
            message = "검기는 기본 검 공격에 자동 발동되는 패시브 오의입니다.";
            return false;
        }

        if (!HasSwordEquipped())
        {
            message = "검을 장비해야 사용할 수 있습니다.";
            return false;
        }

        if (Cooldowns["OHGI"] > 0)
        {
            message = $"오의 재사용 대기 중입니다. ({Math.Max(1, Cooldowns["OHGI"] / 6) / 10f:0.0}초)";
            return false;
        }

        bool used = branch switch
        {
            "A" => UseIssen(progress, stage),
            "C" => UseSwordPinnacle(progress, stage),
            _ => false
        };

        if (!used)
        {
            message = "오의를 사용할 수 없습니다.";
            return false;
        }

        message = "";
        return true;
    }

    public bool TryUseUltimate(SaveData data, out string message)
    {
        if (!data.UltimateAccessGranted || data.SelectedUltimate is null)
        {
            message = "선택된 극의가 없습니다.";
            return false;
        }

        string branch = data.SelectedUltimate;
        SkillProgress progress = data.Skills[$"Ultimate{branch}"];
        int stage = GetUnlockedStage(progress);

        if (stage <= 0)
        {
            message = "선택한 극의에 먼저 SP를 투자해야 합니다.";
            return false;
        }

        if (branch == "A")
        {
            message = "검술의 극은 상시 발동되는 패시브 극의입니다.";
            return false;
        }

        if (!HasSwordEquipped())
        {
            message = "검을 장비해야 사용할 수 있습니다.";
            return false;
        }

        // 보법의 극은 의도적으로 쿨다운이 없다.
        UseUltimateFootwork(progress, stage);
        message = "";
        return true;
    }

    /// <summary>
    /// Called alongside a vanilla sword swing.
    /// Ohgi B (Sword Wave) automatically launches from normal sword attacks.
    /// </summary>
    public void OnVanillaSwordAttack(SaveData data)
    {
        if (!HasSwordEquipped())
            return;

        if (!data.OhgiAccessGranted || data.SelectedOhgi != "B")
            return;

        SkillProgress progress = data.Skills["OhgiB"];
        int stage = GetUnlockedStage(progress);
        if (stage <= 0)
            return;

        SpawnPassiveSwordWave(progress, stage);
    }

    public void Update(SaveData data)
    {
        foreach (string key in Cooldowns.Keys.ToArray())
        {
            if (Cooldowns[key] > 0)
                Cooldowns[key]--;
        }

        UpdatePendingHits();
        UpdateProjectiles();
        UpdateEffects();
        UpdateUltimateAura(data);
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
                scale: new Vector2(wave.VisualLength, wave.VisualWidth),
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
                scale: new Vector2(wave.VisualLength * 0.75f, Math.Max(3f, wave.VisualWidth * 0.30f)),
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

        DashForward(player, facing, 52);

        Vector2 end = player.Position;
        Rectangle path = BuildPathRectangle(start, end, 56);

        // Damage deliberately kept low so multi-hit skills can actually display all hits.
        int total = progress.TotalPoints;

        if (stage == 2)
        {
            int damage = 2 + total / 4;
            DamageArea(location, path, damage, 0.08f);
            AddPathFx(start, end, 13, 12);
        }
        else if (stage >= 3)
        {
            int perHit = 1 + total / 5;

            QueueMultiHit(
                location,
                path,
                perHit,
                hitCount: 4,
                tickGap: 8,
                knockback: 0.02f,
                facing: facing
            );

            AddPathFx(start, end, 15, 18);
        }
        else
        {
            AddPathFx(start, end, 10, 8);
        }

        location.localSound("swordswipe");
        Cooldowns["A"] = 36;
        return true;
    }

    private bool UseSlash(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;
        int total = progress.TotalPoints;

        if (stage == 1)
        {
            Rectangle area = BuildFrontArea(player, range: 92, width: 84);
            DamageArea(location, area, 3 + total / 4, 0.18f);
            AddAreaFlash(area, facing, 12);
        }
        else if (stage == 2)
        {
            Rectangle area = BuildFrontArea(player, range: 150, width: 148);
            DamageArea(location, area, 4 + total / 4, 0.20f);
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
                Speed = 23f,
                RemainingTicks = 30,
                BaseDamage = 4 + total / 4,
                HitboxRadius = 20,
                MaxHitEvents = 1,
                DistanceFalloff = 0f,
                PierceFalloff = 0f,
                VisualLength = 72f,
                VisualWidth = 15f
            });

            AddPathFx(origin - direction * 24f, origin + direction * 54f, 13, 10);
        }

        location.localSound("swordswipe");
        Cooldowns["B"] = 30;
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

        int perHitDamage = 1 + progress.TotalPoints / 5;

        QueueMultiHit(
            location,
            area,
            perHitDamage,
            hitCount,
            tickGap: 8,
            knockback: 0.02f,
            facing: facing
        );

        AddAreaFlash(area, facing, 14);
        location.localSound("swordswipe");
        Cooldowns["C"] = 54;
        return true;
    }

    // -------------------------
    // Ohgi
    // -------------------------

    private bool UseIssen(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;
        Vector2 start = player.Position;

        DashForward(player, facing, 86);

        Vector2 end = player.Position;

        int width = stage switch
        {
            1 => 64,
            2 => 104,
            _ => 152
        };

        Rectangle path = BuildPathRectangle(start, end, width);
        int damage = 6 + progress.TotalPoints / 3;

        DamageArea(location, path, damage, 0.12f);
        AddPathFx(start, end, 22 + stage * 4, 18);

        location.localSound("swordswipe");
        Cooldowns["OHGI"] = 90;
        return true;
    }

    private void SpawnPassiveSwordWave(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        Vector2 direction = DirectionVector(player.FacingDirection);
        Vector2 origin = CenterOf(player.GetBoundingBox()) + direction * 44f;

        int total = progress.TotalPoints;

        int ticks;
        int radius;
        int maxHits;
        float visualLength;
        float visualWidth;

        if (stage == 1)
        {
            ticks = 32;
            radius = 17;
            maxHits = 1;
            visualLength = 62f;
            visualWidth = 12f;
        }
        else if (stage == 2)
        {
            ticks = 52;
            radius = 27;
            maxHits = 2;
            visualLength = 82f;
            visualWidth = 18f;
        }
        else
        {
            // Effectively map-wide for normal Stardew maps.
            ticks = 240;
            radius = 40;
            maxHits = 999;
            visualLength = 108f;
            visualWidth = 27f;
        }

        Projectiles.Add(new SwordWave
        {
            Location = Game1.currentLocation,
            Position = origin,
            Direction = direction,
            Speed = 24f,
            RemainingTicks = ticks,
            BaseDamage = 4 + total / 4,
            HitboxRadius = radius,
            MaxHitEvents = maxHits,
            DistanceFalloff = 0.035f,
            PierceFalloff = stage == 1 ? 0f : 0.14f,
            VisualLength = visualLength,
            VisualWidth = visualWidth
        });

        AddPathFx(origin - direction * 18f, origin + direction * 52f, 11 + stage * 2, 9);
    }

    private bool UseSwordPinnacle(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        Rectangle box = player.GetBoundingBox();

        int range = stage switch
        {
            1 => 110,
            2 => 155,
            _ => 205
        };

        int hitCount = stage switch
        {
            1 => 3,
            2 => 6,
            _ => 10
        };

        Rectangle area = new(
            box.Center.X - range,
            box.Center.Y - range,
            range * 2,
            range * 2
        );

        int perHit = 1 + progress.TotalPoints / 5;

        QueueMultiHit(
            location,
            area,
            perHit,
            hitCount,
            tickGap: 6,
            knockback: 0.01f,
            facing: player.FacingDirection
        );

        AddRadialFx(area, 20);
        location.localSound("swordswipe");
        Cooldowns["OHGI"] = 105;
        return true;
    }

    // -------------------------
    // Ultimate
    // -------------------------

    private void UpdateUltimateAura(SaveData data)
    {
        if (!data.UltimateAccessGranted || data.SelectedUltimate != "A")
        {
            UltimateAuraTick = 0;
            return;
        }

        SkillProgress progress = data.Skills["UltimateA"];
        int stage = GetUnlockedStage(progress);
        if (stage <= 0)
        {
            UltimateAuraTick = 0;
            return;
        }

        if (UltimateAuraTick > 0)
        {
            UltimateAuraTick--;
            return;
        }

        Farmer player = Game1.player;
        Rectangle box = player.GetBoundingBox();

        int range = stage switch
        {
            1 => 78,
            2 => 126,
            _ => 178
        };

        Rectangle area = new(
            box.Center.X - range,
            box.Center.Y - range,
            range * 2,
            range * 2
        );

        int damage = 1 + progress.TotalPoints / 5 + (stage - 1);

        DamageArea(Game1.currentLocation, area, damage, 0f);
        AddRadialFx(area, 11);

        // Always active, but not every single frame.
        UltimateAuraTick = 45;
    }

    private bool UseUltimateFootwork(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;

        Vector2 start = player.Position;

        int moveSteps = stage switch
        {
            1 => 22,
            2 => 40,
            _ => 62
        };

        DashForward(player, facing, moveSteps);

        Vector2 end = player.Position;
        int thickness = stage switch
        {
            1 => 62,
            2 => 88,
            _ => 118
        };

        Rectangle path = BuildPathRectangle(start, end, thickness);
        int damage = 2 + progress.TotalPoints / 4 + (stage - 1);

        DamageArea(location, path, damage, 0.02f);
        AddPathFx(start, end, 15 + stage * 4, 11);

        // No cooldown by design.
        location.localSound("swordswipe");
        return true;
    }

    // -------------------------
    // Runtime
    // -------------------------

    private void UpdatePendingHits()
    {
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
            AddAreaFlash(hit.Area, hit.FacingDirection, 8);
            PendingHits.RemoveAt(i);
        }
    }

    private void UpdateProjectiles()
    {
        for (int i = Projectiles.Count - 1; i >= 0; i--)
        {
            SwordWave wave = Projectiles[i];

            if (Game1.currentLocation != wave.Location)
            {
                Projectiles.RemoveAt(i);
                continue;
            }

            wave.Position += wave.Direction * wave.Speed;
            wave.DistanceTravelled += wave.Speed;
            wave.RemainingTicks--;

            if (wave.HitSkipTicks > 0)
            {
                wave.HitSkipTicks--;
                if (wave.RemainingTicks <= 0)
                    Projectiles.RemoveAt(i);
                continue;
            }

            Rectangle hitbox = new(
                (int)wave.Position.X - wave.HitboxRadius,
                (int)wave.Position.Y - wave.HitboxRadius,
                wave.HitboxRadius * 2,
                wave.HitboxRadius * 2
            );

            float distanceTiles = wave.DistanceTravelled / 64f;
            float multiplier = 1f
                - distanceTiles * wave.DistanceFalloff
                - wave.HitEvents * wave.PierceFalloff;

            multiplier = Math.Clamp(multiplier, 0.30f, 1f);
            int damage = Math.Max(1, (int)Math.Round(wave.BaseDamage * multiplier));

            bool hit = wave.Location.damageMonster(
                hitbox,
                damage,
                damage,
                isBomb: false,
                knockBackModifier: 0.02f,
                addedPrecision: 0,
                critChance: 0f,
                critMultiplier: 1f,
                triggerMonsterInvincibleTimer: false,
                who: Game1.player
            );

            if (hit)
            {
                wave.HitEvents++;

                if (wave.HitEvents >= wave.MaxHitEvents)
                {
                    Projectiles.RemoveAt(i);
                    continue;
                }

                // Push the wave forward so a piercing projectile doesn't repeatedly
                // count the same monster on adjacent update ticks.
                wave.Position += wave.Direction * 54f;
                wave.DistanceTravelled += 54f;
                wave.HitSkipTicks = 2;
            }

            if (wave.RemainingTicks <= 0)
                Projectiles.RemoveAt(i);
        }
    }

    private void UpdateEffects()
    {
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            Effects[i].Ticks--;
            if (Effects[i].Ticks <= 0)
                Effects.RemoveAt(i);
        }
    }

    private void QueueMultiHit(
        GameLocation location,
        Rectangle area,
        int damage,
        int hitCount,
        int tickGap,
        float knockback,
        int facing
    )
    {
        // First hit happens immediately; remaining hits are deliberately spaced out
        // so damage numbers and hit reactions are visible as separate strikes.
        DamageArea(location, area, damage, knockback);

        for (int i = 1; i < hitCount; i++)
        {
            PendingHits.Add(new PendingHit
            {
                Location = location,
                Area = area,
                Damage = damage,
                Knockback = knockback,
                Ticks = i * tickGap,
                FacingDirection = facing
            });
        }
    }

    private static bool HasSwordEquipped()
    {
        return Game1.player.CurrentTool is MeleeWeapon;
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

    private static void DashForward(Farmer player, int facing, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            Vector2 before = player.Position;
            player.tryToMoveInDirection(facing, isFarmer: true, damagesFarmer: 0, glider: false);

            if (player.Position == before)
                break;
        }
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

        return new Rectangle(
            left,
            top,
            Math.Max(thickness, width),
            Math.Max(thickness, height)
        );
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
            Width = 12f,
            Ticks = ticks,
            MaxTicks = ticks
        });
    }

    private void AddRadialFx(Rectangle area, int ticks)
    {
        Vector2 c = new(area.Center.X, area.Center.Y);
        float r = Math.Min(area.Width, area.Height) * 0.42f;

        Effects.Add(new SlashFx
        {
            Start = c + new Vector2(-r, 0f),
            End = c + new Vector2(r, 0f),
            Width = 13f,
            Ticks = ticks,
            MaxTicks = ticks
        });

        Effects.Add(new SlashFx
        {
            Start = c + new Vector2(0f, -r),
            End = c + new Vector2(0f, r),
            Width = 13f,
            Ticks = ticks,
            MaxTicks = ticks
        });

        Effects.Add(new SlashFx
        {
            Start = c + new Vector2(-r * 0.75f, -r * 0.75f),
            End = c + new Vector2(r * 0.75f, r * 0.75f),
            Width = 10f,
            Ticks = ticks,
            MaxTicks = ticks
        });

        Effects.Add(new SlashFx
        {
            Start = c + new Vector2(r * 0.75f, -r * 0.75f),
            End = c + new Vector2(-r * 0.75f, r * 0.75f),
            Width = 10f,
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

        public int BaseDamage { get; set; }
        public int HitboxRadius { get; set; }

        public int HitEvents { get; set; }
        public int MaxHitEvents { get; set; } = 1;
        public int HitSkipTicks { get; set; }

        public float DistanceTravelled { get; set; }
        public float DistanceFalloff { get; set; }
        public float PierceFalloff { get; set; }

        public float VisualLength { get; set; } = 72f;
        public float VisualWidth { get; set; } = 16f;
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
