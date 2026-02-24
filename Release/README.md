<div align="center">

# Haldor Bounties

Turn Haldor into a daily contract board with biome-locked bounty progression, player-style bounty NPCs, and choose-one reward payouts.

[![Version](https://img.shields.io/badge/Version-0.0.1-blue?style=for-the-badge)](https://github.com/JoeCorrell/HaldorBounties/releases)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.2200+-orange?style=for-the-badge)](#requirements)
[![Bounties](https://img.shields.io/badge/Bounties-180-green?style=for-the-badge)](#features)

---

<p align="center">
<a href="https://ko-fi.com/profmags">
<img src="https://storage.ko-fi.com/cdn/kofi3.png?v=3" alt="Support me on Ko-fi" width="300" style="border-radius: 0;"/>
</a>
</p>

## Features

Adds a fourth **Bounties** tab to HaldorOverhaul (Buy/Sell/Bank/Bounties)<br/>
Daily deterministic board reset with shared server-wide selection<br/>
Board rolls **4 bounties per day**: 2 Kill, 1 Miniboss, 1 Raid<br/>
180 default bounty definitions across Meadows -> Mistlands<br/>
Biome-gated progression so contracts match player progression<br/>
Choose **1 of 4** rewards on turn-in: Coins, Ingots, Resources, or Consumables<br/>
Rewards scale by biome tier and bounty type multiplier (Miniboss/Raid bonuses)<br/>
Active bounties persist across days until completed or abandoned

<hr/>

## How It Works

Each in-game day, the board rotates to a new set of contracts. Completed (claimed) bounties reset on day change, while active bounties stay active until you finish or abandon them.<br/>
A live timer in the bounty UI shows exactly when the next reset happens.

The core gameplay loop is:

Accept a bounty from Haldor<br/>
Complete the objective in the world<br/>
Return to Haldor and choose 1 reward option<br/>
Repeat after the daily reset

<hr/>

## Bounty Types

**Easy - Kill Contracts**<br/>
Basic creature hunts like killing Deer, Boars, Greydwarves, Draugr, Wolves, Seekers, etc. These are the bread-and-butter progression contracts.

**Medium - Miniboss Hunts**<br/>
A named bounty NPC spawns in the wild with a map marker. These NPCs are built from the player prefab and fight like hostile humanoid players (melee, 2H, bows/crossbows, armor sets, aggressive AI).

**Hard - Raid Contracts**<br/>
A raider warband spawns as a group objective. You must clear all required raiders for completion. Raids use the same bounty NPC system, tuned for group combat.

<hr/>

## Biome Locking

Bounties are biome-tiered and boss-gated, so contract difficulty follows world progression and avoids late-game bounty pressure early on.

No boss key: Meadows<br/>
Eikthyr defeated: Black Forest<br/>
The Elder defeated: Swamp<br/>
Bonemass defeated: Mountain<br/>
Moder defeated: Plains<br/>
Yagluth defeated: Mistlands

This keeps bounty enemies and rewards aligned with where your world progression currently is.

<hr/>

## Rewards

When a bounty is ready, you choose **one** reward option:

`Coins` (bank deposit)<br/>
`Ingots` (metals/refined bars tiered by biome)<br/>
`Resources` (crafting mats tiered by biome)<br/>
`Consumables` (food/mead tiered by biome)

Coin rewards are deposited directly into the shared Haldor bank system from HaldorOverhaul.<br/>
Item rewards are delivered to inventory (or dropped nearby if inventory is full).

Reward tier is derived from the bounty biome gate, and type multipliers apply:

Miniboss: 1.5x reward scaling<br/>
Raid: 1.25x reward scaling

<hr/>

## UI Flow

Left panel sections:<br/>
`Available` (today's unaccepted bounties)<br/>
`Active` (accepted and in-progress/ready bounties)<br/>
`Completed` (claimed this day)

Right panel shows details, progress, tier, objective, reward choices, and Accept/Abandon/Claim state.

<hr/>

## Configuration

On first launch, the mod creates:

`BepInEx/config/HaldorBounties.bounties.json`

The file uses a wrapper schema:

```json
{
  "Version": 2,
  "Bounties": [
    {
      "Id": "m_deer_1",
      "Title": "Eikthyr's Offering",
      "Description": "Haldor craves fresh venison for his stores...",
      "Type": "Kill",
      "Target": "Deer",
      "Amount": 5,
      "Reward": 30,
      "RequiredBoss": "",
      "SpawnLevel": 0,
      "Tier": "Easy",
      "Gender": 0
    }
  ]
}
```

`Type`: `Kill`<br/>
`Tier`: `Easy`, `Medium`, `Hard`, `Miniboss`, `Raid`<br/>
`SpawnLevel`: must be > 0 for Miniboss/Raid entries<br/>
`Gender`: `0` random, `1` male, `2` female (used for miniboss naming/model selection)

Delete the config file to regenerate defaults.

<hr/>

## Console Command

`BountyReset` clears bounty state/progress and despawns tracked bounty creatures for the local player profile.

<hr/>

## Requirements

Valheim<br/>
BepInEx 5.4.2200 or newer<br/>
JsonDotNET 13.0.4 or newer<br/>
HaldorOverhaul 1.0.18 or newer

<hr/>

## Installation

Install BepInEx and JsonDotNET<br/>
Install HaldorOverhaul (required dependency)<br/>
Download the latest release<br/>
Extract `HaldorBounties.dll` to `BepInEx/plugins/HaldorBounties/`<br/>
Launch the game and open Haldor

<hr/>

## Credits

Built as a companion mod for [HaldorOverhaul](https://github.com/JoeCorrell/HaldorOverhaul)<br/>
Inspired by expanded trader/progression workflows in the Valheim modding ecosystem

[![GitHub](https://img.shields.io/badge/GitHub-Issues-181717?style=for-the-badge&logo=github)](https://github.com/JoeCorrell/HaldorBounties/issues)
[![Discord](https://img.shields.io/badge/Discord-@profmags-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.com)

</div>

