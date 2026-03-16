# ToastLayer

## Purpose

ToastLayer displays non-blocking notification banners at the top or bottom of the screen. Toasts auto-dismiss after a configurable duration and do not block input to content below them. They are used for transient feedback like "Item acquired!", "Quest completed!", or error messages.

## Design Rationale

- **Non-blocking.** Unlike popups, toasts do not require user interaction. The player can continue playing while the toast is visible.
- **Auto-dismiss.** Each toast has a duration (default 3 seconds). No manual dismiss is needed.
- **Stacking.** Multiple toasts can appear simultaneously, stacking vertically. Oldest toasts slide out as new ones arrive.
- **Highest z-order.** Toasts render above popups and overlays so they are always visible.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `show_toast(message: String, duration: float, type: String)` | text, seconds, style type | `void` | Show a toast notification |
| `show_toast_custom(scene: Control, duration: float)` | custom control, seconds | `void` | Show a custom toast with arbitrary content |
| `clear_all()` | none | `void` | Immediately remove all visible toasts |
| `set_position(pos: ToastPosition)` | TOP or BOTTOM | `void` | Set where toasts appear |
| `set_max_visible(count: int)` | max count | `void` | Limit simultaneous visible toasts |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `ShowToast(string message, float duration, string type)` | text, seconds, style type | `void` | Show a toast notification |
| `ShowToastCustom(GameObject content, float duration)` | custom object, seconds | `void` | Show a custom toast |
| `ClearAll()` | none | `void` | Remove all visible toasts |
| `SetPosition(ToastPosition pos)` | TOP or BOTTOM | `void` | Set toast position |
| `SetMaxVisible(int count)` | max count | `void` | Limit simultaneous toasts |

## Toast Types

| Type | Color | Use Case |
|---|---|---|
| `"info"` | Blue / neutral | General information |
| `"success"` | Green | Positive outcome (item acquired, quest complete) |
| `"warning"` | Yellow / orange | Non-critical issue (inventory almost full) |
| `"error"` | Red | Error condition (network failed, invalid action) |

## Usage Examples

```gdscript
# Godot
ToastLayer.show_toast("Item acquired: Iron Sword", 3.0, "success")
ToastLayer.show_toast("Connection lost. Retrying...", 5.0, "error")
ToastLayer.show_toast("Daily login bonus collected!", 3.0, "info")
```

```csharp
// Unity
ToastLayer.ShowToast("Item acquired: Iron Sword", 3.0f, "success");
ToastLayer.ShowToast("Connection lost. Retrying...", 5.0f, "error");
ToastLayer.ShowToast("Daily login bonus collected!", 3.0f, "info");
```

## Stacking Behavior

When multiple toasts are active, they stack from the anchor edge:

```
TOP position:                    BOTTOM position:
┌─────────────────────┐         │                     │
│  Toast 1 (oldest)   │         │     Game content    │
├─────────────────────┤         │                     │
│  Toast 2            │         ├─────────────────────┤
├─────────────────────┤         │  Toast 2            │
│  Toast 3 (newest)   │         ├─────────────────────┤
├─────────────────────┤         │  Toast 1 (oldest)   │
│     Game content    │         └─────────────────────┘
```

When `max_visible` is reached, the oldest toast is immediately removed to make room for the new one.

## Animation

Toasts animate in and out:

1. **Enter**: Slide in from the edge + fade in (0.3s)
2. **Visible**: Hold for the specified duration
3. **Exit**: Slide out + fade out (0.3s)

The animation durations are configurable:

```gdscript
# Godot
ToastLayer.enter_duration = 0.3
ToastLayer.exit_duration = 0.3
```

```csharp
// Unity
ToastLayer.EnterDuration = 0.3f;
ToastLayer.ExitDuration = 0.3f;
```

## Event-Driven Toasts

Toasts often respond to events from the domain layer:

```gdscript
# Godot
func _ready() -> void:
    EventBus.subscribe("item_acquired", _on_item_acquired)
    EventBus.subscribe("quest_completed", _on_quest_completed)

func _on_item_acquired(event: Dictionary) -> void:
    var item_name: String = event.get("item_name", "Unknown")
    ToastLayer.show_toast("Acquired: %s" % item_name, 3.0, "success")

func _on_quest_completed(event: Dictionary) -> void:
    var quest_name: String = event.get("quest_name", "Unknown")
    ToastLayer.show_toast("Quest Complete: %s" % quest_name, 4.0, "success")
```

## Best Practices

1. **Keep messages short.** Toasts are transient. One line of text is ideal.
2. **Use appropriate types.** Color-coded types help players quickly identify the nature of the notification.
3. **Do not use toasts for critical decisions.** If the player needs to act, use a popup instead.
4. **Limit max visible to 3.** More than 3 stacked toasts become unreadable.
5. **Set reasonable durations.** 2-4 seconds for simple messages, 5+ seconds for error messages the player may need to read.
