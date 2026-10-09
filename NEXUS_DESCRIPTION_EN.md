# Sword Mastery

Turn sword combat into a full progression system.

Sword Mastery adds a separate sword level, skill points, branching skill trees, active sword techniques, Secret Arts, Ultimates, unlock quests, custom combat effects, and a respec system.

## Features

- Separate **Sword Mastery Level**
- Gain Sword EXP by defeating monsters
- Earn **SP** and invest it into sword skills
- Three Basic Swordsmanship branches
- Three Secret Art branches, with only one branch selectable
- Two Ultimate branches, with only one branch selectable
- Custom slash, dash, sword-wave, multi-hit, and area effects
- Cooldown HUD with configurable position
- Unlock quests at higher Sword Mastery levels
- Custom quest items and Dragon Orb drops
- Full respec support
- Generic Mod Config Menu integration
- English + Korean localization
- Vortex-friendly package layout

## Skill Tree

### Basic Swordsmanship

**Swordsman's Step**
- Stage I: forward dash
- Stage II: slash along the dash path
- Stage III: multi-slash dash

**Slash**
- Stage I: forward slash
- Stage II: wider slash
- Stage III: ranged crescent sword wave

**Calico Style Swordsmanship**
- Stage I: 3-hit combo
- Stage II: 4-hit wide combo
- Stage III: 6-hit wide combo

### Secret Arts

Choose only one Secret Art branch.

**Issen**
A finishing rush technique with guaranteed 99,999 critical damage. Higher stages increase travel distance and attack width.

**Sword Wave**
Normal sword attacks automatically release crescent sword waves. Higher stages increase size, range, and piercing.

**Pinnacle of Swordsmanship**
A wide-area multi-slash finisher that rapidly cuts enemies around the player.

### Ultimates

Choose only one Ultimate branch.

**Swordsmanship Apex**
A passive area technique that automatically slashes enemies inside its radius.

**Ultimate Footwork**
A cooldown-free movement technique that damages enemies along the movement path.

## Unlock Quests

- **Sword Mastery Lv.30:** Secret Art unlock quest
- **Sword Mastery Lv.50:** Ultimate unlock quest

The Ultimate quest includes Dragon hunting and collecting custom **Dragon Orbs**.

Quest rewards are claimed from the vanilla journal.

## Controls

Default controls:

- `K` — Open Sword Mastery menu
- `F6` — Swordsman's Step
- `F7` — Slash
- `F8` — Calico Style Swordsmanship
- `F9` — Selected active Secret Art
- `F10` — Selected active Ultimate

All hotkeys can be changed through Generic Mod Config Menu.

## Requirements

**Required**
- SMAPI 4.3.0 or later

**Optional**
- Generic Mod Config Menu

## Installation

### Vortex

1. Download with Mod Manager.
2. Enable/deploy the mod in Vortex.
3. Launch the game through SMAPI.

### Manual

1. Install SMAPI.
2. Extract the `SwordMastery` folder into:
   `Stardew Valley/Mods`
3. Launch the game through SMAPI.

The final structure should look like:

```text
Stardew Valley/
└─ Mods/
   └─ SwordMastery/
      ├─ SwordMastery.dll
      ├─ manifest.json
      ├─ assets/
      └─ i18n/
```

## Languages

- English — default
- Korean — included

The mod automatically follows your **Stardew Valley language setting**. No separate language option is needed.

## Compatibility

Developed and tested for Stardew Valley 1.6.x with SMAPI.

This mod uses its own progression and skill systems and does not replace vanilla professions.

## Configuration

With Generic Mod Config Menu you can configure:

- Skill hotkeys
- Main HUD visibility and position
- Cooldown HUD visibility and position
- Debug/testing tools

## Updating

SMAPI update checks are linked to this Nexus page.

## Bug Reports

When reporting a bug, please include:

- SMAPI version
- Stardew Valley version
- Full SMAPI log
- What skill/quest you were using
- Steps to reproduce the problem

## Credits

Created for Stardew Valley using SMAPI.
