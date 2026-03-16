# Tower of Saviors -- Data Schemas

This document describes every JSON data file used by Tower of Saviors, located
under `games/tower-of-saviors/shared/data/`. Each section covers the file's
purpose, its field-by-field structure, cross-references to other files, and the
framework schema that validates it.

All data files are loaded at startup through the `GameData.load_definitions`
API. In Godot this happens in `tos_game.gd` `_ready()`; in Unity it happens via
`TosGame.LoadData()`. The API expects every file to be a **JSON array of
objects**, each containing an integer `id` field used as the lookup key.

```
Element IDs used throughout all files:
  1 = Water    2 = Fire    3 = Grass
  4 = Light    5 = Dark    6 = Heart (healing / neutral)
```

---

## Table of Contents

1. [monsters.json](#1-monstersjson)
2. [skills.json](#2-skillsjson)
3. [leader_skills.json](#3-leader_skillsjson)
4. [stages.json](#4-stagesjson)
5. [gacha_pools.json](#5-gacha_poolsjson)
6. [loot_tables.json](#6-loot_tablesjson)
7. [element_chart.json](#7-element_chartjson)
8. [Framework Schema Reference](#8-framework-schema-reference)
9. [Loading Conventions](#9-loading-conventions)

---

## 1. monsters.json

**Path:** `games/tower-of-saviors/shared/data/monsters.json`
**GameData type key:** `"monsters"`
**Schema:** `framework/shared/schemas/monster_def.schema.json`

Defines every monster card in the game, including normal monsters, evolved
forms, and evolution material items. Each entry specifies base and max stats,
element affinity, skill bindings, and an optional evolution path.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | Yes | Unique monster identifier. Referenced by `evolve_to`, `evolve_materials`, gacha entries, loot entries, and stage rewards. |
| `name` | `string` | Yes | Display name shown in the UI. |
| `element` | `integer` (1-6) | Yes | Element affinity. See element ID table above. |
| `rarity` | `integer` (1-8) | Yes | Star rarity. Determines team cost, gacha pool tier, and visual badge. |
| `max_level` | `integer` | Yes | Maximum level the monster can reach. |
| `base_hp` | `number` | Yes | HP stat at level 1. |
| `base_atk` | `number` | Yes | ATK stat at level 1. |
| `base_rec` | `number` | Yes | REC (recovery) stat at level 1. |
| `max_hp` | `number` | Yes | HP stat at max level. |
| `max_atk` | `number` | Yes | ATK stat at max level. |
| `max_rec` | `number` | Yes | REC stat at max level. |
| `cost` | `integer` | Yes | Team cost consumed when this monster occupies a team slot. |
| `active_skill_id` | `integer` or `null` | No | FK to `skills.json` `id`. `null` if the monster has no active skill (e.g., evolution materials). |
| `leader_skill_id` | `integer` or `null` | No | FK to `leader_skills.json` `id`. `null` if the monster has no leader skill. |
| `evolve_to` | `integer` or `null` | No | FK to another `monsters.json` `id` -- the evolution target. `null` if the monster cannot evolve. |
| `evolve_materials` | `integer[]` | No | Array of `monsters.json` `id` values required as evolution materials. Empty array if no evolution. |
| `exp_curve` | `string` | Yes | Experience curve name. One of `"standard"`, `"fast"`, `"slow"`, `"super_slow"`, `"flat"`, `"advanced"`. |

### Cross-References

- `active_skill_id` --> `skills.json` (by `id`)
- `leader_skill_id` --> `leader_skills.json` (by `id`)
- `evolve_to` --> `monsters.json` (by `id`, self-referential)
- `evolve_materials[n]` --> `monsters.json` (by `id`, self-referential)
- Referenced BY: `gacha_pools.json` entries (`monster_id`), `loot_tables.json` entries (`item_id`), `stages.json` rewards (`id` / `item_id`)

### Example Entry (first record)

```json
{
    "id": 1,
    "name": "Water Dragon",
    "element": 1,
    "rarity": 5,
    "max_level": 99,
    "base_hp": 800,
    "base_atk": 350,
    "base_rec": 150,
    "max_hp": 3200,
    "max_atk": 1400,
    "max_rec": 350,
    "cost": 25,
    "active_skill_id": 1,
    "leader_skill_id": 1,
    "evolve_to": 11,
    "evolve_materials": [101, 102],
    "exp_curve": "standard"
}
```

### Validation Notes

Validated by `monster_def.schema.json`. The schema enforces:
- `element` must be one of `[1, 2, 3, 4, 5, 6]`
- `rarity` must be in range 1-8
- `max_level` must be >= 1
- `cost` must be >= 1
- `exp_curve` must be one of `"standard"`, `"slow"`, `"fast"`, `"super_slow"` (the schema lists these; the data also uses `"flat"` and `"advanced"` for materials and evolved forms)
- `additionalProperties: false` -- no extra fields allowed

---

## 2. skills.json

**Path:** `games/tower-of-saviors/shared/data/skills.json`
**GameData type key:** `"skills"`
**Schema:** `framework/shared/schemas/skill_def.schema.json`

Defines active skills -- abilities that the player manually activates during
battle after their cooldown expires. Each skill uses the **condition-outcome
rule model**: an ordered array of rules, where each rule pairs conditions with
outcomes. Rules are evaluated top-to-bottom; all matching rules fire.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | Yes | Unique skill identifier. Referenced by `monsters.json` `active_skill_id`. |
| `name` | `string` | Yes | Display name of the skill. |
| `description` | `string` | Yes | Human-readable text describing the skill effect. |
| `type` | `string` | Yes | Always `"active"` in this file. Enum: `"active"`, `"leader"`, `"team"`. |
| `max_cd` | `integer` | No | Maximum cooldown in turns (used at skill level 1). Required for active skills. |
| `min_cd` | `integer` | No | Minimum cooldown in turns (used at max skill level). Required for active skills. |
| `max_level` | `integer` | No | Maximum skill level. Each level-up typically reduces cooldown by 1 turn. |
| `rules` | `Rule[]` | Yes | Ordered list of condition-outcome rules. See sub-tables below. |

#### Rule Object

| Field | Type | Required | Description |
|---|---|---|---|
| `conditions` | `Condition[]` | Yes | All conditions must be true for outcomes to fire. |
| `outcomes` | `Outcome[]` | Yes | Effects applied when all conditions are met. Must have at least 1 entry. |

#### Condition Object

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | `string` | Yes | Condition evaluator name. Known types: `"always_true"`, `"combo_gte"`, `"combo_lt"`, `"hp_below"`, `"element_match"`. |
| `params` | `object` | No | Evaluator-specific parameters. Structure varies by condition type. |

#### Outcome Object

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | `string` | Yes | Effect handler name. Known types: `"area_damage"`, `"single_target_damage"`, `"gem_conversion"`, `"heal_over_time"`, `"atk_buff"`, `"self_damage"`, `"defense_buff"`, `"delay_enemies"`, `"combo_scaling_atk"`, `"element_change"`, `"rec_buff"`, `"lifesteal"`. |
| `params` | `object` | No | Handler-specific parameters. Structure varies by outcome type. |
| `duration` | `integer` | No | Number of turns the effect lasts. 0 or omitted means instantaneous. |

### Observed Outcome Param Structures

| Outcome Type | Params |
|---|---|
| `area_damage` | `{ "element": int, "multiplier": float }` |
| `single_target_damage` | `{ "element": int, "multiplier": float, "target": string }` |
| `gem_conversion` | `{ "from_element": int, "to_element": int }` |
| `heal_over_time` | `{ "recovery_multiplier": float, "duration_turns": int }` |
| `atk_buff` | `{ "element": int, "multiplier": float, "duration_turns": int }` |
| `self_damage` | `{ "hp_percent": float }` |
| `defense_buff` | `{ "damage_reduction": float, "duration_turns": int }` |
| `delay_enemies` | `{ "turns": int, "target": string }` |
| `combo_scaling_atk` | `{ "bonus_per_combo": float, "duration_turns": int }` |
| `element_change` | `{ "target": string, "to_element": int, "duration_turns": int }` |
| `rec_buff` | `{ "multiplier": float, "duration_turns": int }` |
| `lifesteal` | `{ "percent_of_damage": float }` |

### Cross-References

- Referenced BY: `monsters.json` `active_skill_id`
- Element IDs in params reference the element chart (`element_chart.json`)

### Example Entry (first record)

```json
{
    "id": 1,
    "name": "Tidal Wave",
    "description": "Deal water damage to all enemies",
    "type": "active",
    "max_cd": 10,
    "min_cd": 5,
    "max_level": 10,
    "rules": [
        {
            "conditions": [{"type": "always_true", "params": {}}],
            "outcomes": [{"type": "area_damage", "params": {"element": 1, "multiplier": 10.0}}]
        }
    ]
}
```

### Validation Notes

Validated by `skill_def.schema.json`. The schema enforces:
- `type` must be one of `"active"`, `"leader"`, `"team"`
- `rules` is an array of rule objects, each with `conditions` and `outcomes`
- `outcomes` must have `minItems: 1`
- Condition and outcome `params` are open objects (`additionalProperties: true`) since param structure varies by type
- `additionalProperties: false` at the top level and within rules

---

## 3. leader_skills.json

**Path:** `games/tower-of-saviors/shared/data/leader_skills.json`
**GameData type key:** `"leader_skills"`
**Schema:** `framework/shared/schemas/skill_def.schema.json` (same schema as active skills)

Defines leader skills -- passive effects that activate when a monster is placed
in the leader or helper slot. Uses the same condition-outcome rule model as
active skills but without cooldown fields.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | Yes | Unique leader skill identifier. Referenced by `monsters.json` `leader_skill_id`. |
| `name` | `string` | Yes | Display name. |
| `description` | `string` | Yes | Human-readable effect description. |
| `type` | `string` | Yes | Always `"leader"` in this file. |
| `rules` | `Rule[]` | Yes | Condition-outcome rules. Same structure as `skills.json` rules. |

Note: `max_cd`, `min_cd`, and `max_level` are **omitted** for leader skills
since they are passive and do not have cooldowns or levels.

### Observed Outcome Types

| Outcome Type | Params |
|---|---|
| `element_atk_mult` | `{ "element": int, "multiplier": float }` |
| `element_hp_mult` | `{ "element": int, "multiplier": float }` |
| `element_rec_mult` | `{ "element": int, "multiplier": float }` |

### Cross-References

- Referenced BY: `monsters.json` `leader_skill_id`
- Element IDs in params reference the element chart (`element_chart.json`)

### Example Entry (first record)

```json
{
    "id": 1,
    "name": "Water Dragon's Might",
    "description": "Water ATK x2.5",
    "type": "leader",
    "rules": [
        {
            "conditions": [{"type": "always_true", "params": {}}],
            "outcomes": [{"type": "element_atk_mult", "params": {"element": 1, "multiplier": 2.5}}]
        }
    ]
}
```

### Validation Notes

Same schema as `skills.json` (`skill_def.schema.json`). The `type` field value
`"leader"` distinguishes these from active skills. Cooldown fields are optional
in the schema, so their absence is valid.

---

## 4. stages.json

**Path:** `games/tower-of-saviors/shared/data/stages.json`
**GameData type key:** `"stages"`
**Schema:** `framework/shared/schemas/stage_def.schema.json`

Defines dungeon stages with wave-based enemy encounters and completion rewards.
Each stage consists of an ordered sequence of waves, and each wave contains one
or more enemies with stats and attack countdown timers.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | Yes | Unique stage identifier. Referenced by `loot_tables.json` `stage_id`. |
| `name` | `string` | Yes | Display name (e.g., "Aqua Temple - Normal"). |
| `stamina_cost` | `integer` | Yes | Stamina consumed when entering this stage. |
| `waves` | `Wave[]` | Yes | Ordered list of enemy waves. Must have at least 1 wave. |
| `rewards` | `Reward[]` | No | Fixed rewards granted upon stage completion. |

#### Wave Object

| Field | Type | Required | Description |
|---|---|---|---|
| `enemies` | `Enemy[]` | Yes | List of enemies in this wave. Must have at least 1 enemy. |

#### Enemy Object

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | Yes | Display name of the enemy. |
| `element` | `integer` (1-6) | Yes | Element affinity. |
| `hp` | `number` | Yes | Total hit points. |
| `atk` | `number` | Yes | Damage dealt when the countdown reaches 0. |
| `defense` | `number` | Yes | Flat damage reduction applied to incoming attacks. |
| `countdown` | `integer` | Yes | Initial turns until the enemy's first attack. |
| `max_countdown` | `integer` | Yes | Countdown resets to this value after each attack. |

#### Reward Object (as used in data)

The actual data uses a slightly different structure from the schema's `reward`
definition. The data format is:

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | `string` | Yes | Reward category: `"currency"` or `"monster"`. |
| `id` | `integer` | Yes | Reference ID. For `"currency"` type this is 0; for `"monster"` this is the monster definition ID. |
| `item_id` | `integer` | Yes | Same as `id` in practice. Used by the loot system. |
| `currency` | `string` | Conditional | Currency name (e.g., `"coins"`). Present only when `type` is `"currency"`. |
| `count` | `integer` | Yes | Quantity of the reward. |

### Cross-References

- `rewards[n].id` (when `type` is `"monster"`) --> `monsters.json` (by `id`)
- Referenced BY: `loot_tables.json` `stage_id`

### Example Entry (first record)

```json
{
    "id": 1,
    "name": "Aqua Temple - Normal",
    "stamina_cost": 10,
    "waves": [
        {
            "enemies": [
                {"name": "Water Slime", "element": 1, "hp": 5000, "atk": 300, "defense": 50, "countdown": 3, "max_countdown": 3},
                {"name": "Water Slime", "element": 1, "hp": 5000, "atk": 300, "defense": 50, "countdown": 2, "max_countdown": 3}
            ]
        },
        {
            "enemies": [
                {"name": "Water Elemental", "element": 1, "hp": 12000, "atk": 600, "defense": 100, "countdown": 2, "max_countdown": 2},
                {"name": "Water Sprite", "element": 1, "hp": 4000, "atk": 200, "defense": 30, "countdown": 1, "max_countdown": 2}
            ]
        },
        {
            "enemies": [
                {"name": "Water Dragon Boss", "element": 1, "hp": 50000, "atk": 1500, "defense": 200, "countdown": 1, "max_countdown": 2}
            ]
        }
    ],
    "rewards": [
        {"type": "currency", "id": 0, "item_id": 0, "currency": "coins", "count": 1000},
        {"type": "monster", "id": 16, "item_id": 16, "count": 1}
    ]
}
```

### Validation Notes

Validated by `stage_def.schema.json`. The schema enforces:
- `waves` must have `minItems: 1`
- Each wave's `enemies` must have `minItems: 1`
- Enemy `element` must be one of `[1, 2, 3, 4, 5, 6]`
- Enemy `hp` must be >= 1; `atk` and `defense` must be >= 0
- Enemy `countdown` and `max_countdown` must be >= 1
- Note: the schema's `reward` definition expects `{ type, id, amount }` fields, while the actual game data uses `{ type, id, item_id, currency, count }`. The data format is a superset that the loot/reward system consumes.

---

## 5. gacha_pools.json

**Path:** `games/tower-of-saviors/shared/data/gacha_pools.json`
**GameData type key:** `"gacha_pools"`
**Schema:** `framework/shared/schemas/gacha_pool.schema.json`

Defines gacha banners (summon pools) with weighted random selection. Each pool
lists pullable monsters with relative weights, a pity threshold for guaranteed
top-rarity pulls, and featured rate-up designations. Used by the `GachaRoller`
module to execute pulls.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | Yes | Unique pool identifier. |
| `name` | `string` | Yes | Display name of the gacha banner (e.g., "Standard Pool", "Fire Festival"). |
| `cost_currency` | `string` | Yes | Currency type required to pull (e.g., `"gems"`, `"tickets"`). |
| `cost_amount` | `integer` | Yes | Amount of currency consumed per single pull. |
| `pity_threshold` | `integer` | No | Number of pulls without a top-rarity result before a guaranteed top-rarity pull is issued. `0` means no pity system. |
| `featured_ids` | `integer[]` | No | Monster IDs that receive a rate-up in this banner. Present on event banners. |
| `entries` | `GachaEntry[]` | Yes | Weighted list of pullable entries. Must have at least 1 entry. |

#### GachaEntry Object

| Field | Type | Required | Description |
|---|---|---|---|
| `monster_id` | `integer` | Yes | FK to `monsters.json` `id`. |
| `rarity` | `integer` (1-8) | Yes | Star rarity of this entry. Should match the referenced monster's rarity. |
| `weight` | `integer` | Yes | Relative weight for weighted random selection. Higher weight = more likely. |
| `is_featured` | `boolean` | No | Whether this entry is a featured/rate-up monster in the current banner. |

### Cross-References

- `entries[n].monster_id` --> `monsters.json` (by `id`)
- `featured_ids[n]` --> `monsters.json` (by `id`)
- `cost_currency` should match a currency key in `PlayerState` currencies section (`"gems"`, `"coins"`, etc.)

### Example Entry (first record)

```json
{
    "id": 1,
    "name": "Standard Pool",
    "cost_currency": "gems",
    "cost_amount": 5,
    "pity_threshold": 50,
    "entries": [
        {"monster_id": 1, "rarity": 5, "weight": 5, "is_featured": true},
        {"monster_id": 2, "rarity": 5, "weight": 5, "is_featured": true},
        {"monster_id": 3, "rarity": 5, "weight": 5, "is_featured": true},
        {"monster_id": 4, "rarity": 5, "weight": 5, "is_featured": true},
        {"monster_id": 5, "rarity": 5, "weight": 5, "is_featured": true},
        {"monster_id": 6, "rarity": 4, "weight": 15, "is_featured": false},
        {"monster_id": 7, "rarity": 4, "weight": 15, "is_featured": false},
        {"monster_id": 8, "rarity": 4, "weight": 15, "is_featured": false},
        {"monster_id": 9, "rarity": 4, "weight": 15, "is_featured": false},
        {"monster_id": 10, "rarity": 4, "weight": 15, "is_featured": false},
        {"monster_id": 16, "rarity": 3, "weight": 40, "is_featured": false},
        {"monster_id": 17, "rarity": 3, "weight": 40, "is_featured": false},
        {"monster_id": 18, "rarity": 3, "weight": 40, "is_featured": false},
        {"monster_id": 19, "rarity": 3, "weight": 40, "is_featured": false},
        {"monster_id": 20, "rarity": 3, "weight": 40, "is_featured": false}
    ]
}
```

### Validation Notes

Validated by `gacha_pool.schema.json`. The schema enforces:
- `cost_amount` must be >= 0
- `pity_threshold` must be >= 0
- `entries` must have `minItems: 1`
- Each entry's `rarity` must be 1-8
- Each entry's `weight` must be >= 1
- `additionalProperties: false` at both pool and entry level

---

## 6. loot_tables.json

**Path:** `games/tower-of-saviors/shared/data/loot_tables.json`
**GameData type key:** not loaded via `load_definitions` in the default startup (loaded on demand by the loot module)
**Schema:** No dedicated JSON Schema file; structure documented in `docs/framework/domain/loot.md`

Defines per-stage loot tables for post-battle random drops. Each table is
associated with a stage and contains weighted entries. Entries can be marked as
guaranteed (always drop) or random (rolled via weighted selection). Rolled by
the `MFLootTable` module.

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `id` | `integer` | Yes | Unique loot table identifier. |
| `stage_id` | `integer` | Yes | FK to `stages.json` `id`. Links this loot table to a stage. |
| `entries` | `LootEntry[]` | Yes | List of possible drops, both guaranteed and random. |

#### LootEntry Object

| Field | Type | Required | Description |
|---|---|---|---|
| `type` | `string` | Yes | Drop category: `"currency"`, `"monster"`, or `"item"`. |
| `item_id` | `integer` | Yes | Reference ID. For `"monster"` type, FK to `monsters.json` `id`. For `"currency"`, typically 0. |
| `currency` | `string` | Conditional | Currency name (e.g., `"coins"`, `"gems"`). Present only when `type` is `"currency"`. |
| `count_min` | `integer` | Yes | Minimum quantity dropped. |
| `count_max` | `integer` | Yes | Maximum quantity dropped. Actual count is random in `[count_min, count_max]`. |
| `weight` | `integer` | Yes | Relative weight for random selection among non-guaranteed entries. |
| `guaranteed` | `boolean` | Yes | If `true`, this entry always drops (not subject to weight-based rolling). |

### Cross-References

- `stage_id` --> `stages.json` (by `id`)
- `item_id` (when `type` is `"monster"`) --> `monsters.json` (by `id`)

### Example Entry (first record)

```json
{
    "id": 1,
    "stage_id": 1,
    "entries": [
        {"type": "currency", "item_id": 0, "currency": "coins", "count_min": 500, "count_max": 1500, "weight": 100, "guaranteed": true},
        {"type": "monster", "item_id": 16, "count_min": 1, "count_max": 1, "weight": 50, "guaranteed": false},
        {"type": "monster", "item_id": 101, "count_min": 1, "count_max": 1, "weight": 30, "guaranteed": false},
        {"type": "monster", "item_id": 102, "count_min": 1, "count_max": 1, "weight": 15, "guaranteed": false}
    ]
}
```

### Validation Notes

No dedicated JSON Schema file exists for loot tables. The structure is
documented in `docs/framework/domain/loot.md` and enforced at runtime by the
`MFLootTable` module. Guaranteed entries are collected unconditionally; remaining
entries are rolled via weighted random selection.

---

## 7. element_chart.json

**Path:** `games/tower-of-saviors/shared/data/element_chart.json`
**GameData type key:** loaded directly (not via `load_definitions` since it is a single object, not an array)
**Schema:** `framework/shared/schemas/element_chart.json` (reference data, not a JSON Schema)

Defines the element type system, including element names, advantage/disadvantage
multipliers, and the default neutral multiplier. This is a **single object**
(not an array), unlike all other data files.

### Top-Level Fields

| Field | Type | Description |
|---|---|---|
| `elements` | `object` | Map of element ID (string key) to element name. Keys: `"1"` through `"6"`. |
| `advantages` | `Matchup[]` | List of attacker-defender pairs where the attacker deals bonus damage. |
| `disadvantages` | `Matchup[]` | List of attacker-defender pairs where the attacker deals reduced damage. |
| `default_multiplier` | `number` | Damage multiplier when no advantage or disadvantage applies. Always `1.0`. |

#### Matchup Object

| Field | Type | Description |
|---|---|---|
| `attacker` | `integer` | Element ID of the attacking side. |
| `defender` | `integer` | Element ID of the defending side. |
| `multiplier` | `number` | Damage multiplier applied. `1.5` for advantages, `0.5` for disadvantages. |

### Element Relationships

```
Advantages (x1.5 damage):
  Water (1) --> Fire (2)
  Fire  (2) --> Grass (3)
  Grass (3) --> Water (1)
  Light (4) <-> Dark (5)    (mutual advantage)

Disadvantages (x0.5 damage):
  Water (1) --> Grass (3)
  Fire  (2) --> Water (1)
  Grass (3) --> Fire (2)

Neutral (x1.0 damage):
  All other combinations
```

Note: Light and Dark have mutual advantage but no mutual disadvantage.

### Full File Content

```json
{
    "elements": {
        "1": "water",
        "2": "fire",
        "3": "grass",
        "4": "light",
        "5": "dark",
        "6": "heart"
    },
    "advantages": [
        {"attacker": 1, "defender": 2, "multiplier": 1.5},
        {"attacker": 2, "defender": 3, "multiplier": 1.5},
        {"attacker": 3, "defender": 1, "multiplier": 1.5},
        {"attacker": 4, "defender": 5, "multiplier": 1.5},
        {"attacker": 5, "defender": 4, "multiplier": 1.5}
    ],
    "disadvantages": [
        {"attacker": 1, "defender": 3, "multiplier": 0.5},
        {"attacker": 2, "defender": 1, "multiplier": 0.5},
        {"attacker": 3, "defender": 2, "multiplier": 0.5}
    ],
    "default_multiplier": 1.0
}
```

### Validation Notes

The framework provides a reference copy at `framework/shared/schemas/element_chart.json`
which includes an additional `special_ids` section defining targeting constants
(`-1` = SELF, `-2` = ALL_ENEMIES, etc.). The game data file does not include
`special_ids`; those are defined at the framework level.

---

## 8. Framework Schema Reference

All JSON Schema files live under `framework/shared/schemas/`:

| Schema File | Validates | Format |
|---|---|---|
| `monster_def.schema.json` | `monsters.json` | JSON Schema Draft-07 |
| `skill_def.schema.json` | `skills.json` and `leader_skills.json` | JSON Schema Draft-07 |
| `stage_def.schema.json` | `stages.json` | JSON Schema Draft-07 |
| `gacha_pool.schema.json` | `gacha_pools.json` | JSON Schema Draft-07 |
| `element_chart.json` | `element_chart.json` (reference data) | Plain JSON (not a schema) |
| `save_format.schema.json` | Save files (not a data definition file) | JSON Schema Draft-07 |
| `gem_status_types.json` | Gem status type definitions (board mechanics) | Plain JSON (not a schema) |

### Additional Framework Reference Files

- **`gem_status_types.json`** -- Defines the 14 gem status types used by the
  board system (e.g., frozen, locked, burning, poisoned, enchanted). Includes
  exclusion groups that prevent conflicting statuses from stacking.

- **`save_format.schema.json`** -- Defines the save file envelope format with
  `version`, `timestamp`, `checksum`, and `data` fields. Used by `SaveMigrator`
  for versioned save file handling.

---

## 9. Loading Conventions

### Godot (tos_game.gd)

Data files are loaded during `_ready()` via the `GameData` autoload singleton:

```gdscript
_game_data.load_definitions(&"monsters", "res://games/tower-of-saviors/shared/data/monsters.json")
_game_data.load_definitions(&"skills", "res://games/tower-of-saviors/shared/data/skills.json")
_game_data.load_definitions(&"leader_skills", "res://games/tower-of-saviors/shared/data/leader_skills.json")
_game_data.load_definitions(&"stages", "res://games/tower-of-saviors/shared/data/stages.json")
_game_data.load_definitions(&"gacha_pools", "res://games/tower-of-saviors/shared/data/gacha_pools.json")
```

The `load_definitions` method:
1. Reads the JSON file from disk
2. Parses it as a JSON array
3. Wraps each object in a `MFDataTypes.Definition`
4. Stores in an internal `Dictionary[StringName, Dictionary[int, Definition]]` keyed by the `id` field

### Unity (TosGame.cs)

Data is loaded via `TosGame.LoadData(type, entries)`, which delegates to
`GameData.LoadDefinitions(string, List<Dictionary<string, object>>)`. JSON
parsing is handled externally (by the platform layer), and pre-parsed
dictionaries are passed in.

### Lookup API (both engines)

```
get_definition(type, id) -> Definition    # Single lookup by ID
get_all_definitions(type) -> Definition[] # All entries of a type
has_definition(type, id) -> bool          # Existence check
get_definition_count(type) -> int         # Entry count
clear_type(type) -> void                  # Remove all entries of a type
clear_all() -> void                       # Remove everything
```

### Data File Conventions

- All definition files (except `element_chart.json`) are **JSON arrays of objects**
- Every object must have an integer `id` field
- IDs must be unique within each file
- `element_chart.json` is a single object, not an array -- it is loaded separately from the standard `load_definitions` flow
- `loot_tables.json` is consumed by the loot module rather than loaded at startup

---

## Entity Relationship Diagram

```
monsters.json
  |-- active_skill_id ---------> skills.json (id)
  |-- leader_skill_id ---------> leader_skills.json (id)
  |-- evolve_to ----------------> monsters.json (id)  [self-ref]
  |-- evolve_materials[n] ------> monsters.json (id)  [self-ref]
  |
  |<-- gacha_pools.json entries[n].monster_id
  |<-- gacha_pools.json featured_ids[n]
  |<-- loot_tables.json entries[n].item_id  (when type="monster")
  |<-- stages.json rewards[n].id            (when type="monster")

stages.json
  |<-- loot_tables.json stage_id

element_chart.json
  (referenced implicitly by element IDs in all other files)
```
