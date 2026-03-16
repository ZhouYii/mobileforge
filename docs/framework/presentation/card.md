# CardView

## Purpose

CardView is a reusable presentation component that displays a monster or item card. It renders the card's portrait, element badge, rarity stars, level indicator, and stat bars in a standardized layout. CardView is used throughout the UI: in inventory grids, team builder slots, gacha result screens, and detail panels.

## Design Rationale

- **Single source of truth for card rendering.** All places that display a card use CardView. Visual consistency is guaranteed.
- **Data-driven.** CardView accepts a plain data dictionary (or a `MonsterInstance` / item definition) and renders it. No internal state, no subscriptions.
- **Scalable.** The same CardView component can be rendered at different sizes: thumbnail (grid cell), medium (team slot), or full (detail panel). The layout adapts to the available space.
- **Code-first.** No scene file. CardView builds its node hierarchy programmatically.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `new()` | none | `CardView` | Create a new empty card |
| `bind(data: Dictionary)` | card data dict | `void` | Populate the card with data |
| `set_size_preset(preset: CardSize)` | THUMBNAIL, MEDIUM, FULL | `void` | Set layout size preset |
| `set_show_level(show: bool)` | true/false | `void` | Toggle level indicator |
| `set_show_stats(show: bool)` | true/false | `void` | Toggle stat bars |
| `set_show_element(show: bool)` | true/false | `void` | Toggle element badge |
| `set_highlight(enabled: bool)` | true/false | `void` | Toggle selection highlight |
| `get_data()` | none | `Dictionary` | Get the currently bound data |

### Unity (C#)

| Method / Property | Params | Return | Description |
|---|---|---|---|
| `CardView()` | none | -- | Constructor |
| `Bind(Dictionary<string, object> data)` | card data | `void` | Populate card with data |
| `SetSizePreset(CardSize preset)` | THUMBNAIL, MEDIUM, FULL | `void` | Set size preset |
| `ShowLevel` | -- | `bool` | Toggle level indicator |
| `ShowStats` | -- | `bool` | Toggle stat bars |
| `ShowElement` | -- | `bool` | Toggle element badge |
| `Highlight` | -- | `bool` | Toggle highlight |
| `GetData()` | none | `Dictionary<string, object>` | Get bound data |

## Size Presets

| Preset | Dimensions | Shows | Use Case |
|---|---|---|---|
| `THUMBNAIL` | 96 x 96 | Portrait, element badge, rarity border | Inventory grid cells |
| `MEDIUM` | 128 x 160 | Portrait, element badge, rarity border, level | Team builder slots |
| `FULL` | 256 x 360 | All: portrait, element, rarity, level, stats, name | Detail panel |

## Card Data Dictionary

CardView expects these keys in the data dictionary:

| Key | Type | Required | Description |
|---|---|---|---|
| `"name"` | String | Yes | Monster/item name |
| `"portrait_path"` | String | Yes | Path to portrait image |
| `"element"` | int / Element | Yes | Element type (Water, Fire, etc.) |
| `"rarity"` | int | Yes | Star rating (1-6) |
| `"level"` | int | No | Current level |
| `"max_level"` | int | No | Maximum level |
| `"hp"` | int | No | Hit points (for stat bars) |
| `"atk"` | int | No | Attack stat |
| `"rcv"` | int | No | Recovery stat |
| `"hp_max"` | int | No | Max HP (for stat bar fill) |
| `"atk_max"` | int | No | Max ATK |
| `"rcv_max"` | int | No | Max RCV |

## Visual Layout

```
FULL size layout:

┌────────────────────────────┐
│  ┌──────────────────────┐  │
│  │                      │  │
│  │     Portrait         │  │
│  │                      │  │
│  │  [Element Badge]     │  │
│  └──────────────────────┘  │
│  Name                      │
│  ★★★★★☆  (rarity stars)    │
│  Lv. 45 / 99              │
│  ───────────────────────── │
│  HP   ████████░░  1200     │
│  ATK  ██████░░░░   800     │
│  RCV  ████░░░░░░   400     │
└────────────────────────────┘

Rarity border color:
  1★ = gray
  2★ = bronze
  3★ = silver
  4★ = gold
  5★ = rainbow shimmer
  6★ = animated rainbow
```

## Usage Examples

### In a Grid Cell (Thumbnail)

```gdscript
# Godot -- used as GridView bind callback
func _bind_cell(cell: Control, data: Variant, index: int) -> void:
    var card: CardView = cell.get_node("CardView")
    card.set_size_preset(CardView.CardSize.THUMBNAIL)
    card.bind(data)
```

### In Team Builder (Medium)

```gdscript
# Godot
for i in range(team_size):
    var card := CardView.new()
    card.set_size_preset(CardView.CardSize.MEDIUM)
    card.set_show_level(true)
    if i < team.size():
        card.bind(team[i].to_card_data())
    team_container.add_child(card)
```

### In Detail Panel (Full)

```gdscript
# Godot
var detail_card := CardView.new()
detail_card.set_size_preset(CardView.CardSize.FULL)
detail_card.set_show_stats(true)
detail_card.bind(selected_monster.to_card_data())
detail_panel.add_child(detail_card)
```

```csharp
// Unity
var detailCard = new CardView();
detailCard.SetSizePreset(CardSize.FULL);
detailCard.ShowStats = true;
detailCard.Bind(selectedMonster.ToCardData());
detailPanel.AddChild(detailCard);
```

## Element Badge Colors

| Element | Color |
|---|---|
| Water | Blue (#4A90D9) |
| Fire | Red (#D94A4A) |
| Grass | Green (#4AD94A) |
| Light | Yellow (#D9D94A) |
| Dark | Purple (#8B4AD9) |
| Heart | Pink (#D94A8B) |

## Best Practices

1. **Bind before adding to tree.** Call `bind()` before the card is visible to avoid a flash of empty content.
2. **Use size presets consistently.** THUMBNAIL for grids, MEDIUM for slots, FULL for detail views.
3. **Toggle visibility features per context.** Grid cells do not need stat bars. Detail panels do.
4. **Cache portrait textures.** Loading portrait images is expensive. Use a shared texture cache to avoid redundant loads when scrolling through a grid.
5. **Highlight for selection.** Use `set_highlight(true)` to indicate the currently selected card, rather than creating a separate selection indicator.
