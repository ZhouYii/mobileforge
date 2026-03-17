# Gacha Module

## Overview

The gacha module implements weighted random monster pulls with a pity system. It is split into three files with zero cross-dependencies beyond the types file:

| File | Purpose | Dependencies |
|---|---|---|
| `gacha_types` | GachaPool, GachaEntry, GachaResult data classes | None (leaf) |
| `gacha_roller` | Pure static functions for rolling pulls | gacha_types |
| `pity_tracker` | Per-pool pull counter state | None |

The gacha module has **no dependency** on Economy, MonsterManager, or any infrastructure module. It takes a pool definition and an RNG, and returns results. The game code is responsible for:
1. Checking if the player can afford the pull (via `MFEconomy.can_afford()`)
2. Spending the currency (via `MFEconomy.spend()`)
3. Creating monster instances from results (via `MFMonsterManager.create_instance()`)
4. Tracking pity state (via `MFPityTracker`)

## GachaPool Definition

A `GachaPool` is loaded from JSON and describes one summon banner:

```json
{
    "id": 1,
    "name": "Standard Pool",
    "cost_currency": "gems",
    "cost_amount": 5,
    "pity_threshold": 50,
    "entries": [
        {"monster_id": 1, "rarity": 5, "weight": 5, "is_featured": true},
        {"monster_id": 6, "rarity": 4, "weight": 15, "is_featured": false},
        {"monster_id": 16, "rarity": 3, "weight": 40, "is_featured": false}
    ]
}
```

### GachaPool Fields

| Field | Type | Description |
|---|---|---|
| `id` | `int` | Unique pool identifier |
| `name` | `String` | Display name for the banner |
| `cost_currency` | `String` | Currency type required (e.g., "gems") |
| `cost_amount` | `int` | Cost per single pull |
| `pity_threshold` | `int` | Guaranteed top-rarity pull after this many without one. 0 = no pity. |
| `featured_ids` | `Array[int]` | Monster IDs with rate-up (informational) |
| `entries` | `Array[GachaEntry]` | The weighted entry list |

### GachaEntry Fields

| Field | Type | Description |
|---|---|---|
| `monster_id` | `int` | Which monster this entry yields |
| `rarity` | `int` | Star rating of this monster |
| `weight` | `int` | Relative weight for weighted random selection |
| `is_featured` | `bool` | Whether this entry has rate-up |

## MFGachaRoller API

All methods are **static** and **pure** -- they take inputs and return outputs with no side effects.

### roll(pool, pity_count, rng) -> GachaResult

Perform a single pull.

**Algorithm:**
1. Check pity: if `pity_count >= pity_threshold - 1`, force a top-rarity result.
2. Otherwise, perform weighted random selection from all entries.
3. Return a `GachaResult` with the selected monster.

**Weighted random:** Sum all entry weights to get `total_weight`. Generate a random number in `[0, total_weight)`. Walk entries, accumulating weight. The first entry whose cumulative weight exceeds the roll is selected.

### roll_multi(pool, count, pity_count, rng) -> Array[GachaResult]

Perform multiple pulls in sequence. Pity counter increments after each non-top-rarity pull and resets after each top-rarity pull.

### get_displayed_rates(pool) -> Dictionary

Calculate the percentage drop rate per rarity tier. Returns `{rarity: float_percent}`.

**Example output for the standard pool:**
```
{3: 61.54, 4: 23.08, 5: 7.69}
```

These are the rates shown to players in the gacha UI (required by law in many jurisdictions).

## MFPityTracker

A simple state container that tracks how many pulls a player has made on each pool since their last top-rarity result.

### API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `get_pity(pool_id)` | `pool_id: int` | `int` | Current pity count for a pool |
| `increment(pool_id)` | `pool_id: int` | `void` | Add 1 to the counter |
| `reset(pool_id)` | `pool_id: int` | `void` | Set counter to 0 (after top-rarity pull) |
| `to_dict()` | | `Dictionary` | Serialize for save/load |
| `from_dict(data)` | `data: Dictionary` | `void` | Deserialize from save data |

### Persistence

The pity tracker is a pure state container. The game code is responsible for saving/loading it via `SaveManager`. Typical usage:

```gdscript
# Save
save_data["pity"] = pity_tracker.to_dict()

# Load
pity_tracker.from_dict(save_data.get("pity", {}))
```

## GachaResult

Output from a single pull:

| Field | Type | Description |
|---|---|---|
| `monster_id` | `int` | The monster definition ID obtained |
| `rarity` | `int` | Star rating of the monster |
| `is_pity` | `bool` | True if this was a guaranteed pity pull |
| `is_featured` | `bool` | True if the monster was on rate-up |

## Pity System Detail

The pity system guarantees a top-rarity result after a certain number of pulls without one. The algorithm:

1. Before each roll, check: `pity_count >= pity_threshold - 1`?
2. If yes: restrict the random selection to only entries with the highest rarity in the pool, then select among them by weight. Set `is_pity = true`.
3. If no: normal weighted random from all entries.

After each roll:
- If the result is top-rarity (pity or not), reset the counter to 0.
- Otherwise, increment the counter by 1.

### Example

Pool with `pity_threshold = 50`:
- Pulls 1-49: normal weighted random. Counter increments each time a non-top-rarity is pulled.
- Pull 50 (counter = 49): if still no top-rarity, this pull is guaranteed top-rarity.
- After getting top-rarity (at any point): counter resets to 0.

## Gacha Pull Flow

### 6-Step Pull Sequence

```
1. Player taps "Pull" or "Pull x10"
   │
2. Economy check: can_afford(pool.cost_currency, pool.cost_amount * count)?
   │── NO → PopupStack.show("insufficient_currency") → STOP
   │── YES → continue
   │
3. Determine rarity per pull:
   │── Check pity: pity_count >= pity_threshold - 1?
   │     YES → Force top rarity (restrict pool to max-rarity entries)
   │     NO  → Normal weighted random from all entries
   │
4. Weighted random select within rarity tier:
   │── Sum all eligible entry weights → total_weight
   │── Generate random in [0, total_weight)
   │── Walk entries, accumulate weight, select first exceeding roll
   │── Return GachaResult { monster_id, rarity, is_pity, is_featured }
   │
5. Create monster instances:
   │── For each GachaResult:
   │     MonsterManager.create_instance(result.monster_id)
   │     PlayerState.add_to_collection(instance)
   │     EventBus.emit("monster_added", { instance })
   │
6. Deduct currency and update pity:
   │── Economy.spend(pool.cost_currency, total_cost)
   │── EventBus emits "currency_changed" → CurrencyBar animates
   │── Update PityTracker: reset if top-rarity, increment otherwise
   │── SaveManager.mark_dirty() → auto-save queued
```

### Pity Counter Mechanics

| Pull # | Pity Counter | Result | Counter After |
|--------|-------------|--------|---------------|
| 1      | 0           | 3-star | 1             |
| 2      | 1           | 3-star | 2             |
| ...    | ...         | ...    | ...           |
| 25     | 24          | 5-star!| 0 (reset)     |
| 26     | 0           | 4-star | 1             |
| ...    | ...         | ...    | ...           |
| 49     | 48          | 3-star | 49            |
| 50     | 49          | 5-star!| 0 (forced)    |

### Rate Display Calculation

Displayed rates group entries by rarity and compute percentages:

```
total_weight = sum(entry.weight for all entries)
rate_per_rarity[r] = sum(entry.weight for entries where rarity == r) / total_weight * 100
```

## Code Examples

### Single Pull with Economy Check

```gdscript
var pool = MFGachaTypes.GachaPool.new(pool_data)
var rng = RandomNumberGenerator.new()

# Check affordability
if not economy.can_afford(pool.cost_currency, pool.cost_amount):
    print("Not enough ", pool.cost_currency)
    return

# Spend currency
economy.spend(pool.cost_currency, pool.cost_amount)

# Roll
var pity = pity_tracker.get_pity(pool.id)
var result = MFGachaRoller.roll(pool, pity, rng)

# Update pity
if result.rarity == _get_max_rarity(pool):
    pity_tracker.reset(pool.id)
else:
    pity_tracker.increment(pool.id)

# Create monster instance
var monster = monster_manager.create_instance(result.monster_id)
print("Pulled: ", monster.def_id, " (pity: ", result.is_pity, ")")
```

### Multi-Pull (10-pull)

```gdscript
var total_cost = pool.cost_amount * 10
if not economy.can_afford(pool.cost_currency, total_cost):
    return

economy.spend(pool.cost_currency, total_cost)

var pity = pity_tracker.get_pity(pool.id)
var results = MFGachaRoller.roll_multi(pool, 10, pity, rng)

# Update pity based on final state
var current_pity = pity
for result in results:
    if result.rarity == max_rarity:
        current_pity = 0
    else:
        current_pity += 1
pity_tracker.from_dict({pool.id: current_pity})

# Create monsters from results
for result in results:
    var monster = monster_manager.create_instance(result.monster_id)
    # Add to player inventory...
```

### Displaying Rates

```gdscript
var rates = MFGachaRoller.get_displayed_rates(pool)
for rarity in rates:
    print("%d-star: %.2f%%" % [rarity, rates[rarity]])
# Output:
# 3-star: 61.54%
# 4-star: 23.08%
# 5-star: 7.69%
```

## Design Rationale

The gacha module is deliberately decoupled from economy and inventory. This separation allows:

1. **Testing without state**: `MFGachaRoller` is pure static functions with injected RNG, so pull distributions can be validated deterministically.
2. **Flexibility**: Different games can wrap the roller with different economy checks, multi-pull discounts, or bonus mechanics.
3. **Compliance**: `get_displayed_rates()` provides the exact data needed for gacha rate disclosure requirements.
