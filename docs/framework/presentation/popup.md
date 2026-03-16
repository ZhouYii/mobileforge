# PopupStack and BasePopup

## Purpose

PopupStack manages a **priority queue of modal popups**. Only one popup is visible at a time. When a popup is dismissed, the next highest-priority popup in the queue is shown automatically. A dimmed background blocks input to everything below the popup.

BasePopup is the abstract base class for all popup implementations. It provides the show/dismiss lifecycle and a result callback mechanism.

## Design Rationale

- **Priority queue, not FIFO.** Some popups are more urgent than others. A "server maintenance" popup should jump ahead of a "daily login reward" popup. Priority ordering handles this automatically.
- **One at a time.** Overlapping modal popups create confusion. The stack ensures only the highest-priority popup is visible.
- **Dismiss callbacks.** The caller that requested the popup can receive the user's decision (e.g., "confirmed" or "cancelled") through a callback, without tight coupling.
- **Dimmed background.** Provides clear visual separation and blocks touch/click on underlying content.

## PopupStack API

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `show_popup(id: String, params: Dictionary, priority: int, on_dismiss: Callable)` | popup ID, display params, priority (higher = more urgent), optional dismiss callback | `void` | Enqueue a popup |
| `dismiss(result: Variant)` | optional result value | `void` | Dismiss the current popup, passing result to its callback |
| `dismiss_all()` | none | `void` | Clear all popups without callbacks |
| `is_showing()` | none | `bool` | True if a popup is currently visible |
| `popup_count()` | none | `int` | Number of popups in the queue (including the current one) |
| `current_popup_id()` | none | `String` | ID of the currently showing popup, or empty string |

### Unity (C#)

| Method / Property | Params | Return | Description |
|---|---|---|---|
| `ShowPopup(string id, Dictionary<string, object> p, int priority, Action<Dictionary<string, object>> onDismiss)` | popup ID, params, priority, optional callback | `void` | Enqueue a popup |
| `Dismiss(Dictionary<string, object> result)` | optional result | `void` | Dismiss the current popup |
| `DismissAll()` | none | `void` | Clear all popups |
| `IsShowing` | -- | `bool` | True if a popup is currently visible |
| `PopupCount` | -- | `int` | Number of popups in queue |
| `CurrentPopupId` | -- | `string` | ID of the currently showing popup |

## Priority System

Popups are ordered by priority (descending). Higher numeric value = higher urgency = shown first.

| Priority Range | Convention |
|---|---|
| 900 -- 999 | System-critical (force update, maintenance) |
| 500 -- 899 | Important gameplay (level up, achievement) |
| 100 -- 499 | Standard popups (confirmation, info) |
| 0 -- 99 | Low-priority (tips, promotions) |

When two popups have the same priority, the one enqueued first is shown first (stable ordering).

## Dimming

When a popup is showing, a full-screen semi-transparent overlay is displayed behind it:

```
┌─────────────────────────────┐
│         Popup Content        │  z: 2001
├─────────────────────────────┤
│    Dim Overlay (50% black)   │  z: 2000
├─────────────────────────────┤
│       Screen Content         │  z: 0 (input blocked)
└─────────────────────────────┘
```

Tapping the dim overlay can optionally dismiss the popup (configurable per popup via `dismiss_on_outside_tap`).

## BasePopup Lifecycle

```gdscript
class_name BasePopup extends Control

## Called when the popup becomes visible.
func on_show(params: Dictionary) -> void:
    pass

## Called when the popup is dismissed. Return a result for the callback.
func on_dismiss() -> Variant:
    return null
```

```csharp
public abstract class BasePopup
{
    public virtual void OnShow(Dictionary<string, object> parameters) { }
    public virtual object OnDismiss() { return null; }
}
```

### Custom Popup Example

```gdscript
# Godot
class_name ConfirmPopup extends BasePopup

var _title_label: Label
var _confirm_button: Button
var _cancel_button: Button
var _result: bool = false

func _init() -> void:
    _title_label = Label.new()
    add_child(_title_label)

    _confirm_button = Button.new()
    _confirm_button.text = "OK"
    _confirm_button.pressed.connect(func():
        _result = true
        PopupStack.dismiss({"accepted": true})
    )
    add_child(_confirm_button)

    _cancel_button = Button.new()
    _cancel_button.text = "Cancel"
    _cancel_button.pressed.connect(func():
        _result = false
        PopupStack.dismiss({"accepted": false})
    )
    add_child(_cancel_button)

func on_show(params: Dictionary) -> void:
    _title_label.text = params.get("title", "Confirm?")
```

```csharp
// Unity
public class ConfirmPopup : BasePopup
{
    private bool _result = false;

    public override void OnShow(Dictionary<string, object> parameters)
    {
        string title = parameters.ContainsKey("title")
            ? (string)parameters["title"] : "Confirm?";
        // ... set up UI with title
    }
}
```

## Usage Pattern

```gdscript
# Godot -- show a confirmation popup and handle the result
PopupStack.show_popup("confirm", {"title": "Buy item for 500 gold?"}, 200,
    func(result: Variant):
        if result is Dictionary and result.get("accepted", false):
            CurrencyService.spend("gold", 500)
            InventoryService.add_item(item_id)
)
```

```csharp
// Unity
PopupStack.ShowPopup("confirm",
    new Dictionary<string, object> { { "title", "Buy item for 500 gold?" } },
    200,
    result =>
    {
        if (result != null && (bool)result["accepted"])
        {
            CurrencyService.Spend("gold", 500);
            InventoryService.AddItem(itemId);
        }
    });
```

## Events Emitted

| Event | Payload | When |
|---|---|---|
| `popup_shown` | `{ "popup_id": String, "priority": int }` | A popup becomes visible |
| `popup_dismissed` | `{ "popup_id": String }` | A popup is dismissed |
| `popup_queue_empty` | `{}` | Last popup dismissed, queue is now empty |

## Best Practices

1. **Use priority ranges consistently.** System popups should always preempt gameplay popups.
2. **Always handle dismiss results.** If a popup needs a user decision, provide the callback. Do not assume the user will always confirm.
3. **Keep popups small.** A popup should contain a single decision. Complex forms belong in screens, not popups.
4. **Avoid chaining popups.** Showing a popup from within a dismiss callback is supported but creates a confusing user experience. Use sparingly.
5. **Test without visuals.** The priority queue and callback logic can be tested without a scene tree by checking `popup_count()`, `current_popup_id()`, and callback invocation.
