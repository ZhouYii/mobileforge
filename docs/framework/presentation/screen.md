# BaseScreen

## Purpose

BaseScreen is the abstract base class for all screens in the presentation layer. It defines the four-phase lifecycle that `UIRouter` manages: enter, pause, resume, and exit. All concrete screens extend BaseScreen and override these methods to set up and tear down their UI.

## Lifecycle

```
┌───────────┐     ┌───────────┐     ┌───────────┐     ┌───────────┐
│  on_enter  │ --> │  on_pause  │ --> │ on_resume  │ --> │  on_exit   │
└───────────┘     └───────────┘     └───────────┘     └───────────┘
```

| Method | Trigger | Typical Actions |
|---|---|---|
| `on_enter(params)` | Screen becomes the active top of stack | Create UI, subscribe to events, start animations |
| `on_pause()` | Another screen is pushed on top | Pause animations, disable input processing |
| `on_resume()` | Returns to top after a pop | Resume animations, re-enable input, refresh data |
| `on_exit()` | Removed from the stack entirely | Unsubscribe from events, free child nodes, cleanup |

A screen may cycle through pause/resume multiple times before finally exiting.

## API Reference

### Godot (GDScript)

```gdscript
class_name BaseScreen extends Control

## Unique screen ID (set by UIRouter during registration).
var screen_id: String = ""

## Parameters passed via navigate/push/replace.
var screen_params: Dictionary = {}

## Called when this screen becomes visible.
func on_enter(params: Dictionary) -> void:
    screen_params = params

## Called when another screen is pushed on top.
func on_pause() -> void:
    pass

## Called when this screen returns to the top after a pop.
func on_resume() -> void:
    pass

## Called when this screen is removed from the stack.
func on_exit() -> void:
    pass
```

### Unity (C#)

```csharp
public abstract class BaseScreen
{
    public string ScreenId { get; set; } = "";
    public Dictionary<string, object> ScreenParams { get; set; }

    public virtual void OnEnter(Dictionary<string, object> parameters)
    {
        ScreenParams = parameters;
    }

    public virtual void OnPause() { }
    public virtual void OnResume() { }
    public virtual void OnExit() { }
}
```

## Implementation Example

```gdscript
# Godot -- a simple shop screen
class_name ShopScreen extends BaseScreen

var _item_list: VirtualList
var _currency_bar: CurrencyBar

func on_enter(params: Dictionary) -> void:
    super(params)

    _currency_bar = CurrencyBar.new("gold")
    add_child(_currency_bar)

    _item_list = VirtualList.new()
    add_child(_item_list)

    EventBus.subscribe("shop_items_loaded", _on_items_loaded)
    EventBus.emit_event("shop_load_requested", {"category": params.get("category", "all")})

func on_pause() -> void:
    # Disable tap handling while another screen is on top
    _item_list.set_process_input(false)

func on_resume() -> void:
    # Re-enable input and refresh in case currency changed
    _item_list.set_process_input(true)
    _currency_bar.refresh()

func on_exit() -> void:
    EventBus.unsubscribe("shop_items_loaded", _on_items_loaded)
    # Child nodes are freed automatically when this node is freed

func _on_items_loaded(event: Dictionary) -> void:
    _item_list.set_data(event["items"])
```

```csharp
// Unity -- same shop screen
public class ShopScreen : BaseScreen
{
    private VirtualList _itemList;
    private CurrencyBar _currencyBar;

    public override void OnEnter(Dictionary<string, object> parameters)
    {
        base.OnEnter(parameters);

        _currencyBar = new CurrencyBar("gold");
        _itemList = new VirtualList();

        EventBus.Instance.Subscribe("shop_items_loaded", OnItemsLoaded);
        EventBus.Instance.Emit("shop_load_requested",
            new Dictionary<string, object> { { "category", parameters.GetValueOrDefault("category", "all") } });
    }

    public override void OnPause()
    {
        _itemList.SetInputEnabled(false);
    }

    public override void OnResume()
    {
        _itemList.SetInputEnabled(true);
        _currencyBar.Refresh();
    }

    public override void OnExit()
    {
        EventBus.Instance.Unsubscribe("shop_items_loaded", OnItemsLoaded);
    }

    private void OnItemsLoaded(Dictionary<string, object> evt)
    {
        // ...
    }
}
```

## Screen Construction Pattern

Screens build their UI in `on_enter()`, not in a constructor or `_ready()`. This ensures:

1. Parameters are available when UI is built.
2. The screen is already in the scene tree (Godot) or hierarchy (Unity).
3. EventBus subscriptions happen at the right time.

```gdscript
# Correct: build UI in on_enter
func on_enter(params: Dictionary) -> void:
    super(params)
    var label := Label.new()
    label.text = params.get("title", "Default")
    add_child(label)

# Wrong: build UI in _init -- params not available yet
func _init() -> void:
    var label := Label.new()
    add_child(label)  # screen_params is empty here
```

## Best Practices

1. **Subscribe in `on_enter`, unsubscribe in `on_exit`.** This prevents leaked subscriptions.
2. **Pause expensive operations in `on_pause`.** Animations, timers, and input processing should stop when the screen is not visible.
3. **Refresh data in `on_resume`.** State may have changed while the screen was paused.
4. **Build UI programmatically.** No scene file dependencies. Create controls in code within `on_enter`.
5. **Use `screen_params` for configuration.** Pass data through the router rather than global state.
