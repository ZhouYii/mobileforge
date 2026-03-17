## Loot Entry -- A single entry in a LootTable defining an item and its drop probability.
##
## Used by LootTable for both sequential chance evaluation and weighted selection.
class_name LootEntry
extends Resource

## The item identifier to drop.
@export var item_id: StringName = &""

## Probability of this item dropping in sequential evaluation (0.0 to 1.0).
## In sequential mode, each entry is checked in order; the first success wins.
@export_range(0.0, 1.0) var chance: float = 0.0

## Weight for weighted random selection. Higher weight = more likely.
## Only used by LootTable.roll_weighted().
@export var weight: float = 1.0

## Minimum player level required for this entry to be eligible.
@export var min_level: int = 0

## Maximum number of times this entry can drop per session/day (0 = unlimited).
@export var max_drops: int = 0
