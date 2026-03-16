# PlayerState

## Purpose

PlayerState holds all mutable player data at runtime. It organizes data into named sections (currencies, inventory, settings, progression, etc.) and emits events through EventBus whenever values change. This is the single source of truth for "what does the player have right now."

PlayerState is not responsible for persistence — SaveManager reads from and writes to PlayerState using `to_save_dict()` and `from_save_dict()`.

## Section Model

Data is organized into flat key-value sections. Each section is a string-keyed namespace.

```
PlayerState
├── currencies
│   ├── gold: 1500
│   ├── gems: 23
│   └── stamina: 80
├── inventory
│   ├── sword_01: 1
│   ├── potion_hp_01: 15
│   └── shield_01: 1
├── progression
│   ├── level: 12
│   ├── xp: 4500
│   └── chapter: 3
└── settings
    ├── music_volume: 0.8
    ├── sfx_volume: 1.0
    └── language: "en"
```

Sections are created on first use. You do not need to pre-register them, but it is good practice to do so at startup for clarity.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `register_section(section: StringName)` | section name | `void` | Pre-register an empty section |
| `set_value(section: StringName, key: StringName, value: Variant)` | section, key, value | `void` | Set a value (emits `state_changed`) |
| `get_value(section: StringName, key: StringName, default: Variant = null)` | section, key, default | `Variant` | Get a value, returning default if missing |
| `has_value(section: StringName, key: StringName)` | section, key | `bool` | Check if key exists in section |
| `remove_value(section: StringName, key: StringName)` | section, key | `void` | Remove a key (emits `state_changed`) |
| `get_section(section: StringName)` | section name | `Dictionary` | Get a copy of all key-value pairs in a section |
| `get_section_keys(section: StringName)` | section name | `Array[StringName]` | List all keys in a section |
| `has_section(section: StringName)` | section name | `bool` | Check if section exists |
| `clear_section(section: StringName)` | section name | `void` | Remove all keys in a section |
| `to_save_dict()` | none | `Dictionary` | Serialize all sections to a save-friendly dict |
| `from_save_dict(data: Dictionary)` | save dict | `void` | Restore all sections from a save dict (emits `state_loaded`) |

### Unity (C#)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `RegisterSection(string section)` | section name | `void` | Pre-register an empty section |
| `SetValue(string section, string key, object value)` | section, key, value | `void` | Set a value (emits `state_changed`) |
| `GetValue<T>(string section, string key, T defaultValue = default)` | section, key, default | `T` | Get a typed value with default |
| `HasValue(string section, string key)` | section, key | `bool` | Check key existence |
| `RemoveValue(string section, string key)` | section, key | `void` | Remove a key (emits `state_changed`) |
| `GetSection(string section)` | section name | `Dictionary<string, object>` | Copy of section data |
| `GetSectionKeys(string section)` | section name | `List<string>` | All keys in section |
| `HasSection(string section)` | section name | `bool` | Check section existence |
| `ClearSection(string section)` | section name | `void` | Remove all keys |
| `ToSaveDict()` | none | `Dictionary<string, object>` | Serialize for saving |
| `FromSaveDict(Dictionary<string, object> data)` | save dict | `void` | Restore from save (emits `state_loaded`) |

## Event Emissions

### `state_changed`

Emitted every time `set_value()` or `remove_value()` is called.

**Payload (ValueChangedEvent):**

| Key         | Type       | Description                         |
|-------------|------------|-------------------------------------|
| `section`   | `string`   | Section name                        |
| `key`       | `string`   | Key that changed                    |
| `old_value` | `Variant` / `object` | Previous value (null if new key) |
| `new_value` | `Variant` / `object` | New value (null if removed)      |

```gdscript
# Godot — emitted internally
EventBus.emit("state_changed", {
    "section": "currencies",
    "key": "gold",
    "old_value": 1500,
    "new_value": 1600
})
```

```csharp
// Unity — emitted internally
EventBus.Instance.Emit("state_changed", new Dictionary<string, object> {
    { "section", "currencies" },
    { "key", "gold" },
    { "old_value", 1500 },
    { "new_value", 1600 }
});
```

### `state_loaded`

Emitted when `from_save_dict()` completes. Tells all subscribers to re-read their state.

**Payload:**

| Key        | Type            | Description                           |
|------------|-----------------|---------------------------------------|
| `sections` | `Array` / `List<string>` | Names of all sections that were loaded |

## Save/Load Integration

PlayerState provides two methods for SaveManager to use:

### `to_save_dict()`

Returns a nested dictionary suitable for JSON serialization:

```json
{
    "currencies": {
        "gold": 1500,
        "gems": 23,
        "stamina": 80
    },
    "inventory": {
        "sword_01": 1,
        "potion_hp_01": 15
    },
    "progression": {
        "level": 12,
        "xp": 4500
    }
}
```

### `from_save_dict(data)`

Replaces all sections with the contents of the provided dictionary. Existing data is cleared first. After loading, emits `state_loaded` so all subscribers can refresh.

```gdscript
# Godot — SaveManager does this internally
var save_data = JSON.parse_string(file_contents)
PlayerState.from_save_dict(save_data)
# state_loaded event fires, all UI refreshes
```

```csharp
// Unity — SaveManager does this internally
var saveData = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContents);
PlayerState.Instance.FromSaveDict(saveData);
// state_loaded event fires, all UI refreshes
```

## Usage Examples

### Basic Read/Write

```gdscript
# Godot
func _ready():
    # Register sections at startup
    PlayerState.register_section("currencies")
    PlayerState.register_section("inventory")
    PlayerState.register_section("progression")

    # Set values
    PlayerState.set_value("currencies", "gold", 500)
    PlayerState.set_value("progression", "level", 1)
    PlayerState.set_value("progression", "xp", 0)

    # Read values
    var gold = PlayerState.get_value("currencies", "gold", 0)
    var level = PlayerState.get_value("progression", "level", 1)
    print("Level %d with %d gold" % [level, gold])
```

```csharp
// Unity
void Start()
{
    PlayerState.Instance.RegisterSection("currencies");
    PlayerState.Instance.RegisterSection("inventory");
    PlayerState.Instance.RegisterSection("progression");

    PlayerState.Instance.SetValue("currencies", "gold", 500);
    PlayerState.Instance.SetValue("progression", "level", 1);
    PlayerState.Instance.SetValue("progression", "xp", 0);

    int gold = PlayerState.Instance.GetValue<int>("currencies", "gold", 0);
    int level = PlayerState.Instance.GetValue<int>("progression", "level", 1);
    Debug.Log($"Level {level} with {gold} gold");
}
```

### Subscribing to Changes

```gdscript
# Godot
func _ready():
    EventBus.subscribe("state_changed", _on_state_changed)

func _on_state_changed(event: Dictionary):
    if event["section"] == "currencies" and event["key"] == "gold":
        gold_label.text = str(event["new_value"])
```

```csharp
// Unity
void Start()
{
    EventBus.Instance.Subscribe("state_changed", OnStateChanged);
}

void OnStateChanged(Dictionary<string, object> evt)
{
    if ((string)evt["section"] == "currencies" && (string)evt["key"] == "gold")
    {
        goldLabel.text = evt["new_value"].ToString();
    }
}
```

### Typed Wrapper Pattern

For frequently accessed state, wrap PlayerState calls in a helper class:

```gdscript
# Godot
class_name CurrencyHelper

static func get_gold() -> int:
    return PlayerState.get_value("currencies", "gold", 0)

static func add_gold(amount: int) -> void:
    var current = get_gold()
    PlayerState.set_value("currencies", "gold", current + amount)

static func can_afford_gold(cost: int) -> bool:
    return get_gold() >= cost

static func spend_gold(cost: int) -> bool:
    if not can_afford_gold(cost):
        return false
    add_gold(-cost)
    return true
```

```csharp
// Unity
namespace MobileForge.Domain
{
    public static class CurrencyHelper
    {
        public static int GetGold()
            => PlayerState.Instance.GetValue<int>("currencies", "gold", 0);

        public static void AddGold(int amount)
            => PlayerState.Instance.SetValue("currencies", "gold", GetGold() + amount);

        public static bool CanAffordGold(int cost)
            => GetGold() >= cost;

        public static bool SpendGold(int cost)
        {
            if (!CanAffordGold(cost)) return false;
            AddGold(-cost);
            return true;
        }
    }
}
```

### Reacting to Save Load

```gdscript
# Godot
func _ready():
    EventBus.subscribe("state_loaded", _on_state_loaded)

func _on_state_loaded(event: Dictionary):
    # Refresh all UI elements from current state
    var gold = PlayerState.get_value("currencies", "gold", 0)
    var level = PlayerState.get_value("progression", "level", 1)
    refresh_hud(gold, level)
```

```csharp
// Unity
void Start()
{
    EventBus.Instance.Subscribe("state_loaded", OnStateLoaded);
}

void OnStateLoaded(Dictionary<string, object> evt)
{
    int gold = PlayerState.Instance.GetValue<int>("currencies", "gold", 0);
    int level = PlayerState.Instance.GetValue<int>("progression", "level", 1);
    RefreshHUD(gold, level);
}
```

## Best Practices

1. **Register sections at startup.** Call `register_section()` for all known sections in your bootstrap code. This documents the state schema and avoids typos.

2. **Use typed wrappers for frequent access.** Raw `get_value("currencies", "gold", 0)` calls scattered through the codebase are brittle. Wrap them in domain helpers like `CurrencyHelper.GetGold()`.

3. **Listen to `state_changed` for reactive UI.** Don't poll PlayerState every frame. Subscribe to `state_changed` and filter by section/key.

4. **Listen to `state_loaded` for full refreshes.** After a save is loaded, every piece of UI needs to re-read its values. `state_loaded` is your signal to do so.

5. **Don't store engine objects.** PlayerState values must be JSON-serializable: numbers, strings, booleans, arrays, dictionaries. No Nodes, no GameObjects, no textures.

6. **Keep sections focused.** One section per concern. Don't dump everything into a single "player" section.

7. **Default values matter.** Always provide a sensible default to `get_value()`. A missing key should not crash the game — it should return a safe fallback.
