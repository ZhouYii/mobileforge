# Tower of Saviors -- MobileForge Game Implementation

## What Is Tower of Saviors?

Tower of Saviors (ToS) is a PAD-style match-3 RPG where the player assembles a team of monsters and clears multi-wave dungeons. The core loop is:

1. **Select a dungeon** (costs stamina).
2. **Assemble a team** of up to 5 monsters plus a friend leader.
3. **Battle** through waves of enemies by solving a 6x5 gem board.
4. **Collect rewards** -- monster drops, coins, evolution materials.
5. **Upgrade monsters** -- level up, evolve, fuse.
6. **Roll gacha** for new monsters using premium currency (gems).

The puzzle mechanic is distinctive: the player picks up a gem and drags it freely across the board for a limited time. Every cell the gem passes through swaps with the dragged gem, allowing skilled players to rearrange the entire board in a single move.

## How It Maps to MobileForge

Tower of Saviors is the **reference game** that MobileForge was designed around. Every framework module has a direct ToS counterpart:

| MobileForge Module | ToS Feature |
|---|---|
| `MFBoardLogic` / `MFBoardConfig` | 6x5 gem board with drag-swap mechanic |
| `MFMatchDetector` / `MFCascadeResolver` | Match-3 detection and cascade resolution |
| `MFCombatResolver` / `MFElementChart` | 5-hook damage pipeline, element advantage chart |
| `MFDungeonRunner` / `MFDungeonTypes` | Multi-wave dungeon run with turn loop |
| `MFSkillPipeline` / `MFEffectRegistry` | Active/leader/team skills via condition-outcome JSON |
| `MFMonsterManager` / `MFStatCalculator` | Monster leveling, evolution, stat growth |
| `MFEconomy` / `MFStaminaTimer` | Gems, coins, stamina with timed refill |
| `MFGachaRoller` / `MFPityTracker` | Weighted random pulls with pity system |
| `MFLootTable` | Per-stage drop tables with guaranteed + weighted entries |
| `MFEnemyAI` | Countdown-based enemy attacks with status effects |
| `UIRouter` / `MFBaseScreen` | Screen stack navigation (title, dungeon, team, battle, result) |

## Directory Structure

```
games/tower-of-saviors/
  godot/
    game/
      tos_game.gd               # Main entry point, wires all modules
      screens/
        title_screen.gd          # Title screen with start button
        dungeon_select_screen.gd # Dungeon list, stamina check
        team_select_screen.gd    # Team composition before battle
        battle_screen.gd         # Board + enemies + combat loop
        result_screen.gd         # Win/loss and rewards display
      skill_defs/
        register_all.gd          # Registers all ToS skill conditions and effects
        conditions/              # (reserved for complex condition classes)
        outcomes/                # (reserved for complex outcome classes)
      battle/                    # (reserved for board interaction, drag handler)
  shared/
    data/
      monsters.json              # Monster definitions (23 monsters)
      skills.json                # Active skill definitions (10 skills)
      leader_skills.json         # Leader skill definitions (5 skills)
      stages.json                # Dungeon stage definitions (5 stages)
      gacha_pools.json           # Gacha pool definitions (2 pools)
      loot_tables.json           # Per-stage loot tables (5 tables)
      element_chart.json         # Element advantage/disadvantage data
```

## How to Run

1. Open the MobileForge framework Godot project at `framework/godot/`.
2. Ensure the `mobileforge` addon is enabled (Project > Project Settings > Plugins).
3. Register the framework autoloads (`EventBus`, `GameData`, `PlayerState`).
4. Set `tos_game.gd` as the main scene root script (or create a `.tscn` referencing it).
5. Run the project. The title screen appears; press "Start Game" to enter dungeon selection.

The game code has **zero framework modifications**. It only calls into framework APIs through the public interfaces documented in `docs/framework/`.

## Game Data Files Overview

### monsters.json

Array of monster definitions. Each entry has:
- `id`, `name`, `element` (1=Water, 2=Fire, 3=Grass, 4=Light, 5=Dark, 6=Heart)
- `rarity` (1-6 stars), `max_level`, stat fields (`base_hp/atk/rec`, `max_hp/atk/rec`)
- `cost` (team cost), `active_skill_id`, `leader_skill_id`
- `evolve_to` (target monster ID or null), `evolve_materials` (array of material monster IDs)
- `exp_curve` ("fast", "standard", "advanced", "flat")

Three tiers are defined: 3-star commons (ids 16-20), 4-star rares (ids 6-10), 5-star ultra-rares (ids 1-5), 6-star evolved forms (ids 11-15), and evolution materials (ids 101-103).

### skills.json

Array of active skill definitions using the condition-outcome composition format. Each skill has `rules` containing `conditions` (all must pass) and `outcomes` (executed in order). Skills include AoE damage, single-target damage, gem conversion, healing, ATK buffs, defense buffs, enemy delay, combo scaling, element change, and lifesteal.

### leader_skills.json

Passive skills applied when a monster is in the leader or friend slot. Uses the same condition-outcome format. Current leaders provide element-specific ATK/HP/REC multipliers.

### stages.json

Dungeon definitions with multi-wave enemy layouts. Each stage has `stamina_cost`, `waves` (array of enemy groups), and `rewards`. Enemies specify `element`, `hp`, `atk`, `defense`, `countdown`, and `max_countdown`.

### gacha_pools.json

Weighted pull pools with `pity_threshold` for guaranteed top-rarity. Entries specify `monster_id`, `rarity`, `weight`, and `is_featured`. Two pools: standard and event (Fire Festival with rate-up).

### loot_tables.json

Per-stage drop tables. Each entry specifies `type` ("currency" or "monster"), `item_id`, `count_min`/`count_max`, `weight`, and `guaranteed` flag. Guaranteed entries always drop; non-guaranteed use weighted random.
