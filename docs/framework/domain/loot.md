# Loot Module

## Overview

The loot module handles post-battle reward generation. It rolls drops from weighted loot tables with support for guaranteed entries and random count ranges. Like the gacha module, it is purely functional -- static methods with injected RNG, no side effects.

| File | Purpose | Dependencies |
|---|---|---|
| `loot_types` | LootTableDef, LootEntry, LootDrop data classes | None (leaf) |
| `loot_table` | MFLootTable: static roll function | loot_types |

## LootTableDef

A loot table definition loaded from JSON:

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

### LootTableDef Fields

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Unique table identifier |
| `entries` | `Array[LootEntry]` | List of possible drops |

### LootEntry Fields

| Field | Type | Description |
|---|---|---|
| `type` | `String` | Drop category: "monster", "currency", or "item" |
| `item_id` | `int` | ID of the dropped item/monster. 0 for currency types. |
| `count_min` | `int` | Minimum quantity per drop |
| `count_max` | `int` | Maximum quantity per drop (inclusive) |
| `weight` | `int` | Relative weight for weighted random selection |
| `guaranteed` | `bool` | If true, this entry always drops regardless of roll |

## MFLootTable API

### roll_drops(table_def, rng, roll_count) -> Array[LootDrop]

Roll drops from a loot table. Returns an array of `LootDrop` instances.

**Algorithm:**

1. **Guaranteed drops**: Iterate all entries with `guaranteed = true`. For each, generate a random count in `[count_min, count_max]` and add to the result.

2. **Random drops**: Collect all non-guaranteed entries. Calculate total weight. For `roll_count` iterations, perform weighted random selection and add the selected entry to results with a random count.

```
For each guaranteed entry:
    count = rng.randi_range(count_min, count_max)
    drops.append(LootDrop(type, item_id, count))

total_weight = sum(non_guaranteed entry weights)
For i in range(roll_count):
    roll = rng.randi() % total_weight
    cumulative = 0
    For each non_guaranteed entry:
        cumulative += entry.weight
        If roll < cumulative:
            count = rng.randi_range(count_min, count_max)
            drops.append(LootDrop(type, item_id, count))
            break
```

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `table_def` | `LootTableDef` | (required) | The loot table to roll from |
| `rng` | `RandomNumberGenerator` | (required) | RNG instance for deterministic testing |
| `roll_count` | `int` | 1 | Number of random (non-guaranteed) drops to roll |

## LootDrop

Output from a loot roll:

| Field | Type | Description |
|---|---|---|
| `type` | `String` | "monster", "currency", or "item" |
| `item_id` | `int` | ID of the dropped item/monster |
| `count` | `int` | Quantity obtained |

## Code Examples

### Rolling Loot After Battle

```gdscript
# Load loot table from data
var table_data = game_data.get_definition(&"loot_tables", stage_id)
var table_def = MFLootTypes.LootTableDef.new(table_data.raw())

# Roll drops
var rng = RandomNumberGenerator.new()
var drops = MFLootTable.roll_drops(table_def, rng, 3)  # 3 random rolls

# Process results
for drop in drops:
    match drop.type:
        "currency":
            economy.earn("coins", drop.count)
        "monster":
            var monster = monster_manager.create_instance(drop.item_id)
            # Add to player inventory
        "item":
            # Add item to inventory
            pass
```

### Testing with Deterministic RNG

```gdscript
# Set seed for reproducible results
var rng = RandomNumberGenerator.new()
rng.seed = 12345

var drops = MFLootTable.roll_drops(table_def, rng, 1)
# Same seed always produces same results
assert(drops[0].type == "currency")  # Guaranteed entry
```

### Displaying Loot on Result Screen

```gdscript
# After battle, show rewards
var drops = MFLootTable.roll_drops(table_def, rng, 2)
for drop in drops:
    var label = Label.new()
    label.text = "%s x%d" % [_get_drop_name(drop), drop.count]
    rewards_container.add_child(label)

func _get_drop_name(drop: MFLootTypes.LootDrop) -> String:
    match drop.type:
        "currency":
            return "Coins"
        "monster":
            var def = monster_manager.get_def(drop.item_id)
            return def.name if def != null else "Unknown Monster"
        _:
            return "Item #%d" % drop.item_id
```

## Loot Table Design Patterns

### Guaranteed + Random Mix

Most dungeon loot tables follow a pattern: one guaranteed currency drop plus several weighted random monster/material drops.

```json
{
    "entries": [
        {"type": "currency", "count_min": 500, "count_max": 1500, "weight": 100, "guaranteed": true},
        {"type": "monster", "item_id": 16, "weight": 50, "guaranteed": false},
        {"type": "monster", "item_id": 101, "weight": 30, "guaranteed": false},
        {"type": "monster", "item_id": 102, "weight": 15, "guaranteed": false}
    ]
}
```

Players always get coins (500-1500 range), plus a weighted random chance at monsters. The common monster (weight 50) drops more often than the rare material (weight 15).

### Scaling with Difficulty

Higher-difficulty stages increase both the guaranteed currency range and the relative weight of rare drops:

| Stage | Guaranteed Coins | Rare Material Weight | Gem Drop Weight |
|---|---|---|---|
| Stage 1 | 500-1500 | 15 | 0 |
| Stage 3 | 700-1800 | 20 | 0 |
| Stage 5 | 1000-2500 | 20 | 8 |

Stage 5 introduces a small chance at premium currency (gems), which lower stages do not offer.

### Multiple Roll Counts

The `roll_count` parameter controls how many non-guaranteed rolls are made. This can be used for:

- **Standard clear**: `roll_count = 1` (one random drop)
- **S-rank clear**: `roll_count = 3` (three random drops for high performance)
- **Event bonus**: `roll_count = 2` (double drop event)

## Design Rationale

### Pure Functions

Like the gacha roller, `MFLootTable.roll_drops()` is a static pure function. It takes all inputs explicitly and returns all outputs. This makes it trivial to unit test with deterministic RNG seeds and to reuse across different game contexts (dungeon rewards, event rewards, daily login rewards).

### Separation from Dungeon

The loot module does not know about dungeons. The `DungeonRunner` has a `rewards` field on `DungeonDef`, but the actual loot rolling is done by game code that links the stage to its loot table. This keeps the modules independent.

### Guaranteed vs Random Separation

Guaranteed drops are processed first, before any random rolls. This ensures the player always gets the base reward regardless of RNG luck. The random rolls are additive -- they never replace guaranteed drops.
