# OverlayManager

## Purpose

OverlayManager controls persistent overlays that sit between screens and popups in the z-order stack. Overlays are used for cross-cutting UI concerns like loading spinners, tutorial highlights, network status indicators, and fade transitions.

Unlike screens (which are stacked and mutually exclusive) and popups (which are modal), overlays can be shown and hidden independently of each other and of the current screen.

## Design Rationale

- **Independent show/hide.** Overlays do not interact with the screen stack. Showing a loading overlay does not push a new screen.
- **Named overlays.** Each overlay has a unique ID. You can show/hide specific overlays by name.
- **Z-order between screens and popups.** Overlays cover the current screen but do not block popups (which are more urgent).
- **Optional input blocking.** Some overlays (like loading) should block all input. Others (like a tutorial arrow) should not.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `register_overlay(id: String, factory: Callable)` | overlay ID, factory function | `void` | Register an overlay factory |
| `show_overlay(id: String, params: Dictionary)` | overlay ID, optional params | `void` | Show the named overlay |
| `hide_overlay(id: String)` | overlay ID | `void` | Hide the named overlay |
| `is_overlay_visible(id: String)` | overlay ID | `bool` | Check if overlay is currently visible |
| `hide_all()` | none | `void` | Hide all visible overlays |
| `visible_overlay_ids()` | none | `Array[String]` | List of currently visible overlay IDs |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `RegisterOverlay(string id, Func<object> factory)` | overlay ID, factory | `void` | Register an overlay factory |
| `ShowOverlay(string id, Dictionary<string, object> p)` | overlay ID, optional params | `void` | Show the named overlay |
| `HideOverlay(string id)` | overlay ID | `void` | Hide the named overlay |
| `IsOverlayVisible(string id)` | overlay ID | `bool` | Check if overlay is visible |
| `HideAll()` | none | `void` | Hide all overlays |
| `VisibleOverlayIds` | -- | `List<string>` | List of visible overlay IDs |

## Common Overlays

| Overlay ID | Blocks Input | Use Case |
|---|---|---|
| `"loading"` | Yes | Full-screen loading spinner during data loads |
| `"transition"` | Yes | Fade-to-black between screen transitions |
| `"tutorial"` | Partial | Highlight specific UI elements during tutorial |
| `"network_error"` | No | Persistent banner showing offline status |
| `"debug"` | No | FPS counter, memory stats (dev builds only) |

## Usage Examples

### Loading Overlay

```gdscript
# Godot
OverlayManager.show_overlay("loading", {"message": "Loading dungeon..."})

# ... after async load completes
OverlayManager.hide_overlay("loading")
```

```csharp
// Unity
OverlayManager.ShowOverlay("loading",
    new Dictionary<string, object> { { "message", "Loading dungeon..." } });

// ... after async load completes
OverlayManager.HideOverlay("loading");
```

### Screen Transition

```gdscript
# Godot -- fade out, navigate, fade in
OverlayManager.show_overlay("transition", {"direction": "out"})
await get_tree().create_timer(0.5).timeout
router.navigate("battle")
OverlayManager.show_overlay("transition", {"direction": "in"})
await get_tree().create_timer(0.5).timeout
OverlayManager.hide_overlay("transition")
```

### Tutorial Highlight

```gdscript
# Godot -- highlight a specific button during tutorial
OverlayManager.show_overlay("tutorial", {
    "target_rect": shop_button.get_global_rect(),
    "message": "Tap here to open the shop!"
})
```

## Input Blocking

Overlays that block input consume all input events so nothing reaches screens or other UI below them:

```gdscript
# Godot -- loading overlay blocks all input
class_name LoadingOverlay extends Control

func _init() -> void:
    mouse_filter = Control.MOUSE_FILTER_STOP
    # Full-screen coverage
    set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
```

```csharp
// Unity -- loading overlay blocks raycasts
public class LoadingOverlay : MonoBehaviour, ICanvasRaycastFilter
{
    public bool IsRaycastLocationValid(Vector2 sp, Camera cam) => false;
}
```

## Events Emitted

| Event | Payload | When |
|---|---|---|
| `overlay_shown` | `{ "overlay_id": String }` | An overlay becomes visible |
| `overlay_hidden` | `{ "overlay_id": String }` | An overlay is hidden |

## Best Practices

1. **Use loading overlay for all async operations.** Players should always see feedback when something is loading.
2. **Hide overlays in error paths.** If a load fails, make sure the loading overlay is hidden so the player is not stuck.
3. **Keep overlay counts low.** Rarely should more than one or two overlays be visible simultaneously.
4. **Test overlay show/hide independently.** Overlays should work regardless of which screen is active.
