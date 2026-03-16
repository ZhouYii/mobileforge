# Board Module

## Overview

A **6x5 match-3 puzzle board** in the style of Tower of Saviors (ToS). The board uses a flat array representation where each cell holds a `GemData` struct. Players drag a gem along a path, swapping with each gem it passes over, then the board resolves matches and cascades.

## Module Files

| File | Purpose | Dependencies |
|---|---|---|
| `board_types` | GemData, GemState, Element enum, CascadeStep, MatchGroup | None (leaf) |
| `board_config` | BoardConfig: dimensions, element pool, min match length | board_types |
| `board_logic` | BoardLogic: core board manipulation and orchestration | board_types, board_config, match_detector, cascade_resolver |
| `match_detector` | MatchDetector: horizontal/vertical scanning and merge | board_types, board_config |
| `cascade_resolver` | CascadeResolver: remove/gravity/spawn loop | board_types, board_config, match_detector |
| `gem_modifier` | GemModifier: status effects, shields, exclusion groups | board_types |
| `board_events` | Event data classes emitted during resolution | board_types |

## BoardConfig

```gdscript
class_name BoardConfig extends RefCounted

var rows: int = 5
var cols: int = 6
var elements: Array[Element] = [
    Element.WATER, Element.FIRE, Element.GRASS,
    Element.LIGHT, Element.DARK, Element.HEART
]
var min_match: int = 3

func index(row: int, col: int) -> int:
    return row * cols + col

func row_of(idx: int) -> int:
    return idx / cols

func col_of(idx: int) -> int:
    return idx % cols

func is_valid(row: int, col: int) -> bool:
    return row >= 0 and row < rows and col >= 0 and col < cols
```

```csharp
public class BoardConfig
{
    public int Rows { get; } = 5;
    public int Cols { get; } = 6;
    public Element[] Elements { get; }
    public int MinMatch { get; } = 3;

    public int Index(int row, int col) => row * Cols + col;
    public int RowOf(int idx) => idx / Cols;
    public int ColOf(int idx) => idx % Cols;
    public bool IsValid(int row, int col) => row >= 0 && row < Rows && col >= 0 && col < Cols;
}
```

The board is a flat `Array[GemData]` of size `rows * cols` (30 cells for 6x5). Position conversion always goes through `BoardConfig` helpers.

## BoardLogic API

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `new` | `config: BoardConfig` | `BoardLogic` | Construct with config |
| `init_board` | `rng: RandomNumberGenerator` | `void` | Fill board with random gems, no initial matches |
| `set_board` | `gems: Array[GemData]` | `void` | Overwrite board state (for testing) |
| `get_gem` | `row: int, col: int` | `GemData` | Read gem at position |
| `set_gem` | `row: int, col: int, gem: GemData` | `void` | Write gem at position |
| `swap_gems` | `idx_a: int, idx_b: int` | `void` | Swap two gems by flat index |
| `move_gem_path` | `path: Array[int]` | `void` | ToS-style drag: move gem along path, swapping at each step |
| `find_matches` | | `Array[MatchGroup]` | Scan board for all matches |
| `resolve_cascade` | | `CascadeResult` | Full cascade loop: detect, remove, gravity, spawn, repeat |
| `get_board_snapshot` | | `Array[GemData]` | Deep copy of current board state |
| `count_element` | `element: Element` | `int` | Count gems of given element on board |
| `get_empty_count` | | `int` | Count empty/null cells |

## Match Detection Algorithm

`MatchDetector` scans in two passes then merges:

### Pass 1: Horizontal Scan
```
For each row (0..rows-1):
    Walk columns left-to-right.
    Track run_start and run_element.
    When element changes or row ends:
        If run_length >= min_match:
            Record MatchGroup(element, indices[run_start..run_end])
    Reset run.
```

### Pass 2: Vertical Scan
```
For each col (0..cols-1):
    Walk rows top-to-bottom.
    Same run-tracking logic as horizontal.
    Record qualifying runs as MatchGroups.
```

### Pass 3: Merge Overlapping Matches
```
For each pair of MatchGroups with same element:
    If they share any index:
        Union their index sets into one MatchGroup.
        Remove the originals, insert the merged group.
Repeat until no more merges possible.
```

This correctly detects L-shapes, T-shapes, and crosses as single matches.

## Cascade Resolution Algorithm

`CascadeResolver` runs a loop that produces an array of `CascadeStep` for animation playback:

```
steps = []
loop:
    matches = match_detector.find_matches(board)
    if matches.empty():
        break

    step = CascadeStep.new()
    step.combo_index = steps.size()
    step.matches = matches

    # 1. Remove matched gems (set to null/empty)
    for match in matches:
        for idx in match.indices:
            step.removed.append({idx: board[idx]})
            board[idx] = null

    # 2. Apply gravity (gems fall into empty spaces)
    for col in range(cols):
        write_row = rows - 1
        for read_row in range(rows - 1, -1, -1):
            idx = config.index(read_row, col)
            if board[idx] != null:
                target = config.index(write_row, col)
                if target != idx:
                    step.falls.append({from: idx, to: target})
                    board[target] = board[idx]
                    board[idx] = null
                write_row -= 1

    # 3. Spawn new gems in empty spaces (top of each column)
    for col in range(cols):
        for row in range(rows):
            idx = config.index(row, col)
            if board[idx] == null:
                board[idx] = random_gem()
                step.spawned.append({idx: board[idx]})

    steps.append(step)

return CascadeResult.new(steps, total_combo=steps.size())
```

Each `CascadeStep` captures everything the UI layer needs to animate one cascade frame: which gems were removed, which fell (with from/to positions), and which spawned.

## GemState: Status Types

Gems carry a `state` field of type `GemState` that modifies match/damage behavior:

| GemState | Description |
|---|---|
| `NORMAL` | Default state, no special behavior |
| `ENHANCED` | Matched gems deal 1.5x damage (glowing gem) |
| `FROZEN` | Cannot be moved for N turns |
| `LOCKED` | Cannot be moved or matched for N turns |
| `POISON` | Deals damage to player when matched |
| `JAMMER` | Non-elemental; cannot contribute to element combos |
| `BOMB` | Explodes in a 3x3 area when matched |
| `MORTAL_POISON` | Stacking poison; damage increases each turn |
| `SHIELD` | Absorbs one modification attempt, then becomes NORMAL |
| `SUPER_ENHANCED` | Clears entire row and column when matched |
| `ABSORB` | Enemy-placed; heals enemy when matched |
| `BLIND` | Hidden element; revealed only when matched |
| `STICKY` | Cannot be swapped away from current position |
| `WEATHERED` | Disappears after N turns if not matched |

## GemModifier

`GemModifier` handles gem state transitions and respects **exclusion groups**:

### Exclusion Groups
Certain states cannot coexist. Applying a new state in the same group replaces the old one:

| Group | States |
|---|---|
| Movement | FROZEN, LOCKED, STICKY |
| Damage | POISON, MORTAL_POISON, ABSORB |
| Enhancement | ENHANCED, SUPER_ENHANCED |

### Shield Absorption
A gem with `SHIELD` state ignores the first state-change attempt:
```gdscript
func apply_state(gem: GemData, new_state: GemState) -> GemData:
    if gem.state == GemState.SHIELD:
        gem.state = GemState.NORMAL  # shield consumed
        return gem                    # new_state not applied
    # ... exclusion group logic, then apply
```

## Code Examples

### Creating a Board and Initializing

```gdscript
# GDScript
var config := BoardConfig.new()
config.rows = 5
config.cols = 6

var board := BoardLogic.new(config)
var rng := RandomNumberGenerator.new()
rng.seed = 12345  # deterministic for testing
board.init_board(rng)
```

```csharp
// C#
var config = new BoardConfig { Rows = 5, Cols = 6 };
var board = new BoardLogic(config);
var rng = new Random(12345);
board.InitBoard(rng);
```

### Setting Up a Specific Board State for Testing

```gdscript
# GDScript -- set up a board with a known horizontal match in row 0
var gems: Array[GemData] = []
for i in range(30):
    gems.append(GemData.new(Element.HEART))

# Place 3 water gems at row 0, cols 0-2
gems[0] = GemData.new(Element.WATER)
gems[1] = GemData.new(Element.WATER)
gems[2] = GemData.new(Element.WATER)

board.set_board(gems)
var matches := board.find_matches()
assert(matches.size() == 1)
assert(matches[0].element == Element.WATER)
assert(matches[0].indices.size() == 3)
```

```csharp
// C#
var gems = new GemData[30];
for (int i = 0; i < 30; i++)
    gems[i] = new GemData(Element.Heart);

gems[0] = new GemData(Element.Water);
gems[1] = new GemData(Element.Water);
gems[2] = new GemData(Element.Water);

board.SetBoard(gems);
var matches = board.FindMatches();
Debug.Assert(matches.Count == 1);
Debug.Assert(matches[0].Element == Element.Water);
Debug.Assert(matches[0].Indices.Count == 3);
```

### Running Cascade Resolution

```gdscript
# GDScript
board.move_gem_path(drag_path)
var result: CascadeResult = board.resolve_cascade()

print("Total combos: ", result.total_combo)
for step in result.steps:
    print("Combo ", step.combo_index + 1, ": ",
          step.matches.size(), " match groups, ",
          step.removed.size(), " gems removed")
```

```csharp
// C#
board.MoveGemPath(dragPath);
CascadeResult result = board.ResolveCascade();

Console.WriteLine($"Total combos: {result.TotalCombo}");
foreach (var step in result.Steps)
{
    Console.WriteLine($"Combo {step.ComboIndex + 1}: " +
        $"{step.Matches.Count} match groups, " +
        $"{step.Removed.Count} gems removed");
}
```

### Reading Cascade Steps for Animation

```gdscript
# GDScript -- UI layer reads steps to drive animations
for step in result.steps:
    # 1. Flash and remove matched gems
    for entry in step.removed:
        var idx: int = entry.keys()[0]
        await animate_remove(idx)

    # 2. Animate gems falling
    for fall in step.falls:
        animate_fall(fall.from, fall.to)
    await all_falls_complete()

    # 3. Spawn new gems with drop-in animation
    for entry in step.spawned:
        var idx: int = entry.keys()[0]
        animate_spawn(idx, entry[idx])
    await all_spawns_complete()
```

## The ToS Drag Mechanic: `move_gem_path`

In Tower of Saviors, the player picks up a gem and drags it across the board. As the dragged gem enters each new cell, it **swaps with the gem in that cell**. The original gems shuffle into the wake of the drag path.

```gdscript
func move_gem_path(path: Array[int]) -> void:
    # path[0] = pickup position, path[1..N] = positions dragged through
    for i in range(1, path.size()):
        swap_gems(path[i - 1], path[i])
```

This is not a "swap two gems" mechanic -- it is a **continuous rearrangement** along the drag path. A skilled player can rearrange the entire board in a single drag.

## Data Flow Diagram

```
User Input           Domain (pure logic)              UI / Presentation
-----------          -------------------              ------------------

  Drag gem     --->  move_gem_path(path)
  (touch/mouse)        |
                       v
                  resolve_cascade()
                       |
                       v
                  Array[CascadeStep]       --->   For each step:
                   .matches                         - highlight matched gems
                   .removed                         - play remove animation
                   .falls                           - tween gems downward
                   .spawned                         - drop in new gems
                       |
                       v
                  CascadeResult            --->   Display combo counter
                   .total_combo                    Pass to CombatResolver
                   .matches_by_element
```

The domain layer produces a complete, deterministic description of what happened. The UI layer reads it and animates. No callbacks, no signals crossing the boundary during resolution -- it is fully synchronous data-in, data-out.
