# Tower of Saviors Board Mechanics

## Overview

The ToS board is a **6-column by 5-row** grid of colored gems. Each gem belongs to one of six elements. The player drags a gem across the board, swapping it with every gem along the drag path, then the board resolves matches and cascades. The number and type of matches determine attack damage, healing, and skill activation.

## The 6x5 Flat Array Board

The board is stored as a flat `Array` of 30 `GemState` objects. Position mapping uses row-major order with the origin at the top-left corner:

```
Position layout (rows x cols = 5 x 6):

 0  1  2  3  4  5      <- Row 0 (top)
 6  7  8  9 10 11      <- Row 1
12 13 14 15 16 17      <- Row 2
18 19 20 21 22 23      <- Row 3
24 25 26 27 28 29      <- Row 4 (bottom)
```

Conversion helpers in `MFBoardConfig`:

| Method | Formula | Example |
|---|---|---|
| `rc_to_pos(row, col)` | `row * cols + col` | `rc_to_pos(2, 3) = 15` |
| `pos_to_row(pos)` | `pos / cols` | `pos_to_row(15) = 2` |
| `pos_to_col(pos)` | `pos % cols` | `pos_to_col(15) = 3` |

## Elements

Six gem elements are defined in `MFBoardTypes.Element`:

| ID | Element | Color | Role |
|---|---|---|---|
| 1 | Water | Blue | Damage type |
| 2 | Fire | Red | Damage type |
| 3 | Grass | Green | Damage type |
| 4 | Light | Gold | Damage type |
| 5 | Dark | Purple | Damage type |
| 6 | Heart | Pink | Healing (matched hearts restore HP) |

Heart gems do not deal damage. Instead, they trigger healing based on the team's total REC stat, combo multiplier, and number of heart gems matched.

## Gem Matching Rules

### Minimum Match Length

A match requires **3 or more** gems of the same element in a contiguous horizontal or vertical line. Diagonal lines do not count.

### Match Detection Algorithm

`MFMatchDetector` scans in two passes then merges overlapping results:

1. **Horizontal scan**: For each row, walk left to right tracking runs of the same element. When a run of 3+ is found, record it as a `MatchResult`.

2. **Vertical scan**: For each column, walk top to bottom with the same run-tracking logic.

3. **Merge overlapping**: If two `MatchResult` objects share any position AND have the same element, they are merged into a single match. This correctly identifies L-shapes, T-shapes, and cross patterns as single matches rather than two separate matches.

### Match Result Data

Each `MatchResult` contains:
- `element: int` -- the element matched
- `positions: Array[int]` -- all flat-index positions in the match
- `combo_index: int` -- which combo number this is within the cascade
- `gem_count: int` -- `positions.size()` (convenience getter)

### Locked and Petrified Gems

Gems with `LOCKED` or `PETRIFIED` status are excluded from matching. The match detector skips them during scanning, treating them as run-breakers.

## Cascade Resolution

After the player finishes dragging, the board enters cascade resolution. `MFCascadeResolver.resolve()` runs a loop:

```
While matches exist on the board:
    1. Detect all matches
    2. Remove matched gems (set positions to null)
    3. Apply gravity (gems above empty spaces fall down)
    4. Spawn new gems in empty top-row positions
    5. Record this step as a CascadeStep
    6. Repeat
```

The loop continues until no matches remain. Each iteration produces a `CascadeStep` that captures:

| Field | Type | Description |
|---|---|---|
| `matches` | `Array[MatchResult]` | All matches found in this step |
| `removed_positions` | `Array[int]` | Flat positions of all removed gems |
| `drops` | `Array[Dictionary]` | `{from: int, to: int}` pairs for falling gems |
| `spawned` | `Array[Dictionary]` | `{position: int, element: int}` for new gems |
| `step_index` | `int` | Zero-based step number within the cascade |

### Gravity

Gravity processes one column at a time, bottom to top. A "write pointer" starts at the bottom row and moves upward as non-null gems are found and placed:

```
Before gravity (column 2):     After gravity (column 2):
  Row 0: [null]                  Row 0: [new gem]
  Row 1: [Water]                 Row 1: [new gem]
  Row 2: [null]        -->       Row 2: [Water]
  Row 3: [Fire]                  Row 3: [Fire]
  Row 4: [null]                  Row 4: [Grass]
                                 (Grass was at some other row)
```

### Spawning

After gravity, any remaining null positions at the top of each column are filled with randomly chosen gems from the element pool. Spawned gems can create new matches, which is why the cascade loops.

### Combo Counting

Each `MatchResult` in each `CascadeStep` increments the combo counter. The total combo count across all cascade steps determines the **combo multiplier** used in damage calculation:

```
combo_multiplier = 1.0 + (combo_count - 1) * 0.25
```

| Combos | Multiplier |
|---|---|
| 1 | 1.00x |
| 2 | 1.25x |
| 3 | 1.50x |
| 5 | 2.00x |
| 8 | 2.75x |
| 10 | 3.25x |

## The Drag Mechanic

Tower of Saviors uses a distinctive drag mechanic that sets it apart from other match-3 games. Instead of swapping two adjacent gems, the player:

1. **Picks up** a gem at position A.
2. **Drags** it across the board through a sequence of positions.
3. At each position the dragged gem enters, it **swaps** with the gem currently there.
4. **Releases** when time expires or the player lifts their finger.

This means a single drag can rearrange the entire board. The gems left in the wake of the drag path are shifted by one position along the path.

### Implementation

`MFBoardLogic.move_gem_path(start_pos, path)` implements this:

```gdscript
func move_gem_path(start_pos: int, path: Array[int]) -> void:
    var current := start_pos
    for next_pos in path:
        swap_gems(current, next_pos)
        current = next_pos
```

Each `swap_gems` call checks for `FROZEN` and `PETRIFIED` statuses -- frozen/petrified gems cannot be moved, and the swap fails silently.

### Example

Starting board state (showing row 2 only, positions 12-17):

```
[Water] [Fire] [Grass] [Light] [Dark] [Heart]
  12      13     14      15      16      17
```

Drag Water gem at position 12 along path [13, 14, 15]:

- Step 1: Swap 12 and 13 -> `[Fire] [Water] [Grass] [Light] [Dark] [Heart]`
- Step 2: Swap 13 and 14 -> `[Fire] [Grass] [Water] [Light] [Dark] [Heart]`
- Step 3: Swap 14 and 15 -> `[Fire] [Grass] [Light] [Water] [Dark] [Heart]`

Result: Water moved from position 12 to 15, and Fire/Grass/Light each shifted one position left.

## Gem Status Types

Gems can carry status effects that modify their matching and movement behavior. Statuses are tracked as an array of `{type, turns, data}` dictionaries on each `GemState`.

| Status | ID | Movement | Matching | Description |
|---|---|---|---|---|
| `FROZEN` | 1 | Blocked | Allowed | Cannot be moved by drag. Can still be matched if adjacent gems form a line. |
| `LOCKED` | 2 | Allowed | Blocked | Can be moved but does not participate in match detection. |
| `WEATHERED` | 3 | Allowed | Allowed | Disappears after N turns if not matched. |
| `BURNING` | 4 | Allowed | Allowed | Deals damage to the player each turn. |
| `STICKY` | 5 | Special | Allowed | Swaps with an adjacent gem when moved (implementation-specific). |
| `POISONED` | 6 | Allowed | Allowed | Converts to poison element when matched. |
| `ENCHANTED` | 7 | Allowed | Allowed | Counts as two elements simultaneously. |
| `PETRIFIED` | 8 | Blocked | Blocked | Cannot be moved or matched. Fully inert. |
| `HIDDEN` | 9 | Allowed | Allowed | Element is hidden from the player until the gem is moved. |
| `MARKED` | 10 | Allowed | Allowed | Deals extra damage when matched. |
| `CHAINED` | 11 | Allowed | Special | Requires multiple matches adjacent to free. |
| `TRANSMUTED` | 12 | Allowed | Allowed | Element has been changed from its original. |
| `SHIELDED` | 13 | Allowed | Allowed | Absorbs one incoming status modification, then becomes normal. |

### Exclusion Groups

Certain statuses cannot coexist on the same gem. Applying a status from the same group replaces the existing one:

| Group | Statuses | Rationale |
|---|---|---|
| Movement Block | `FROZEN`, `PETRIFIED` | Both prevent movement; only one can apply. |
| Element Modify | `ENCHANTED`, `TRANSMUTED` | Both alter the gem's effective element. |

### Shield Absorption

A `SHIELDED` gem absorbs the first status modification attempt. When any other status is applied to a shielded gem:
1. The shield is consumed (removed).
2. The incoming status is discarded.
3. The gem becomes status-free (unless it had other statuses).

This is handled by `MFGemModifier.apply_status()`.

## Board Initialization

`MFBoardLogic.init_board()` fills all 30 positions with random gems, ensuring **no initial matches**. For each position, the algorithm checks the two gems to the left and the two gems above. If placing a gem would create a 3-in-a-row, that element is removed from the candidate pool before random selection.

## Data Flow: Board to Combat

```
Player drags gem
       |
       v
MFBoardLogic.move_gem_path(start, path)
       |
       v
MFCascadeResolver.resolve(board) -> Array[CascadeStep]
       |
       v
MFDungeonRunner.execute_player_turn(cascade_steps, team, team_stats)
       |
       +--> Aggregate matches by element
       +--> For each combo, for each team monster, for each enemy:
       |      MFCombatResolver.resolve_player_attack(damage_context) -> DamageResult
       +--> Apply heart gem healing
       +--> Check wave clear / battle end
       |
       v
TurnResult (damage_per_enemy, healing, enemies_killed, wave_cleared, battle_won)
```

The board module is purely concerned with gem positions, matches, and cascades. It produces data that the combat and dungeon modules consume. No combat logic leaks into the board module.
