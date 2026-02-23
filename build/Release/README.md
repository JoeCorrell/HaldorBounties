<div align="center">

# Haldor Bounties

**A daily bounty board extension for Haldor's shop — accept quests, hunt minibosses, and earn coins**

[![Version](https://img.shields.io/badge/Version-1.0.0-blue?style=for-the-badge)](https://github.com/JoeCorrell/HaldorBounties/releases)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.2200+-orange?style=for-the-badge)](#-requirements)
[![Bounties](https://img.shields.io/badge/Bounties-150+-green?style=for-the-badge)](#)

---

</div>

## Overview

Haldor Bounties adds a fourth **Bounties** tab to the HaldorOverhaul trader UI. Every in-game day, a new set of bounties is drawn from a pool of over 150 quests. Accept bounties, track your progress, and claim rewards deposited directly into Haldor's Bank.

This mod **requires** [HaldorOverhaul](https://github.com/JoeCorrell/HaldorOverhaul) as a dependency. It integrates seamlessly as an extension tab alongside Buy, Sell, and Bank.

---

## How It Works

### Daily Rotation

Every in-game day, the bounty board refreshes with **6 new bounties**:

| Slot | Type | Description |
|------|------|-------------|
| 2 | **Gather** | Collect resources by picking them up in the world |
| 2 | **Kill** | Hunt and kill a number of creatures |
| 2 | **Miniboss** | Accept a contract to slay a powerful starred creature |

The selection is **deterministic** — all players on the same server see the same bounties each day. A live countdown timer in the top-right corner shows when the next rotation happens.

### Bounty States

Each bounty moves through a simple lifecycle:

1. **Available** — Shown in the top section of the bounty list. Click to view details, then click **Accept** to start.
2. **Active** — Moves to the bottom section. Progress is tracked automatically as you play. You can **Abandon** active bounties if you change your mind (abandoned bounties disappear for the rest of the day).
3. **Ready** — Progress is complete. Return to Haldor to claim your reward.
4. **Claimed** — Reward deposited to your bank. Resets on the next day.

Active bounties persist across day changes — if you accepted a bounty yesterday and haven't finished it, it stays in your active list until you complete or abandon it.

---

## Bounty Types

### Kill Bounties

Kill a specified number of creatures. Progress is tracked automatically whenever you kill a matching creature. Only creatures you've damaged count toward your bounty.

**Example:** *Greydwarf Scouts* — Kill 8 Greydwarves (45 coins)

### Gather Bounties

Collect resources by physically picking them up from the world. Items already in your inventory, transferred from chests, or moved between containers do **not** count — only fresh pickups from the ground are tracked.

When the bounty is ready, you must have the required items in your inventory to turn in. Haldor keeps the materials and pays you in coins.

**Example:** *Hide Delivery* — Gather 10 Deer Hides (35 coins)

### Miniboss Bounties

The most rewarding bounties. When you accept a miniboss bounty, a **starred creature** spawns approximately 300 meters from your position. These creatures have:

- **Starred levels** (2-star or 3-star) with massively increased health and damage
- A **boss health bar** displayed on screen (slightly smaller than game boss bars)
- A **unique name** generated each day (e.g., "Grendel the Champion Troll")
- A **red area circle** on the minimap showing the approximate spawn region (not the exact location)

Kill the creature to complete the bounty. The minimap marker is removed once the target is dead.

**Example:** *Champion Troll* — Slay Thundermaw the Champion Troll (500 coins)

---

## Difficulty Tiers

Bounties are color-coded by difficulty in the bounty list:

| Tier | Color | Biomes | Reward Range |
|------|-------|--------|-------------|
| **Easy** | Green | Meadows, Black Forest | 25 - 200 coins |
| **Medium** | Yellow | Swamp, Mountain | 100 - 350 coins |
| **Hard** | Red | Plains, Mistlands, Ashlands | 200 - 800 coins |
| **Miniboss** | Purple | All biomes (scaled) | 80 - 2,500 coins |

Higher-tier bounties are **locked behind boss progression**. You must defeat the corresponding boss before bounties from that biome appear:

| Boss Defeated | Unlocks |
|---------------|---------|
| *None* | Meadows bounties |
| Eikthyr | Black Forest bounties |
| The Elder | Swamp bounties |
| Bonemass | Mountain bounties |
| Moder | Plains bounties |
| Yagluth | Mistlands bounties |

---

## UI Layout

The bounty panel is split into two sections:

**Left Panel**
- **Available** (top) — Today's bounties you haven't accepted yet, color-coded by tier
- **Active** (bottom) — Bounties you've accepted, showing progress

**Right Panel**
- Bounty title, tier badge, and full description
- Progress bar and objective counter
- Goal summary above the action button
- Reward amount
- Action button (Accept / Abandon / Claim / Turn In)

---

## Rewards

All bounty rewards are deposited directly into **Haldor's Bank** (from HaldorOverhaul). Use the Bank tab to withdraw coins to your inventory, or spend them directly on purchases from Haldor's shop.

---

## Configuration

On first launch, the mod generates `BepInEx/config/HaldorBounties.bounties.json` containing all 150+ bounty definitions. You can edit this file to:

- Change reward amounts
- Modify kill/gather quantities
- Add custom bounties
- Remove bounties you don't want
- Adjust boss progression gates

Each entry looks like this:

```json
{
  "id": "greydwarf_scout",
  "title": "Greydwarf Scouts",
  "description": "Greydwarf scouts have been spotted creeping along the forest edge...",
  "type": "Kill",
  "target": "Greydwarf",
  "amount": 8,
  "reward": 45,
  "required_boss": "defeated_eikthyr",
  "spawn_level": 0,
  "tier": "Easy"
}
```

**Fields:**
- `id` — Unique identifier (must be unique across all bounties)
- `title` — Display name in the bounty list
- `description` — Flavor text shown in the detail panel
- `type` — `"Kill"` or `"Gather"`
- `target` — Creature or item prefab name (e.g., `"Greydwarf"`, `"DeerHide"`)
- `amount` — Number of kills or items required
- `reward` — Coins deposited to bank on completion
- `required_boss` — Global key for boss gate (empty string = always available)
- `spawn_level` — For miniboss bounties: the starred level to spawn (2 = 1-star, 3 = 2-star, 4 = 3-star). Set to `0` for normal bounties.
- `tier` — `"Easy"`, `"Medium"`, `"Hard"`, or `"Miniboss"` (controls color coding and daily selection)

Delete the config file to regenerate defaults.

---

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and [JsonDotNET](https://valheim.thunderstore.io/package/ValheimModding/JsonDotNET/)
2. Install [HaldorOverhaul](https://github.com/JoeCorrell/HaldorOverhaul) (required dependency)
3. Download the latest release and extract `HaldorBounties.dll` to `BepInEx/plugins/HaldorBounties/`
4. Launch Valheim and visit Haldor — the **Bounties** tab will appear alongside Buy, Sell, and Bank

---

## Requirements

- **Valheim** (latest version)
- **BepInEx 5.4.2200+**
- **JsonDotNET 13.0.4+**
- **HaldorOverhaul 1.0.16+**

---

## Credits

Built as a companion mod for [HaldorOverhaul](https://github.com/JoeCorrell/HaldorOverhaul).

[![GitHub](https://img.shields.io/badge/GitHub-Issues-181717?style=for-the-badge&logo=github)](https://github.com/JoeCorrell/HaldorBounties/issues)
[![Discord](https://img.shields.io/badge/Discord-@profmags-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.com)
