# VirtualList and ListItemPool

## Purpose

VirtualList is a high-performance scrollable list that recycles off-screen items instead of creating new ones. For a list of 10,000 items, only the visible items (plus a small buffer) exist as actual UI nodes. As the user scrolls, items leaving the viewport are returned to a pool and reused for items entering the viewport.

ListItemPool manages the object pool that VirtualList draws from. It handles allocation, deallocation, and reuse of list item instances.

## Design Rationale

- **Constant memory.** A list of 10 items and a list of 10,000 items use roughly the same number of UI nodes. Only visible items are instantiated.
- **Smooth scrolling.** By recycling existing nodes instead of allocating and freeing, scroll performance stays consistent with no GC spikes.
- **Data-model separation.** The full dataset lives in `VirtualListModel` as plain data. The visual representation is a thin view layer on top.
- **Pool-based recycling.** ListItemPool provides acquire/release semantics. Released items are reset and stored for reuse.

## Architecture

```
┌──────────────────────────────────────┐
│           VirtualList (View)          │
│  Extends ScrollContainer / ScrollRect │
│  - Manages scroll position            │
│  - Calculates visible range           │
│  - Binds data to visible items        │
├──────────────────────────────────────┤
│        VirtualListModel (Data)        │
│  - Stores full item data array        │
│  - Provides count, get_item(index)    │
├──────────────────────────────────────┤
│        ListItemPool (Recycling)       │
│  - acquire() -> item instance         │
│  - release(item) -> back to pool      │
│  - clear() -> free all pooled items   │
└──────────────────────────────────────┘
```

## VirtualListModel API

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `set_data(items: Array)` | array of item data | `void` | Set the full data array |
| `get_data_count()` | none | `int` | Number of items in the data |
| `get_item(index: int)` | item index | `Variant` | Get data for item at index |
| `insert_item(index: int, item: Variant)` | index, data | `void` | Insert item at index |
| `remove_item(index: int)` | index | `void` | Remove item at index |
| `update_item(index: int, item: Variant)` | index, new data | `void` | Update data at index |
| `clear()` | none | `void` | Remove all data |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `SetData(List<object> items)` | list of item data | `void` | Set the full data list |
| `GetDataCount()` | none | `int` | Number of items |
| `GetItem(int index)` | item index | `object` | Get data for item at index |
| `InsertItem(int index, object item)` | index, data | `void` | Insert item at index |
| `RemoveItem(int index)` | index | `void` | Remove item at index |
| `UpdateItem(int index, object item)` | index, new data | `void` | Update data at index |
| `Clear()` | none | `void` | Remove all data |

## ListItemPool API

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `acquire()` | none | `Control` | Get an item from the pool (or create new) |
| `release(item: Control)` | item to return | `void` | Return an item to the pool |
| `pool_size()` | none | `int` | Number of items currently in the pool |
| `clear()` | none | `void` | Free all pooled items |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `Acquire()` | none | `GameObject` | Get an item from the pool |
| `Release(GameObject item)` | item to return | `void` | Return an item to the pool |
| `PoolSize` | -- | `int` | Number of pooled items |
| `Clear()` | none | `void` | Destroy all pooled items |

## VirtualList API

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `set_data(items: Array)` | data array | `void` | Set data and refresh visible items |
| `set_item_height(height: float)` | pixels | `void` | Set fixed item height |
| `set_bind_callback(cb: Callable)` | `(item: Control, data: Variant, index: int)` | `void` | Set the function that populates an item |
| `scroll_to_index(index: int)` | item index | `void` | Scroll to make item at index visible |
| `refresh()` | none | `void` | Recalculate and rebind all visible items |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `SetData(List<object> items)` | data list | `void` | Set data and refresh |
| `SetItemHeight(float height)` | pixels | `void` | Set fixed item height |
| `SetBindCallback(Action<GameObject, object, int> cb)` | bind function | `void` | Set the bind function |
| `ScrollToIndex(int index)` | item index | `void` | Scroll to make item visible |
| `Refresh()` | none | `void` | Rebind all visible items |

## Usage Example

```gdscript
# Godot
var list := VirtualList.new()
list.set_item_height(80.0)
list.set_bind_callback(_bind_item)
add_child(list)

# Set data -- could be 10,000 items
list.set_data(all_inventory_items)

func _bind_item(item_control: Control, data: Variant, index: int) -> void:
    var label: Label = item_control.get_node("NameLabel")
    label.text = data["name"]
    var icon: TextureRect = item_control.get_node("Icon")
    icon.texture = load(data["icon_path"])
```

```csharp
// Unity
var list = new VirtualList();
list.SetItemHeight(80f);
list.SetBindCallback((go, data, index) =>
{
    var itemData = (Dictionary<string, object>)data;
    go.GetComponentInChildren<Text>().text = (string)itemData["name"];
});
list.SetData(allInventoryItems);
```

## Recycling Flow

```
1. User scrolls down
2. Item at top leaves viewport
3. VirtualList calls pool.release(topItem)
   -> topItem is hidden and stored in pool
4. New item enters viewport at bottom
5. VirtualList calls pool.acquire()
   -> returns the recycled topItem (or creates new if pool is empty)
6. VirtualList calls bind_callback(item, data[newIndex], newIndex)
   -> item is updated with new data and repositioned
```

## Performance Characteristics

| Data Size | Active Nodes | Memory |
|---|---|---|
| 10 items | ~10 | Low |
| 100 items | ~12-15 | Low |
| 1,000 items | ~12-15 | Low |
| 10,000 items | ~12-15 | Low |

The number of active nodes depends on item height and viewport height, not data size.

## Best Practices

1. **Use fixed item heights.** Variable heights require additional calculation and reduce scroll smoothness.
2. **Keep bind callbacks fast.** They run every time an item is recycled. Avoid loading assets synchronously in the bind callback.
3. **Reset item state in bind.** When a recycled item is rebound, make sure all fields are updated. Stale data from the previous binding must be overwritten.
4. **Pool items before clearing data.** Call `refresh()` after `set_data()` to ensure the pool and visible items stay synchronized.
