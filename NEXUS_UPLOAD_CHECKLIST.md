# Nexus Upload Checklist — Mod ID 53641

## 1. General
- Name: Sword Mastery
- Game: Stardew Valley
- Category: Gameplay Mechanics
- Current version: 0.1.0
- Brief summary:
  Adds sword levels, SP, skill trees, Secret Arts, Ultimates, quests, and custom combat effects.

## 2. Detailed description
Paste `NEXUS_DESCRIPTION_EN.md` into the main description.
Optionally add the Korean section from `NEXUS_DESCRIPTION_KO.md` underneath.

## 3. Requirements
Required:
- SMAPI

Optional:
- Generic Mod Config Menu

## 4. Files
Upload the GitHub Actions artifact ZIP:
`SwordMastery-0.1.0-Nexus-Vortex.zip`

File category:
- Main File

Recommended file name:
- Sword Mastery 0.1.0

File description:
- Main release. Includes English and Korean localization. Vortex and manual installation supported.

## 5. Vortex
The ZIP already contains a single top-level `SwordMastery` folder, so Vortex can install it directly into the Stardew Valley Mods folder.

## 6. Images
Recommended screenshots:
1. Skill tree screen
2. Issen effect
3. Sword Wave / Pinnacle effect
4. Quest journal
5. GMCM configuration page

Use at least one clean 16:9 image as the main image.

## 7. Manifest / Updates
Already configured:
`"UpdateKeys": ["Nexus:53641"]`

After publishing, SMAPI can use the Nexus page for update checks.

## 8. Before publishing
- Download the exact Nexus/Vortex ZIP from GitHub Actions
- Install that ZIP through Vortex once
- Start a fresh test save or backup save
- Verify English game language
- Verify Korean game language
- Verify both unlock quests
- Verify item rewards and skill menu
