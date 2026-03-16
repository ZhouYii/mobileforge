# GridView and GridCellPool

## Purpose

GridView displays items in a scrollable grid layout with cell recycling. It uses the same virtualization strategy as VirtualList -- only visible cells exist as UI nodes -- but arranges items in rows and columns instead of a single column. GridCellPool manages the recycling of grid cell instances.

## Design Rationale

- **Shared recycling pattern.** GridCellPool works identically to ListItemPool. The only difference is layout: cells are arranged in a grid with configurable column count.
- **Fixed cell size.** All cells have the same width and height, enabling fast position calculation and consistent scrolling.
- **Column-first layout.** Items fill columns left-to-right, then wrap to the next row. This matches typical mobile inventory grids.

## Architecture

```
┌──────────────────────────────────────┐
│            GridView (View)            │
│  Extends ScrollContainer / ScrollRect │
│  - Column count and cell size config  │
│  - Calculates visible row range       │
│  - Binds data to visible cells        │
├──────────────────────────────────────┤
│        VirtualListModel (Data)        │
│  - Shared data model with VirtualList │
│  - Stores full item data array        │
├──────────────────────────────────────┤
│         GridCellPool (Recycling)      │
│  - acquire() -> cell instance         │
│  - release(cell) -> back to pool      │
│  - clear() -> free all pooled cells   │
└──────────────────────────────────────┘
```

## GridCellPool API

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `acquire()` | none | `Control` | Get a cell from the pool (or create new) |
| `release(cell: Control)` | cell to return | `void` | Return a cell to the pool |
| `pool_size()` | none | `int` | Number of cells currently in the pool |
| `clear()` | none | `void` | Free all pooled cells |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `Acquire()` | none | `GameObject` | Get a cell from the pool |
| `Release(GameObject cell)` | cell to return | `void` | Return a cell to the pool |
| `PoolSize` | -- | `int` | Number of pooled cells |
| `Clear()` | none | `void` | Destroy all pooled cells |

## GridView API

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `set_data(items: Array)` | data array | `void` | Set data and refresh grid |
| `set_column_count(cols: int)` | number of columns | `void` | Set grid column count |
| `set_cell_size(size: Vector2)` | width x height | `void` | Set fixed cell dimensions |
| `set_spacing(h: float, v: float)` | horizontal, vertical | `void` | Set gaps between cells |
| `set_bind_callback(cb: Callable)` | `(cell: Control, data: Variant, index: int)` | `void` | Set the cell bind function |
| `scroll_to_index(index: int)` | item index | `void` | Scroll to make cell visible |
| `refresh()` | none | `void` | Recalculate and rebind visible cells |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `SetData(List<object> items)` | data list | `void` | Set data and refresh |
| `SetColumnCount(int cols)` | columns | `void` | Set column count |
| `SetCellSize(Vector2 size)` | width x height | `void` | Set cell dimensions |
| `SetSpacing(float h, float v)` | horizontal, vertical gaps | `void` | Set spacing |
| `SetBindCallback(Action<GameObject, object, int> cb)` | bind function | `void` | Set bind function |
| `ScrollToIndex(int index)` | item index | `void` | Scroll to index |
| `Refresh()` | none | `void` | Rebind all visible cells |

## Layout Calculation

```
Given: 4 columns, cell_size = (100, 100), spacing = (8, 8)

Row 0:  [0]  [1]  [2]  [3]
Row 1:  [4]  [5]  [6]  [7]
Row 2:  [8]  [9]  [10] [11]
...

Cell position for item at index i:
  col = i % column_count
  row = i / column_count
  x = col * (cell_width + h_spacing)
  y = row * (cell_height + v_spacing)

Total rows = ceil(data_count / column_count)
Content height = total_rows * (cell_height + v_spacing) - v_spacing
```

## Usage Example

```gdscript
# Godot -- inventory grid
var grid := GridView.new()
grid.set_column_count(5)
grid.set_cell_size(Vector2(96, 96))
grid.set_spacing(8.0, 8.0)
grid.set_bind_callback(_bind_cell)
add_child(grid)

grid.set_data(inventory_items)

func _bind_cell(cell: Control, data: Variant, index: int) -> void:
    var icon: TextureRect = cell.get_node("Icon")
    icon.texture = load(data["icon_path"])
    var count_label: Label = cell.get_node("Count")
    count_label.text = str(data["count"]) if data["count"] > 1 else ""
```

```csharp
// Unity -- inventory grid
var grid = new GridView();
grid.SetColumnCount(5);
grid.SetCellSize(new Vector2(96, 96));
grid.SetSpacing(8f, 8f);
grid.SetBindCallback((go, data, index) =>
{
    var item = (Dictionary<string, object>)data;
    // ... bind icon and count label
});
grid.SetData(inventoryItems);
```

## Recycling Flow

Same as VirtualList, but operating on rows of cells:

```
1. User scrolls down
2. Top row of cells leaves viewport
3. GridView releases all cells in that row to the pool
4. Bottom row enters viewport
5. GridView acquires cells from pool (or creates new)
6. GridView calls bind_callback for each cell in the new row
```

## Performance

Like VirtualList, GridView maintains a constant number of active nodes regardless of data size. The number of active cells equals `(visible_rows + buffer_rows) * column_count`.

| Data Size | Columns | Active Cells | Memory |
|---|---|---|---|
| 50 items | 5 | ~25-35 | Low |
| 500 items | 5 | ~25-35 | Low |
| 5,000 items | 5 | ~25-35 | Low |

## Best Practices

1. **Use fixed cell sizes.** Variable cell sizes break the fast position calculation.
2. **Choose column count based on screen width.** Dynamically adjust columns when screen orientation changes.
3. **Keep bind callbacks lightweight.** Load icons asynchronously if they are not cached.
4. **Reuse the same data model.** GridView and VirtualList share `VirtualListModel`. You can switch between list and grid views without rebuilding the data layer.
