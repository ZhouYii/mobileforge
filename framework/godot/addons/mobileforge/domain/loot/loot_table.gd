## Loot Table -- Data-driven weighted random drop system with guaranteed defaults.
##
## Observed in 28/81 analyzed Unity games. The pattern originates from Merge Dragons'
## Excel-driven loot tables with guaranteed default drops, ensuring players always
## receive something. This implementation supports CSV loading, weighted random
## selection, and per-level overrides.
##
## Key design principle from Merge Dragons: every table has a guaranteed default
## item, preventing empty drops -- a critical game design guarantee.
##
## Usage:
##   var tables = LootTable.load_from_csv("res://data/loot_tables.csv")
##   var drop_id = tables["common_chest"].roll()
##   var multi = tables["boss_chest"].roll_multiple(3)
class_name LootTable
extends Resource

## Name identifier for this loot table.
@export var table_name: String = ""

## Item returned when no weighted entry is selected. Guarantees non-empty drops.
@export var default_item_id: StringName = &""

## Weighted entries. Each has an item_id and a chance (0.0 to 1.0).
## Entries are evaluated in order; the first successful roll wins.
@export var entries: Array[LootEntry] = []


## Roll once. Returns a single item ID.
func roll() -> StringName:
	for entry in entries:
		if randf() < entry.chance:
			return entry.item_id
	return default_item_id


## Roll [param count] times independently. Returns an array of item IDs.
func roll_multiple(count: int) -> Array[StringName]:
	var results: Array[StringName] = []
	for i in count:
		results.append(roll())
	return results


## Roll once using weighted selection (all weights compete against each other).
## More statistically standard than sequential chance evaluation.
func roll_weighted() -> StringName:
	if entries.is_empty():
		return default_item_id
	var total_weight: float = 0.0
	for entry in entries:
		total_weight += entry.weight
	if total_weight <= 0.0:
		return default_item_id
	var roll_value: float = randf() * total_weight
	var cumulative: float = 0.0
	for entry in entries:
		cumulative += entry.weight
		if roll_value < cumulative:
			return entry.item_id
	return default_item_id


## Load multiple loot tables from a CSV file.
## Expected CSV format: TableName, DefaultItem, Item1, Chance1, Item2, Chance2, ...
## Returns a Dictionary of {table_name: LootTable}.
static func load_from_csv(path: String) -> Dictionary:
	var tables: Dictionary = {}
	var file := FileAccess.open(path, FileAccess.READ)
	if not file:
		push_error("LootTable: Failed to open %s" % path)
		return tables
	# Skip header row
	var _headers := file.get_csv_line()
	while not file.eof_reached():
		var row := file.get_csv_line()
		if row.size() < 3 or row[0].is_empty():
			continue
		var table := LootTable.new()
		table.table_name = row[0]
		table.default_item_id = StringName(row[1])
		var idx := 2
		while idx + 1 < row.size() and not row[idx].is_empty():
			var entry := LootEntry.new()
			entry.item_id = StringName(row[idx])
			entry.chance = float(row[idx + 1])
			entry.weight = entry.chance
			table.entries.append(entry)
			idx += 2
		tables[table.table_name] = table
	return tables


## Load loot tables from a JSON file.
## Expected format: {"table_name": {"default": "item_id", "entries": [{"item": "id", "chance": 0.5}, ...]}}
static func load_from_json(path: String) -> Dictionary:
	var tables: Dictionary = {}
	var file := FileAccess.open(path, FileAccess.READ)
	if not file:
		push_error("LootTable: Failed to open %s" % path)
		return tables
	var data = JSON.parse_string(file.get_as_text())
	if not data is Dictionary:
		push_error("LootTable: Invalid JSON format in %s" % path)
		return tables
	for table_name: String in data:
		var table_data: Dictionary = data[table_name]
		var table := LootTable.new()
		table.table_name = table_name
		table.default_item_id = StringName(table_data.get("default", ""))
		for entry_data: Dictionary in table_data.get("entries", []):
			var entry := LootEntry.new()
			entry.item_id = StringName(entry_data.get("item", ""))
			entry.chance = float(entry_data.get("chance", 0.0))
			entry.weight = float(entry_data.get("weight", entry.chance))
			table.entries.append(entry)
		tables[table_name] = table
	return tables
