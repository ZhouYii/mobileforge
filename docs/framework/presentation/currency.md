# CurrencyBar

## Purpose

CurrencyBar is a HUD component that displays a player's currency amount with live-binding to PlayerState changes. When the currency value changes (via `CurrencyService` emitting a `currency_changed` event), the bar updates automatically with an animated count-up or count-down effect.

## Design Rationale

- **Event-driven.** CurrencyBar subscribes to `currency_changed` events. It never polls PlayerState.
- **Animated transitions.** When the amount changes, the display smoothly interpolates from the old value to the new value, providing satisfying feedback.
- **Reusable.** A single CurrencyBar instance can display any currency type (gold, gems, stamina, etc.) by configuring its `currency_id`.
- **Self-contained.** CurrencyBar creates its own icon and label. No scene file needed.

## API Reference

### Godot (GDScript)

| Method / Property | Params | Return | Description |
|---|---|---|---|
| `new(currency_id: String)` | currency ID | `CurrencyBar` | Create a bar for the given currency |
| `currency_id` | -- | `String` | The currency this bar tracks |
| `current_amount` | -- | `int` | The current displayed amount |
| `set_icon(texture: Texture2D)` | icon texture | `void` | Set the currency icon |
| `set_format(fmt: String)` | format string | `void` | Set display format (e.g., `"%d"`, `"%,d"`) |
| `refresh()` | none | `void` | Force re-read from PlayerState and update |
| `set_animate(enabled: bool)` | true/false | `void` | Enable or disable count animation |
| `set_animation_duration(seconds: float)` | duration | `void` | Set count animation duration |

### Unity (C#)

| Method / Property | Params | Return | Description |
|---|---|---|---|
| `CurrencyBar(string currencyId)` | currency ID | -- | Constructor |
| `CurrencyId` | -- | `string` | The currency this bar tracks |
| `CurrentAmount` | -- | `int` | The current displayed amount |
| `SetIcon(Sprite icon)` | icon sprite | `void` | Set the currency icon |
| `SetFormat(string fmt)` | format string | `void` | Set display format |
| `Refresh()` | none | `void` | Force refresh |
| `AnimateEnabled` | -- | `bool` | Enable/disable animation |
| `AnimationDuration` | -- | `float` | Animation duration in seconds |

## Live-Binding Flow

```
CurrencyService.spend("gold", 50)
        │
        v
EventBus.emit("currency_changed", {
    "currency_id": "gold",
    "old_amount": 500,
    "new_amount": 450,
    "delta": -50
})
        │
        v
CurrencyBar._on_currency_changed(event)
        │
        v
Animate display: 500 -> 450 over 0.3s
```

## Usage Example

```gdscript
# Godot -- add currency bars to HUD
var gold_bar := CurrencyBar.new("gold")
gold_bar.set_icon(preload("res://assets/icons/gold.png"))
gold_bar.set_format("%,d")
gold_bar.set_animation_duration(0.3)
hud_container.add_child(gold_bar)

var gems_bar := CurrencyBar.new("gems")
gems_bar.set_icon(preload("res://assets/icons/gem.png"))
gems_bar.set_format("%,d")
hud_container.add_child(gems_bar)

var stamina_bar := CurrencyBar.new("stamina")
stamina_bar.set_icon(preload("res://assets/icons/stamina.png"))
stamina_bar.set_format("%d / %d")  # current / max
hud_container.add_child(stamina_bar)
```

```csharp
// Unity
var goldBar = new CurrencyBar("gold");
goldBar.SetIcon(Resources.Load<Sprite>("Icons/gold"));
goldBar.SetFormat("{0:N0}");
goldBar.AnimationDuration = 0.3f;
hudContainer.AddChild(goldBar);

var gemsBar = new CurrencyBar("gems");
gemsBar.SetIcon(Resources.Load<Sprite>("Icons/gem"));
gemsBar.SetFormat("{0:N0}");
hudContainer.AddChild(gemsBar);
```

## Animation Details

The count animation uses linear interpolation between the old and new values:

```
Frame update (each frame during animation):
    elapsed += delta_time
    t = clamp(elapsed / animation_duration, 0.0, 1.0)
    displayed_value = lerp(old_amount, new_amount, t)
    label.text = format % displayed_value
```

For large changes (e.g., 0 -> 10,000 on first load), animation can be disabled:

```gdscript
# Godot
currency_bar.set_animate(false)
currency_bar.refresh()  # instant update
currency_bar.set_animate(true)
```

## Insufficient Funds Flash

When the player attempts to spend currency they do not have, CurrencyBar can flash red:

```gdscript
# Godot
func _on_purchase_failed(event: Dictionary) -> void:
    if event.get("reason") == "insufficient_funds":
        currency_bar.flash_insufficient()
```

The flash animation briefly tints the amount label red and shakes it, providing clear feedback without a popup.

## Layout

CurrencyBar creates this internal structure:

```
CurrencyBar (HBoxContainer / HorizontalLayoutGroup)
├── Icon (TextureRect / Image) -- 32x32 currency icon
└── AmountLabel (Label / TextMeshProUGUI) -- formatted amount text
```

## Best Practices

1. **One CurrencyBar per currency type.** Do not reuse a single bar for multiple currencies.
2. **Place bars in a persistent HUD.** They should be visible across multiple screens (not destroyed on screen transitions).
3. **Use `refresh()` after loading a save.** The initial state may not trigger a `currency_changed` event.
4. **Keep animation duration short.** 0.2-0.5 seconds feels snappy. Longer feels sluggish.
5. **Disable animation for large jumps.** When loading a save or entering a new session, set the value instantly to avoid a long scroll animation.
