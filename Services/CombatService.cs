using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using SwordMastery.Models;

namespace SwordMastery.Services;

internal readonly record struct CooldownStatus(
    string SkillId,
    int RemainingTicks,
    int MaxTicks,
    bool Available
);

/// <summary>
/// Sword Mastery combat runtime.
/// PC hotkeys and future Android touch buttons call the same methods.
/// </summary>
internal sealed class CombatService
{
    private const int CooldownA = 420;       // 7.0 sec
    private const int CooldownB = 180;       // 3.0 sec
    private const int CooldownC = 300;       // 5.0 sec
    private const int CooldownIssen = 720;   // 12.0 sec
    private const int CooldownPinnacle = 570; // 9.5 sec

    private readonly Dictionary<string, int> Cooldowns = new()
    {
        ["A"] = 0,
        ["B"] = 0,
        ["C"] = 0,
        ["OHGI"] = 0
    };

    private readonly List<PendingHit> PendingHits = new();
    private readonly List<SwordWave> Projectiles = new();
    private readonly List<ArcFx> ArcEffects = new();

    private int UltimateAuraTick;

    public CombatService(IMonitor monitor)
    {
        // Reserved for later combat debug logging.
    }

    public IReadOnlyList<CooldownStatus> GetCooldownStatuses(SaveData data)
    {
        List<CooldownStatus> result = new()
        {
            new("BasicA", Cooldowns["A"], CooldownA, data.Skills["BasicA"].TotalPoints > 0),
            new("BasicB", Cooldowns["B"], CooldownB, data.Skills["BasicB"].TotalPoints > 0),
            new("BasicC", Cooldowns["C"], CooldownC, data.Skills["BasicC"].TotalPoints > 0)
        };

        if (data.OhgiAccessGranted && data.SelectedOhgi == "A")
        {
            result.Add(new(
                "OhgiA",
                Cooldowns["OHGI"],
                CooldownIssen,
                data.Skills["OhgiA"].TotalPoints > 0
            ));
        }
        else if (data.OhgiAccessGranted && data.SelectedOhgi == "C")
        {
            result.Add(new(
                "OhgiC",
                Cooldowns["OHGI"],
                CooldownPinnacle,
                data.Skills["OhgiC"].TotalPoints > 0
            ));
        }

        return result;
    }

    public bool TryUseBasicSkill(string branch, SaveData data, out string message)
    {
        branch = branch.ToUpperInvariant();

        if (!TryGetEquippedSword(out _))
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
            message = $"재사용 대기 {cooldown / 60f:0.0}초";
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

        if (!TryGetEquippedSword(out _))
        {
            message = "검을 장비해야 사용할 수 있습니다.";
            return false;
        }

        if (Cooldowns["OHGI"] > 0)
        {
            message = $"오의 재사용 대기 {Cooldowns["OHGI"] / 60f:0.0}초";
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

        if (!TryGetEquippedSword(out _))
        {
            message = "검을 장비해야 사용할 수 있습니다.";
            return false;
        }

        // 보법의 극은 컨셉대로 쿨다운 없음.
        UseUltimateFootwork(progress, stage);
        message = "";
        return true;
    }

    /// <summary>
    /// Ohgi B "검기": vanilla sword swing launches a crescent sword wave.
    /// </summary>
    public void OnVanillaSwordAttack(SaveData data)
    {
        if (!TryGetEquippedSword(out _))
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
        foreach (ArcFx fx in ArcEffects)
        {
            float alpha = Math.Clamp(fx.Ticks / (float)fx.MaxTicks, 0f, 1f);
            Vector2 center = WorldToScreen(fx.Center);

            DrawArc(
                b,
                center,
                fx.Radius,
                fx.CenterAngle,
                fx.Sweep,
                fx.Width,
                new Color(55, 185, 255) * alpha,
                segments: 12
            );

            DrawArc(
                b,
                center,
                fx.Radius - Math.Max(2f, fx.Width * 0.22f),
                fx.CenterAngle,
                fx.Sweep,
                Math.Max(2f, fx.Width * 0.34f),
                Color.White * Math.Min(1f, alpha + 0.15f),
                segments: 12
            );
        }

        foreach (SwordWave wave in Projectiles)
        {
            Vector2 center = WorldToScreen(wave.Position);
            float directionAngle = (float)Math.Atan2(wave.Direction.Y, wave.Direction.X);

            // Curved crescent, not a laser/bar.
            DrawArc(
                b,
                center,
                wave.VisualRadius,
                directionAngle + MathF.PI,
                wave.VisualSweep,
                wave.VisualWidth,
                new Color(55, 195, 255) * 0.92f,
                segments: 14
            );

            DrawArc(
                b,
                center,
                wave.VisualRadius - wave.VisualWidth * 0.28f,
                directionAngle + MathF.PI,
                wave.VisualSweep,
                Math.Max(2.5f, wave.VisualWidth * 0.33f),
                Color.White * 0.95f,
                segments: 14
            );
        }
    }

    // ---------------------------------------------------------------------
    // Basic skills
    // ---------------------------------------------------------------------

    private bool UseSwordsmanStep(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;

        AnimateSwordSkill(player, 28f);

        Vector2 start = player.Position;

        // Max movement distance by stage:
        // 1 = 2 tiles, 2 = 3 tiles, 3 = 4 tiles.
        float maxDistancePixels = stage switch
        {
            1 => 2f * 64f,
            2 => 3f * 64f,
            _ => 4f * 64f
        };

        DashForwardPixels(player, facing, maxDistancePixels);
        Vector2 end = player.Position;

        Rectangle path = BuildPathRectangle(start, end, 64);
        int weaponDamage = GetWeaponReferenceDamage();

        if (stage == 2)
        {
            // Single-hit baseline: 130% of equipped sword average damage.
            int damage = ScaleDamage(
                weaponDamage,
                SingleHitMultiplier(progress)
            );

            DamageArea(location, path, damage, 0.06f);
            AddDashSlashFx(start, end, facing, 2);
        }
        else if (stage >= 3)
        {
            // Multi-hit baseline: 75% PER HIT, with visible SP scaling.
            int perHit = ScaleDamage(
                weaponDamage,
                MultiHitMultiplier(progress)
            );

            QueueMultiHit(
                location,
                path,
                perHit,
                hitCount: 4,
                tickGap: 8,
                knockback: 0.01f,
                facing: facing,
                effectCenter: new Vector2(path.Center.X, path.Center.Y),
                effectRadius: 58f
            );

            AddDashSlashFx(start, end, facing, 4);
        }
        else
        {
            AddDashSlashFx(start, end, facing, 1);
        }

        location.localSound("swordswipe");
        Cooldowns["A"] = CooldownA;
        return true;
    }

    private bool UseSlash(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;
        int weaponDamage = GetWeaponReferenceDamage();

        AnimateSwordSkill(player, 36f);

        if (stage == 1)
        {
            Rectangle area = BuildFrontArea(player, range: 92, width: 92);
            int damage = ScaleDamage(
                weaponDamage,
                SingleHitMultiplier(progress)
            );

            DamageArea(location, area, damage, 0.12f);
            AddFrontSlashFx(area, facing, 0, 60f, 15);
        }
        else if (stage == 2)
        {
            Rectangle area = BuildFrontArea(player, range: 150, width: 170);
            int damage = ScaleDamage(
                weaponDamage,
                SingleHitMultiplier(progress, 0.15f)
            );

            DamageArea(location, area, damage, 0.12f);
            AddFrontSlashFx(area, facing, 1, 88f, 16);
        }
        else
        {
            Vector2 direction = DirectionVector(facing);
            Vector2 origin = CenterOf(player.GetBoundingBox()) + direction * 50f;

            Projectiles.Add(new SwordWave
            {
                Location = location,
                Position = origin,
                Direction = direction,
                Speed = 21f,
                RemainingTicks = 34,
                BaseDamage = ScaleDamage(
                    weaponDamage,
                    SingleHitMultiplier(progress, 0.25f)
                ),
                HitboxRadius = 25,
                MaxHitEvents = 1,
                DistanceFalloff = 0f,
                PierceFalloff = 0f,
                VisualRadius = 48f,
                VisualWidth = 13f,
                VisualSweep = 2.45f
            });

            AddFrontSlashFx(
                new Rectangle((int)origin.X - 55, (int)origin.Y - 55, 110, 110),
                facing,
                0,
                52f,
                11
            );
        }

        location.localSound("swordswipe");
        Cooldowns["B"] = CooldownB;
        return true;
    }

    private bool UseCalicoStyle(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;
        int weaponDamage = GetWeaponReferenceDamage();

        AnimateSwordSkill(player, 27f);

        int hitCount;
        Rectangle area;
        float stageBonus;

        if (stage == 1)
        {
            hitCount = 3;
            area = BuildFrontArea(player, range: 108, width: 112);
            stageBonus = 0f;
        }
        else if (stage == 2)
        {
            hitCount = 4;
            area = BuildFrontArea(player, range: 145, width: 226);
            stageBonus = 0.05f;
        }
        else
        {
            hitCount = 6;
            area = BuildFrontArea(player, range: 174, width: 268);
            stageBonus = 0.10f;
        }

        int perHitDamage = ScaleDamage(
            weaponDamage,
            MultiHitMultiplier(progress, stageBonus)
        );

        QueueMultiHit(
            location,
            area,
            perHitDamage,
            hitCount,
            tickGap: 8,
            knockback: 0.01f,
            facing: facing,
            effectCenter: new Vector2(area.Center.X, area.Center.Y),
            effectRadius: stage == 1 ? 58f : stage == 2 ? 82f : 102f
        );

        location.localSound("swordswipe");
        Cooldowns["C"] = CooldownC;
        return true;
    }

    // ---------------------------------------------------------------------
    // Ohgi
    // ---------------------------------------------------------------------

    private bool UseIssen(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;
        int weaponDamage = GetWeaponReferenceDamage();

        AnimateSwordSkill(player, 22f);

        Vector2 start = player.Position;

        // Issen max movement range: 4 / 6 / 8 tiles.
        float maxDistancePixels = stage switch
        {
            1 => 4f * 64f,
            2 => 6f * 64f,
            _ => 8f * 64f
        };

        DashForwardPixels(player, facing, maxDistancePixels);
        Vector2 end = player.Position;

        // User-requested path width:
        // stage 1 = 1 tile, stage 2 = 5 tiles, stage 3 = 10 tiles.
        int width = stage switch
        {
            1 => 1 * 64,
            2 => 5 * 64,
            _ => 10 * 64
        };

        Rectangle path = BuildPathRectangle(start, end, width);

        float multiplier = stage switch
        {
            1 => 2.15f,
            2 => 2.55f,
            _ => 3.00f
        };

        // Each invested point visibly increases the Ohgi's damage.
        multiplier += progress.TotalPoints * 0.025f;

        int damage = ScaleDamage(weaponDamage, multiplier);
        DamageArea(location, path, damage, 0.08f);

        AddIssenFx(start, end, facing, width, stage);
        location.localSound("swordswipe");
        Cooldowns["OHGI"] = CooldownIssen;
        return true;
    }

    private void SpawnPassiveSwordWave(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        Vector2 direction = DirectionVector(player.FacingDirection);
        Vector2 origin = CenterOf(player.GetBoundingBox()) + direction * 50f;

        int weaponDamage = GetWeaponReferenceDamage();

        int ticks;
        int radius;
        int maxHits;
        float visualRadius;
        float visualWidth;
        float damageMultiplier;

        if (stage == 1)
        {
            ticks = 32;
            radius = 19;
            maxHits = 1;
            visualRadius = 42f;
            visualWidth = 11f;
            damageMultiplier = 1.30f + progress.TotalPoints * 0.02f;
        }
        else if (stage == 2)
        {
            ticks = 58;
            radius = 29;
            maxHits = 2;
            visualRadius = 55f;
            visualWidth = 15f;
            damageMultiplier = 1.45f + progress.TotalPoints * 0.02f;
        }
        else
        {
            ticks = 250;
            radius = 43;
            maxHits = 999;
            visualRadius = 72f;
            visualWidth = 20f;
            damageMultiplier = 1.60f + progress.TotalPoints * 0.02f;
        }

        Projectiles.Add(new SwordWave
        {
            Location = Game1.currentLocation,
            Position = origin,
            Direction = direction,
            Speed = 22f,
            RemainingTicks = ticks,
            BaseDamage = ScaleDamage(weaponDamage, damageMultiplier),
            HitboxRadius = radius,
            MaxHitEvents = maxHits,
            DistanceFalloff = 0.035f,
            PierceFalloff = stage == 1 ? 0f : 0.14f,
            VisualRadius = visualRadius,
            VisualWidth = visualWidth,
            VisualSweep = 2.55f
        });

        AddFrontSlashFx(
            new Rectangle((int)origin.X - 52, (int)origin.Y - 52, 104, 104),
            player.FacingDirection,
            0,
            50f,
            10
        );
    }

    private bool UseSwordPinnacle(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        Rectangle box = player.GetBoundingBox();

        int range = stage switch
        {
            1 => 115,
            2 => 165,
            _ => 220
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

        int weaponDamage = GetWeaponReferenceDamage();
        float stageBonus = stage switch
        {
            1 => 0f,
            2 => 0.05f,
            _ => 0.10f
        };

        int perHit = ScaleDamage(
            weaponDamage,
            MultiHitMultiplier(progress, stageBonus)
        );

        AnimateSwordSkill(player, 23f);

        QueueMultiHit(
            location,
            area,
            perHit,
            hitCount,
            tickGap: 6,
            knockback: 0.005f,
            facing: player.FacingDirection,
            effectCenter: new Vector2(area.Center.X, area.Center.Y),
            effectRadius: stage == 1 ? 80f : stage == 2 ? 120f : 160f
        );

        location.localSound("swordswipe");
        Cooldowns["OHGI"] = CooldownPinnacle;
        return true;
    }

    // ---------------------------------------------------------------------
    // Ultimate
    // ---------------------------------------------------------------------

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
        Vector2 playerCenter = CenterOf(player.GetBoundingBox());

        // User-requested range:
        // stage 1 ≈ previous stage-3 (~3 tiles), stage 2 = 5 tiles, stage 3 = 7.5 tiles.
        float radiusPixels = stage switch
        {
            1 => 205f,
            2 => 320f,
            _ => 480f
        };

        int weaponDamage = GetWeaponReferenceDamage();
        float damageMultiplier = stage switch
        {
            1 => 0.75f,
            2 => 0.90f,
            _ => 1.05f
        };

        damageMultiplier += progress.TotalPoints * 0.015f;
        int damage = ScaleDamage(weaponDamage, damageMultiplier);

        int fxIndex = 0;

        foreach (NPC npc in Game1.currentLocation.characters)
        {
            if (npc is not Monster monster || monster.Health <= 0)
                continue;

            Rectangle monsterBox = monster.GetBoundingBox();
            Vector2 monsterCenter = CenterOf(monsterBox);

            if (Vector2.Distance(playerCenter, monsterCenter) > radiusPixels)
                continue;

            // Damage is applied at the monster, and the slash effect appears ON the monster.
            Game1.currentLocation.damageMonster(
                monsterBox,
                damage,
                damage,
                isBomb: false,
                knockBackModifier: 0f,
                addedPrecision: 0,
                critChance: 0f,
                critMultiplier: 1f,
                triggerMonsterInvincibleTimer: false,
                who: Game1.player
            );

            AddArcFx(
                monsterCenter,
                radius: Math.Max(34f, Math.Min(monsterBox.Width, monsterBox.Height) * 0.9f),
                centerAngle: 0.4f + (fxIndex % 4) * 0.85f,
                sweep: 2.0f,
                width: 10f,
                ticks: 15
            );

            fxIndex++;
        }

        // Passive, but not every frame. Higher stages feel more "mastered" by ticking a little faster.
        UltimateAuraTick = stage switch
        {
            1 => 60,
            2 => 54,
            _ => 48
        };
    }

    private bool UseUltimateFootwork(SkillProgress progress, int stage)
    {
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        int facing = player.FacingDirection;
        int weaponDamage = GetWeaponReferenceDamage();

        AnimateSwordSkill(player, 20f);

        Vector2 start = player.Position;

        float maxDistancePixels = stage switch
        {
            1 => 2f * 64f,
            2 => 4f * 64f,
            _ => 6f * 64f
        };

        DashForwardPixels(player, facing, maxDistancePixels);

        Vector2 end = player.Position;
        int thickness = stage switch
        {
            1 => 64,
            2 => 92,
            _ => 124
        };

        Rectangle path = BuildPathRectangle(start, end, thickness);
        float stageBonus = stage switch
        {
            1 => 0f,
            2 => 0.15f,
            _ => 0.30f
        };

        int damage = ScaleDamage(
            weaponDamage,
            SingleHitMultiplier(progress, stageBonus)
        );

        DamageArea(location, path, damage, 0.01f);
        AddDashSlashFx(start, end, facing, stage + 1);

        // No cooldown by design.
        location.localSound("swordswipe");
        return true;
    }

    // ---------------------------------------------------------------------
    // Runtime
    // ---------------------------------------------------------------------

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

            AddArcFx(
                hit.EffectCenter,
                hit.EffectRadius,
                FacingAngle(hit.FacingDirection) + hit.EffectVariant * 0.65f,
                sweep: 1.95f,
                width: 10f + (hit.EffectVariant % 2) * 2f,
                ticks: 13
            );

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
                AddArcFx(
                    wave.Position,
                    wave.VisualRadius * 0.72f,
                    (float)Math.Atan2(wave.Direction.Y, wave.Direction.X) + MathF.PI,
                    2.2f,
                    Math.Max(7f, wave.VisualWidth * 0.65f),
                    9
                );

                wave.HitEvents++;

                if (wave.HitEvents >= wave.MaxHitEvents)
                {
                    Projectiles.RemoveAt(i);
                    continue;
                }

                wave.Position += wave.Direction * 58f;
                wave.DistanceTravelled += 58f;
                wave.HitSkipTicks = 2;
            }

            if (wave.RemainingTicks <= 0)
                Projectiles.RemoveAt(i);
        }
    }

    private void UpdateEffects()
    {
        for (int i = ArcEffects.Count - 1; i >= 0; i--)
        {
            ArcEffects[i].Ticks--;

            if (ArcEffects[i].Ticks <= 0)
                ArcEffects.RemoveAt(i);
        }
    }

    private void QueueMultiHit(
        GameLocation location,
        Rectangle area,
        int damage,
        int hitCount,
        int tickGap,
        float knockback,
        int facing,
        Vector2 effectCenter,
        float effectRadius
    )
    {
        DamageArea(location, area, damage, knockback);

        AddArcFx(
            effectCenter,
            effectRadius,
            FacingAngle(facing) - 0.5f,
            sweep: 1.95f,
            width: 11f,
            ticks: 13
        );

        for (int i = 1; i < hitCount; i++)
        {
            PendingHits.Add(new PendingHit
            {
                Location = location,
                Area = area,
                Damage = damage,
                Knockback = knockback,
                Ticks = i * tickGap,
                FacingDirection = facing,
                EffectCenter = effectCenter,
                EffectRadius = effectRadius + (i % 3 - 1) * 9f,
                EffectVariant = i
            });
        }
    }

    // ---------------------------------------------------------------------
    // Damage / animation / geometry
    // ---------------------------------------------------------------------

    private static bool TryGetEquippedSword(out MeleeWeapon? weapon)
    {
        weapon = Game1.player.CurrentTool as MeleeWeapon;
        return weapon is not null;
    }

    private static int GetWeaponReferenceDamage()
    {
        if (!TryGetEquippedSword(out MeleeWeapon? weapon) || weapon is null)
            return 1;

        int min = Math.Max(1, weapon.minDamage.Value);
        int max = Math.Max(min, weapon.maxDamage.Value);
        return Math.Max(1, (min + max) / 2);
    }

    private static float SingleHitMultiplier(
        SkillProgress progress,
        float stageBonus = 0f
    )
    {
        // Base single-hit skill = 130% of equipped sword average damage.
        // Each invested SP adds +2 percentage points so upgrades are easy to feel.
        return 1.30f + stageBonus + progress.TotalPoints * 0.02f;
    }

    private static float MultiHitMultiplier(
        SkillProgress progress,
        float stageBonus = 0f
    )
    {
        // Base multi-hit skill = 75% PER HIT.
        // Each invested SP adds +1.5 percentage points per hit.
        return 0.75f + stageBonus + progress.TotalPoints * 0.015f;
    }

    private static int ScaleDamage(int weaponReferenceDamage, float multiplier)
    {
        return Math.Max(1, (int)Math.Round(weaponReferenceDamage * multiplier));
    }

    private static void AnimateSwordSkill(Farmer player, float interval)
    {
        int animation = player.FacingDirection switch
        {
            0 => 248,
            1 => 240,
            2 => 232,
            3 => 256,
            _ => 232
        };

        player.FarmerSprite.PauseForSingleAnimation = false;
        player.FarmerSprite.animateOnce(animation, interval, 6);
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

    private static void DashForwardPixels(
        Farmer player,
        int facing,
        float maxDistancePixels
    )
    {
        Vector2 start = player.Position;

        // Safety cap prevents any unexpected infinite movement loop.
        for (int i = 0; i < 512; i++)
        {
            if (Vector2.Distance(start, player.Position) >= maxDistancePixels)
                break;

            Vector2 before = player.Position;

            player.tryToMoveInDirection(
                facing,
                isFarmer: true,
                damagesFarmer: 0,
                glider: false
            );

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

    private static void DamageArea(
        GameLocation location,
        Rectangle area,
        int damage,
        float knockback
    )
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

    // ---------------------------------------------------------------------
    // Visual FX
    // ---------------------------------------------------------------------

    private void AddDashSlashFx(Vector2 start, Vector2 end, int facing, int count)
    {
        Vector2 worldStart = start + new Vector2(32f, 32f);
        Vector2 worldEnd = end + new Vector2(32f, 32f);

        for (int i = 0; i < count; i++)
        {
            float t = (i + 1f) / (count + 1f);
            Vector2 center = Vector2.Lerp(worldStart, worldEnd, t);

            AddArcFx(
                center,
                38f + i * 8f,
                FacingAngle(facing) + (i % 2 == 0 ? -0.55f : 0.55f),
                sweep: 1.95f,
                width: 9f + i * 1.5f,
                ticks: 11 + i * 2
            );
        }
    }

    private void AddIssenFx(
        Vector2 start,
        Vector2 end,
        int facing,
        int widthPixels,
        int stage
    )
    {
        Vector2 startCenter = start + new Vector2(32f, 32f);
        Vector2 endCenter = end + new Vector2(32f, 32f);
        Vector2 direction = DirectionVector(facing);
        Vector2 perpendicular = new(-direction.Y, direction.X);

        int laneCount = stage switch
        {
            1 => 1,
            2 => 5,
            _ => 9
        };

        float spread = Math.Max(0f, widthPixels - 64f);

        for (int lane = 0; lane < laneCount; lane++)
        {
            float laneT = laneCount == 1 ? 0.5f : lane / (float)(laneCount - 1);
            float offset = -spread / 2f + spread * laneT;

            for (int along = 1; along <= 3 + stage; along++)
            {
                float t = along / (float)(4 + stage);
                Vector2 center =
                    Vector2.Lerp(startCenter, endCenter, t)
                    + perpendicular * offset;

                AddArcFx(
                    center,
                    radius: 38f + stage * 11f + along * 3f,
                    centerAngle: FacingAngle(facing)
                        + ((lane + along) % 2 == 0 ? -0.55f : 0.55f),
                    sweep: 2.10f + stage * 0.10f,
                    width: 9f + stage * 2.5f,
                    ticks: 12 + stage * 3
                );
            }
        }
    }

    private void AddFrontSlashFx(
        Rectangle area,
        int facing,
        int variant,
        float radius,
        int ticks
    )
    {
        Vector2 center = new(area.Center.X, area.Center.Y);

        AddArcFx(
            center,
            radius,
            FacingAngle(facing) + (variant % 2 == 0 ? -0.25f : 0.35f),
            sweep: variant == 0 ? 2.0f : 2.25f,
            width: variant == 0 ? 12f : 15f,
            ticks: ticks
        );
    }

    private void AddArcFx(
        Vector2 center,
        float radius,
        float centerAngle,
        float sweep,
        float width,
        int ticks
    )
    {
        ArcEffects.Add(new ArcFx
        {
            Center = center,
            Radius = radius,
            CenterAngle = centerAngle,
            Sweep = sweep,
            Width = width,
            Ticks = ticks,
            MaxTicks = ticks
        });
    }

    private static float FacingAngle(int facingDirection)
    {
        return facingDirection switch
        {
            0 => -MathF.PI / 2f,
            1 => 0f,
            2 => MathF.PI / 2f,
            3 => MathF.PI,
            _ => 0f
        };
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

    private static void DrawArc(
        SpriteBatch b,
        Vector2 center,
        float radius,
        float centerAngle,
        float sweep,
        float width,
        Color color,
        int segments
    )
    {
        float startAngle = centerAngle - sweep / 2f;
        Vector2 previous = center + new Vector2(
            MathF.Cos(startAngle),
            MathF.Sin(startAngle)
        ) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = startAngle + sweep * t;

            Vector2 current = center + new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle)
            ) * radius;

            DrawLine(b, previous, current, width, color);
            previous = current;
        }
    }

    private static void DrawLine(
        SpriteBatch b,
        Vector2 start,
        Vector2 end,
        float width,
        Color color
    )
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
        public Vector2 EffectCenter { get; set; }
        public float EffectRadius { get; set; }
        public int EffectVariant { get; set; }
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

        public float VisualRadius { get; set; } = 48f;
        public float VisualWidth { get; set; } = 13f;
        public float VisualSweep { get; set; } = 2.45f;
    }

    private sealed class ArcFx
    {
        public Vector2 Center { get; set; }
        public float Radius { get; set; }
        public float CenterAngle { get; set; }
        public float Sweep { get; set; }
        public float Width { get; set; }
        public int Ticks { get; set; }
        public int MaxTicks { get; set; }
    }
}
