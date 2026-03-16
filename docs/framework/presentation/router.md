# UIRouter

## Purpose

UIRouter is the screen navigation manager. It maintains a stack of screens and provides push, pop, replace, and navigate operations. Only the top screen on the stack is visible and receives input. All other screens are paused.

UIRouter works together with `ScreenRegistry`, which maps screen IDs to factory callables that create screen instances on demand.

## Design Rationale

- **Stack-based navigation.** Matches the natural flow of mobile apps: main menu -> shop -> item detail -> back. Push adds depth, pop returns.
- **Navigate resets the stack.** When the player taps "Home", navigate clears the entire stack and starts fresh. No stale screens linger.
- **Replace swaps the top.** Useful for tab-like navigation within the same depth level. The stack depth does not change.
- **Factory-based screen creation.** Screens are not pre-instantiated. A factory callable creates them when needed. This keeps memory low and avoids stale state.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `register_screen(id: String, factory: Callable)` | screen ID, factory function | `void` | Register a screen factory |
| `has_screen(id: String)` | screen ID | `bool` | Check if a screen ID is registered |
| `navigate(id: String, params: Dictionary)` | screen ID, optional params | `void` | Clear stack and show screen (depth becomes 1) |
| `push(id: String, params: Dictionary)` | screen ID, optional params | `void` | Push a new screen on top of the stack |
| `pop()` | none | `void` | Remove top screen, resume the one below. No-op if depth is 1 |
| `replace(id: String, params: Dictionary)` | screen ID, optional params | `void` | Replace top screen without changing depth |
| `current_screen_id()` | none | `String` | ID of the currently visible screen |
| `stack_depth()` | none | `int` | Number of screens on the stack |
| `can_pop()` | none | `bool` | True if stack depth > 1 |

### Unity (C#)

| Method / Property | Params | Return | Description |
|---|---|---|---|
| `RegisterScreen(string id, Func<object> factory)` | screen ID, factory | `void` | Register a screen factory |
| `HasScreen(string id)` | screen ID | `bool` | Check if a screen ID is registered |
| `Navigate(string id, Dictionary<string, object> p)` | screen ID, optional params | `void` | Clear stack and show screen |
| `Push(string id, Dictionary<string, object> p)` | screen ID, optional params | `void` | Push a new screen on top |
| `Pop()` | none | `void` | Remove top screen. No-op if depth is 1 |
| `Replace(string id, Dictionary<string, object> p)` | screen ID, optional params | `void` | Replace top screen |
| `CurrentScreenId` | -- | `string` | ID of the currently visible screen |
| `StackDepth` | -- | `int` | Number of screens on the stack |
| `CanPop` | -- | `bool` | True if stack depth > 1 |

## Navigation Operations

### Navigate

Clears the entire stack and shows the target screen. Use this for top-level navigation (e.g., "go home").

```
Before: [Home, Shop, ItemDetail]
navigate("Battle")
After:  [Battle]
```

```gdscript
# Godot
router.navigate("battle", {"dungeon_id": "forest_01"})
```

```csharp
// Unity
router.Navigate("battle", new Dictionary<string, object> { { "dungeon_id", "forest_01" } });
```

### Push

Adds a new screen on top of the stack. The previous screen is paused but stays in memory.

```
Before: [Home]
push("Shop")
After:  [Home, Shop]
```

```gdscript
# Godot
router.push("shop")
```

```csharp
// Unity
router.Push("shop");
```

### Pop

Removes the top screen and resumes the one below it. If only one screen remains, pop is a no-op.

```
Before: [Home, Shop, ItemDetail]
pop()
After:  [Home, Shop]
```

```gdscript
# Godot
if router.can_pop():
    router.pop()
```

```csharp
// Unity
if (router.CanPop)
    router.Pop();
```

### Replace

Swaps the top screen for a different one without changing the stack depth. Useful for tab navigation.

```
Before: [Home, ShopTab_Weapons]
replace("ShopTab_Armor")
After:  [Home, ShopTab_Armor]
```

```gdscript
# Godot
router.replace("shop_tab_armor")
```

```csharp
// Unity
router.Replace("shop_tab_armor");
```

## Screen Lifecycle Integration

UIRouter calls lifecycle methods on screens during transitions:

| Operation | Old Top Screen | New Top Screen |
|---|---|---|
| `navigate` | `on_exit()` (all screens) | `on_enter()` |
| `push` | `on_pause()` | `on_enter()` |
| `pop` | `on_exit()` | `on_resume()` |
| `replace` | `on_exit()` | `on_enter()` |

## Registration Pattern

Register all screens during initialization, before any navigation occurs:

```gdscript
# Godot
func _ready() -> void:
    router.register_screen("home", func(): return HomeScreen.new())
    router.register_screen("shop", func(): return ShopScreen.new())
    router.register_screen("battle", func(): return BattleScreen.new())
    router.register_screen("inventory", func(): return InventoryScreen.new())

    # Initial navigation
    router.navigate("home")
```

```csharp
// Unity
void Start()
{
    router.RegisterScreen("home", () => new HomeScreen());
    router.RegisterScreen("shop", () => new ShopScreen());
    router.RegisterScreen("battle", () => new BattleScreen());
    router.RegisterScreen("inventory", () => new InventoryScreen());

    router.Navigate("home");
}
```

## Back Button Handling

On Android, the back button maps to `pop()`:

```gdscript
# Godot
func _unhandled_input(event: InputEvent) -> void:
    if event.is_action_pressed("ui_cancel"):
        if router.can_pop():
            router.pop()
            get_viewport().set_input_as_handled()
```

```csharp
// Unity
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape) && router.CanPop)
        router.Pop();
}
```

## Events Emitted

UIRouter emits events via EventBus when navigation occurs:

| Event | Payload | When |
|---|---|---|
| `screen_entered` | `{ "screen_id": String }` | A screen becomes visible via navigate/push |
| `screen_exited` | `{ "screen_id": String }` | A screen is removed from the stack |
| `screen_paused` | `{ "screen_id": String }` | A screen is paused (another pushed on top) |
| `screen_resumed` | `{ "screen_id": String }` | A screen resumes after the one above is popped |

## Thread Safety

UIRouter is single-threaded. All navigation calls must happen on the main thread. Navigation during a transition (e.g., calling `push` inside `on_enter`) is queued and processed after the current transition completes.
