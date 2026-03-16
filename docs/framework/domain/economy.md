# Economy Module

## Overview

The economy module manages in-game currencies and stamina. It provides a thin API layer over `PlayerState` for currency operations, plus a timer-based stamina refill system. The module emits events via `EventBus` when currency values change.

| File | Purpose | Dependencies |
|---|---|---|
| `economy_types` | CurrencyType, StaminaConfig data classes | None (leaf) |
| `economy` | MFEconomy: currency spend/earn/check, stamina management | PlayerState (injected), EventBus (injected) |
| `stamina_timer` | MFStaminaTimer: time-based stamina regeneration calculator | economy_types (StaminaConfig) |

## MFEconomy API

`MFEconomy` is a `RefCounted` (not a Node). It receives `PlayerState` and `EventBus` references via constructor injection.

### Constructor

```gdscript
var economy = MFEconomy.new(player_state, event_bus)
```

Both parameters are optional (can be null for testing). If `player_state` is null, all balance checks return 0 and all spend operations return false.

### Currency Operations

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `can_afford(currency, amount)` | `currency: String, amount: int` | `bool` | Check if balance >= amount |
| `spend(currency, amount)` | `currency: String, amount: int` | `bool` | Deduct amount if affordable. Returns false if insufficient. |
| `earn(currency, amount)` | `currency: String, amount: int` | `void` | Add amount to balance |
| `get_balance(currency)` | `currency: String` | `int` | Current balance for the currency |

All currency operations read from and write to the `currencies` section of `PlayerState` using `StringName` keys.

### Stamina Operations

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `check_stamina(cost)` | `cost: int` | `bool` | Shorthand for `get_balance("stamina") >= cost` |
| `spend_stamina(cost)` | `cost: int` | `bool` | Shorthand for `spend("stamina", cost)` |
| `setup_stamina(config)` | `config: StaminaConfig` | `void` | Initialize the stamina timer |

### Event Emission

Every `spend()` and `earn()` call emits a `CURRENCY_CHANGED` event via `EventBus` (if available):

```gdscript
{
    "currency_type": "gems",      # Which currency changed
    "old_value": 50,              # Balance before the change
    "new_value": 45,              # Balance after the change
}
```

This allows UI elements (e.g., `CurrencyBar`) to react to balance changes without polling.

## CurrencyType

Defines metadata for a currency type:

| Field | Type | Description |
|---|---|---|
| `id` | `String` | Internal key (e.g., "gems", "coins", "stamina") |
| `name` | `String` | Display name |
| `max_amount` | `int` | Maximum balance cap. -1 = unlimited. |

### Common Currency Types in ToS

| ID | Name | Typical Initial | Max |
|---|---|---|---|
| `gems` | Diamonds/Gems | 50 | Unlimited |
| `coins` | Gold Coins | 10000 | Unlimited |
| `stamina` | Stamina | 100 | Varies by rank |

## StaminaConfig

Configuration for the stamina regeneration timer:

| Field | Type | Default | Description |
|---|---|---|---|
| `max_stamina` | `int` | 100 | Maximum stamina capacity |
| `refill_rate_seconds` | `float` | 300.0 | Seconds per 1 stamina point (5 minutes default) |
| `refill_cost_currency` | `String` | "gems" | Currency used to buy instant refill |
| `refill_cost_amount` | `int` | 1 | Cost for instant refill |

### Example Configuration

```gdscript
var config = MFEconomyTypes.StaminaConfig.new(
    100,      # max_stamina
    300.0,    # refill_rate_seconds (1 point per 5 minutes)
    "gems",   # refill_cost_currency
    1         # refill_cost_amount
)
economy.setup_stamina(config)
```

## MFStaminaTimer

A pure calculator that determines how much stamina has regenerated based on elapsed time. It does not tick automatically -- the game code must call `calculate_refill()` when it needs the current stamina value.

### API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `calculate_refill(current_stamina, current_time)` | `int, float` | `Dictionary` | Calculate regen since last update |
| `seconds_until_next(current_stamina, current_time)` | `int, float` | `float` | Time until next stamina point |
| `set_last_update(time)` | `float` | `void` | Set the reference timestamp |

### calculate_refill Return Format

```gdscript
{
    "stamina": 85,              # New stamina value after regen
    "remainder_seconds": 120.0  # Partial progress toward next point
}
```

### Refill Calculation

```
elapsed = current_time - last_update_time
points_gained = floor(elapsed / refill_rate_seconds)
new_stamina = min(current_stamina + points_gained, max_stamina)
remainder = elapsed mod refill_rate_seconds
```

The remainder is preserved so that partial progress is not lost between calls.

### seconds_until_next

Returns how many seconds until the next stamina point regenerates. Returns 0 if stamina is already at max.

```gdscript
var seconds = stamina_timer.seconds_until_next(current_stamina, Time.get_unix_time_from_system())
# Use this to display "Next stamina in: 2:45" in the UI
```

## Code Examples

### Initializing the Economy

```gdscript
# In game _ready()
var economy = MFEconomy.new(player_state, event_bus)

# Register initial currency balances via PlayerState
player_state.register_section(&"currencies", {
    "gems": 50,
    "coins": 10000,
    "stamina": 100,
})

# Setup stamina timer
var stamina_config = MFEconomyTypes.StaminaConfig.new(100, 300.0, "gems", 1)
economy.setup_stamina(stamina_config)
```

### Checking and Spending Currency

```gdscript
# Before gacha pull
if economy.can_afford("gems", 5):
    economy.spend("gems", 5)
    # Proceed with pull
else:
    # Show "not enough gems" popup
    pass

# After dungeon clear
economy.earn("coins", 1500)
```

### Stamina Check Before Dungeon Entry

```gdscript
var stamina_cost = stage_def.get_int(&"stamina_cost", 10)
if economy.check_stamina(stamina_cost):
    economy.spend_stamina(stamina_cost)
    # Enter dungeon
else:
    # Show "not enough stamina" with refill option
    var timer = economy._stamina_timer
    var seconds = timer.seconds_until_next(
        economy.get_balance("stamina"),
        Time.get_unix_time_from_system()
    )
    print("Next stamina in %.0f seconds" % seconds)
```

### Responding to Currency Changes in UI

```gdscript
# In a UI component
func _ready() -> void:
    event_bus.connect_event(EventNames.CURRENCY_CHANGED, _on_currency_changed)

func _on_currency_changed(data: Dictionary) -> void:
    if data["currency_type"] == "gems":
        gem_label.text = str(data["new_value"])
```

## Design Rationale

### Why Not a Singleton?

`MFEconomy` is a `RefCounted`, not an autoload singleton. This allows:
- Multiple economy instances in tests
- Dependency injection (no global state)
- Game code controls the lifecycle

### Why Separate from PlayerState?

PlayerState is a generic key-value store. Economy adds domain-specific semantics:
- `can_afford` + `spend` as an atomic check-and-deduct pattern
- Stamina-specific timer logic
- Event emission on every balance change
- Potential for max balance caps, daily limits, etc.

The economy module is the **domain interface** for currency operations. Game code should always go through `MFEconomy` rather than modifying PlayerState's `currencies` section directly, to ensure events are emitted and invariants are maintained.
