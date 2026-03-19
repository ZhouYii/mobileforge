# Presentation Layer

## Purpose

The presentation layer is the engine-specific UI management layer. It provides screens, popups, overlays, toasts, lists, and HUD elements -- all built programmatically with a **code-first approach**. The core framework modules use no scene files or prefabs -- everything is created and composed in code. (The `MobileForge.UIComponents` extension adds a prefab-based Primitives tier on top of this layer for visual polish; see the [UI Components Extension](#ui-components-extension) section below.)

- **Godot**: presentation classes extend `Node`, `Control`, or `ScrollContainer`. They are added to the scene tree at runtime.
- **Unity**: presentation classes extend `MonoBehaviour` or are plain C# classes that manage `GameObject` hierarchies via code.

The presentation layer subscribes to `EventBus` events from the domain and infrastructure layers. It never calls domain methods directly for state reads -- it reacts to events and updates visuals accordingly.

## Code-First Philosophy

Every UI element is created programmatically:

```gdscript
# Godot -- no .tscn file needed
var button := Button.new()
button.text = "Start"
button.pressed.connect(_on_start_pressed)
add_child(button)
```

```csharp
// Unity -- no .prefab file needed
var go = new GameObject("StartButton");
var button = go.AddComponent<Button>();
var text = go.AddComponent<TextMeshProUGUI>();
text.text = "Start";
```

This ensures:
- No merge conflicts on binary scene files
- Full programmatic control over layout and styling
- Easy theming and runtime customization
- Testable construction logic

## Module List

| Module | Files | Description | Doc |
|---|---|---|---|
| **UIRouter** | `ui_router.gd` / `UIRouter.cs` | Screen navigation with push/pop/replace stack | [router.md](router.md) |
| **BaseScreen** | `base_screen.gd` / `BaseScreen.cs` | Abstract screen lifecycle (enter, exit, pause, resume) | [screen.md](screen.md) |
| **PopupStack** | `popup_stack.gd` / `PopupStack.cs` | Priority queue of modal popups with dimming | [popup.md](popup.md) |
| **BasePopup** | `base_popup.gd` / `BasePopup.cs` | Abstract popup with show/dismiss lifecycle | [popup.md](popup.md) |
| **ToastLayer** | `toast_layer.gd` / `ToastLayer.cs` | Non-blocking notification banners | [toast.md](toast.md) |
| **OverlayManager** | `overlay_manager.gd` / `OverlayManager.cs` | Persistent overlays (loading, tutorial) | [overlay.md](overlay.md) |
| **VirtualList** | `virtual_list.gd` / `VirtualList.cs` | Scrollable list with item recycling | [list.md](list.md) |
| **GridView** | `grid_view.gd` / `GridView.cs` | Grid layout with cell recycling | [grid.md](grid.md) |
| **CurrencyBar** | `currency_bar.gd` / `CurrencyBar.cs` | Live-binding HUD currency display | [currency.md](currency.md) |
| **CardView** | `card_view.gd` / `CardView.cs` | Monster/item card display component | [card.md](card.md) |

## Z-Order Stack

All presentation elements are organized into four z-order layers, from back to front:

```
┌─────────────────────────────────────────┐
│  TOASTS       (z: 3000)   top-most      │
├─────────────────────────────────────────┤
│  POPUPS       (z: 2000)   modal dialogs │
├─────────────────────────────────────────┤
│  OVERLAYS     (z: 1000)   loading, etc. │
├─────────────────────────────────────────┤
│  SCREENS      (z: 0)      main content  │
└─────────────────────────────────────────┘
```

- **Screens** are the primary content (home, shop, battle, etc.). Only one screen is visible at a time (unless transitioning). Managed by `UIRouter`.
- **Overlays** sit above screens. Used for persistent UI like loading spinners or tutorial highlights. Managed by `OverlayManager`.
- **Popups** are modal dialogs. They show a dimmed background and block input to lower layers. Managed by `PopupStack` with priority ordering.
- **Toasts** are non-blocking notifications at the very top. They auto-dismiss after a timeout. Managed by `ToastLayer`.

## Event Subscription Flow

Presentation modules follow a consistent pattern for reacting to domain state changes:

1. **Subscribe** in on_enter() / _ready() / Start():
   EventBus.subscribe("event_name", callback)

2. **React** in the callback:
   Update visual state from the event payload dictionary

3. **Unsubscribe** in on_exit() / _exit_tree() / OnDestroy():
   EventBus.unsubscribe("event_name", callback)

Common event subscriptions by module:
| Presentation Module | Events Subscribed | Purpose |
|---|---|---|
| CurrencyBar | currency_changed | Animate count up/down |
| BattleScreen | damage_dealt, enemy_killed, wave_cleared, battle_won, battle_lost | Update battle UI |
| GachaScreen | monster_added | Show pull result |
| MonsterBoxScreen | monster_added, monster_removed | Refresh grid |
| ToastLayer | toast_requested | Show notification |

## Screen Lifecycle State Machine

```
                    navigate(id)
                        │
                        ▼
              ┌──────────────────┐
              │    on_enter()    │ ← First shown, initialize UI
              │  Subscribe to    │
              │  EventBus events │
              └────────┬─────────┘
                       │
              ┌────────▼─────────┐  push(other_id)  ┌──────────────┐
              │     ACTIVE       │ ────────────────> │  on_pause()  │
              │  Accepts input,  │                   │  Disable     │
              │  animations run  │                   │  input       │
              └────────▲─────────┘                   └──────┬───────┘
                       │                                    │
              ┌────────┴─────────┐                   ┌──────▼───────┐
              │   on_resume()   │ <──────────────── │   PAUSED     │
              │   Re-enable     │      pop()        │  Hidden by   │
              │   input         │                   │  screen above│
              └─────────────────┘                   └──────────────┘
                       │
                  pop() / replace()
                       │
              ┌────────▼─────────┐
              │    on_exit()     │ ← Cleanup, unsubscribe
              │  Remove from     │
              │  scene tree      │
              └──────────────────┘
```

## Event-Driven Updates

All presentation modules subscribe to `EventBus` for domain state changes. They never poll or directly query domain services.

```gdscript
# Godot -- CurrencyBar subscribes to currency changes
func _ready() -> void:
    EventBus.subscribe("currency_changed", _on_currency_changed)

func _exit_tree() -> void:
    EventBus.unsubscribe("currency_changed", _on_currency_changed)

func _on_currency_changed(event: Dictionary) -> void:
    if event["currency_id"] == _currency_id:
        _animate_to(event["new_amount"])
```

```csharp
// Unity -- CurrencyBar subscribes to currency changes
void Start()
{
    EventBus.Instance.Subscribe("currency_changed", OnCurrencyChanged);
}

void OnDestroy()
{
    EventBus.Instance.Unsubscribe("currency_changed", OnCurrencyChanged);
}

void OnCurrencyChanged(Dictionary<string, object> evt)
{
    if ((string)evt["currency_id"] == _currencyId)
        AnimateTo((int)evt["new_amount"]);
}
```

## Screen Lifecycle

Every screen goes through a standard lifecycle managed by `UIRouter`:

```
┌───────────┐     ┌───────────┐     ┌───────────┐     ┌───────────┐
│  on_enter  │ --> │  on_pause  │ --> │ on_resume  │ --> │  on_exit   │
└───────────┘     └───────────┘     └───────────┘     └───────────┘
      │                 │                 │                  │
  First shown      Another screen     Returns to       Removed from
  via navigate()   pushed on top      top of stack     stack entirely
```

- `on_enter()`: Called when the screen becomes the active screen. Initialize UI, subscribe to events.
- `on_pause()`: Called when another screen is pushed on top. Pause animations, disable input.
- `on_resume()`: Called when the screen returns to the top after a pop. Resume animations, re-enable input.
- `on_exit()`: Called when the screen is removed from the stack. Cleanup, unsubscribe from events.

## Popup Lifecycle

Popups have a simpler two-phase lifecycle:

```
┌───────────┐     ┌──────────────┐
│  on_show   │ --> │  on_dismiss   │
└───────────┘     └──────────────┘
      │                   │
  Popup becomes      Popup closed,
  visible, input     result passed
  enabled            to callback
```

## File Structure

```
Godot:
addons/mobileforge/presentation/
├── ui_router/
│   └── ui_router.gd
├── base_screen/
│   └── base_screen.gd
├── popup_stack/
│   └── popup_stack.gd
├── base_popup/
│   └── base_popup.gd
├── toast_layer/
│   └── toast_layer.gd
├── overlay_manager/
│   └── overlay_manager.gd
├── virtual_list/
│   ├── virtual_list.gd
│   ├── virtual_list_model.gd
│   ├── list_item_pool.gd
│   └── grid_cell_pool.gd
├── grid_view/
│   └── grid_view.gd
├── currency_bar/
│   └── currency_bar.gd
└── card_view/
    └── card_view.gd

Unity:
MobileForge/Presentation/
├── UIRouter.cs
├── BaseScreen.cs
├── PopupStack.cs
├── BasePopup.cs
├── ToastLayer.cs
├── OverlayManager.cs
├── VirtualList.cs
├── ListItemPool.cs
├── GridView.cs
├── GridCellPool.cs
├── CurrencyBar.cs
└── CardView.cs
```

## UI Components Extension

The `MobileForge.UIComponents` assembly extends this presentation layer with a two-tier system:

- **Primitives** — prefab-backed visual components (MFButton, MFGemBoard, MFProgressBar, etc.) that provide designer-friendly polish while still supporting a code-first fallback.
- **Pages** — code-based `PageBase<TScreen>` subclasses that compose Primitives into full screens, replacing the monolithic `GameRenderer` routing.

This extension builds on top of the core presentation modules (UIRouter, BaseScreen, etc.) — it does not change them. `UIComponentRouter` subscribes to the same `UIRouter.OnNavigated` events and manages the Page lifecycle in parallel.

See [UI Components Architecture](../ui-components-architecture.md) for full details.

## Further Reading

- [router.md](router.md) -- UIRouter navigation API
- [screen.md](screen.md) -- BaseScreen lifecycle
- [popup.md](popup.md) -- PopupStack and BasePopup
- [toast.md](toast.md) -- ToastLayer API
- [overlay.md](overlay.md) -- OverlayManager API
- [list.md](list.md) -- VirtualList and ListItemPool
- [grid.md](grid.md) -- GridView and GridCellPool
- [currency.md](currency.md) -- CurrencyBar live binding
- [card.md](card.md) -- CardView component
- [../README.md](../README.md) -- Framework architecture overview
