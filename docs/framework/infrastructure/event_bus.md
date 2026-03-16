# EventBus

## Purpose

EventBus is the pub/sub message broker at the heart of MobileForge. It decouples modules so they communicate through named events rather than direct references. Lower layers emit events; upper layers subscribe to them. This enforces the "no upward calls" rule without sacrificing responsiveness.

## Design Rationale

- **Single global instance.** One bus per game. Simple, predictable, easy to test.
- **String-keyed events.** Event names are `snake_case` strings, identical in both engines. No typed signal declarations needed.
- **Dictionary payloads.** Every event carries a `Dictionary` of key-value pairs. Flexible, serializable, and schema-validatable.
- **Synchronous dispatch.** Handlers run immediately when `emit()` is called, in subscription order. No deferred queue, no threading.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `subscribe(event_name: StringName, callback: Callable)` | event name, handler function | `void` | Register a handler for the named event |
| `unsubscribe(event_name: StringName, callback: Callable)` | event name, handler function | `void` | Remove a previously registered handler |
| `emit(event_name: StringName, payload: Dictionary)` | event name, data dict | `void` | Fire the event, calling all subscribers in order |
| `has_subscribers(event_name: StringName)` | event name | `bool` | Check if any handlers are registered |
| `clear(event_name: StringName)` | event name | `void` | Remove all handlers for one event |
| `clear_all()` | none | `void` | Remove all handlers for all events |

### Unity (C#)

| Method | Params | Return | Description |
|--------|--------|--------|-------------|
| `Subscribe(string eventName, Action<Dictionary<string, object>> callback)` | event name, handler | `void` | Register a handler |
| `Unsubscribe(string eventName, Action<Dictionary<string, object>> callback)` | event name, handler | `void` | Remove a handler |
| `Emit(string eventName, Dictionary<string, object> payload)` | event name, data | `void` | Fire the event |
| `HasSubscribers(string eventName)` | event name | `bool` | Check if handlers exist |
| `Clear(string eventName)` | event name | `void` | Remove all handlers for one event |
| `ClearAll()` | none | `void` | Remove all handlers |

## Usage Examples

### Subscribe and Handle

```gdscript
# Godot
extends Node

func _ready():
    EventBus.subscribe("level_up", _on_level_up)
    EventBus.subscribe("currency_changed", _on_currency_changed)

func _exit_tree():
    EventBus.unsubscribe("level_up", _on_level_up)
    EventBus.unsubscribe("currency_changed", _on_currency_changed)

func _on_level_up(event: Dictionary):
    var new_level = event["new_level"]
    print("Reached level %d!" % new_level)

func _on_currency_changed(event: Dictionary):
    var currency = event["currency_id"]
    var amount = event["new_amount"]
    update_display(currency, amount)
```

```csharp
// Unity
using MobileForge.Infrastructure;
using System.Collections.Generic;
using UnityEngine;

public class LevelDisplay : MonoBehaviour
{
    void Start()
    {
        EventBus.Instance.Subscribe("level_up", OnLevelUp);
        EventBus.Instance.Subscribe("currency_changed", OnCurrencyChanged);
    }

    void OnDestroy()
    {
        EventBus.Instance.Unsubscribe("level_up", OnLevelUp);
        EventBus.Instance.Unsubscribe("currency_changed", OnCurrencyChanged);
    }

    void OnLevelUp(Dictionary<string, object> evt)
    {
        int newLevel = (int)evt["new_level"];
        Debug.Log($"Reached level {newLevel}!");
    }

    void OnCurrencyChanged(Dictionary<string, object> evt)
    {
        string currency = (string)evt["currency_id"];
        int amount = (int)evt["new_amount"];
        UpdateDisplay(currency, amount);
    }
}
```

### Emit Events

```gdscript
# Godot — from a domain service
EventBus.emit("level_up", {
    "old_level": 4,
    "new_level": 5,
    "unlocked_features": ["gacha", "guild"]
})
```

```csharp
// Unity — from a domain service
EventBus.Instance.Emit("level_up", new Dictionary<string, object> {
    { "old_level", 4 },
    { "new_level", 5 },
    { "unlocked_features", new List<object> { "gacha", "guild" } }
});
```

## Event Naming Conventions

- Always `snake_case`, even in C#. This keeps event names identical across engines and in JSON test vectors.
- Use past tense for things that happened: `item_acquired`, `quest_completed`, `level_up`.
- Use present tense for requests: `save_requested`, `purchase_requested`.
- Prefix with module name for module-specific events: `gacha_pull_completed`, `inventory_full`.

Define all event names as constants in a central file:

```gdscript
# Godot — event_names.gd
class_name EventNames

const STATE_CHANGED = &"state_changed"
const STATE_LOADED = &"state_loaded"
const LEVEL_UP = &"level_up"
const CURRENCY_CHANGED = &"currency_changed"
const ITEM_ACQUIRED = &"item_acquired"
const ITEM_REMOVED = &"item_removed"
const QUEST_COMPLETED = &"quest_completed"
const GACHA_PULL_COMPLETED = &"gacha_pull_completed"
const SAVE_COMPLETED = &"save_completed"
const DATA_LOADED = &"data_loaded"
```

```csharp
// Unity — EventNames.cs
namespace MobileForge.Infrastructure
{
    public static class EventNames
    {
        public const string StateChanged = "state_changed";
        public const string StateLoaded = "state_loaded";
        public const string LevelUp = "level_up";
        public const string CurrencyChanged = "currency_changed";
        public const string ItemAcquired = "item_acquired";
        public const string ItemRemoved = "item_removed";
        public const string QuestCompleted = "quest_completed";
        public const string GachaPullCompleted = "gacha_pull_completed";
        public const string SaveCompleted = "save_completed";
        public const string DataLoaded = "data_loaded";
    }
}
```

## Framework Event List

| Event Name              | Emitted By    | Payload Keys                                        |
|-------------------------|---------------|-----------------------------------------------------|
| `state_changed`         | PlayerState   | `section`, `key`, `old_value`, `new_value`          |
| `state_loaded`          | PlayerState   | `sections` (array of section names)                 |
| `data_loaded`           | GameData      | `type`, `count`                                     |
| `save_completed`        | SaveManager   | `success`, `path`                                   |
| `level_up`              | ProgressionService | `old_level`, `new_level`, `unlocked_features`  |
| `currency_changed`      | CurrencyService | `currency_id`, `old_amount`, `new_amount`, `delta` |
| `item_acquired`         | InventoryService | `item_id`, `quantity`, `source`                  |
| `item_removed`          | InventoryService | `item_id`, `quantity`, `reason`                  |
| `quest_completed`       | QuestService  | `quest_id`, `rewards`                               |
| `gacha_pull_completed`  | GachaService  | `banner_id`, `results` (array of item dicts)        |

## Thread Safety

EventBus is designed for single-threaded use. Both Godot and Unity run game logic on the main thread, so no locks or synchronization are needed.

Do not call `emit()` from a background thread. If you need to emit from an async operation (e.g., network response), marshal back to the main thread first:

```gdscript
# Godot — use call_deferred
call_deferred("_emit_on_main_thread", event_name, payload)
```

```csharp
// Unity — use main thread dispatcher or UnityMainThreadDispatcher
UnityMainThreadDispatcher.Instance.Enqueue(() => {
    EventBus.Instance.Emit(eventName, payload);
});
```

## Testing

Create a fresh EventBus instance per test to avoid cross-test contamination:

```gdscript
# Godot test
func test_subscribe_and_emit():
    var bus = EventBusClass.new()
    var received = []
    bus.subscribe("test_event", func(e): received.append(e))

    bus.emit("test_event", {"value": 42})

    assert_eq(received.size(), 1)
    assert_eq(received[0]["value"], 42)
```

```csharp
// Unity test
[Test]
public void SubscribeAndEmit()
{
    var bus = new EventBus();
    var received = new List<Dictionary<string, object>>();
    bus.Subscribe("test_event", (e) => received.Add(e));

    bus.Emit("test_event", new Dictionary<string, object> { { "value", 42 } });

    Assert.AreEqual(1, received.Count);
    Assert.AreEqual(42, received[0]["value"]);
}
```

## Best Practices

1. **Subscribe in `_ready()` / `Start()`.** This ensures the handler is registered before any events fire during gameplay.
2. **Always unsubscribe in `_exit_tree()` / `OnDestroy()`.** Leaked subscriptions cause errors when destroyed objects receive events.
3. **Keep handlers fast.** Emit is synchronous. A slow handler blocks the entire dispatch chain.
4. **Don't emit inside a handler for the same event.** This causes re-entrant dispatch. If you need chain reactions, emit a different event.
5. **Use EventNames constants.** Never use raw string literals for event names — typos are silent bugs.
6. **One concern per handler.** If a handler does two things, split it into two subscriptions.
