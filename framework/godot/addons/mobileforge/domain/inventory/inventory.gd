class_name MFInventory extends RefCounted
## Generic inventory system supporting stackable items and unique equipment.

signal inventory_changed(item_id: StringName, old_count: int, new_count: int)
signal item_equipped(slot_type: StringName, item_id: StringName)
signal item_unequipped(slot_type: StringName, item_id: StringName)

var _stacks: Dictionary = {}  # item_id -> {count: int, instance_data: Dictionary or null}
var _equipped: Dictionary = {}  # slot_type -> item_id
var _item_defs: Dictionary = {}  # item_id -> def dict (optional, for type/tag lookups)
var _max_slots: int = 0  # 0 = unlimited


## Set max inventory slots (0 = unlimited).
func set_max_slots(max_slots: int) -> void:
	_max_slots = max_slots


## Register item definitions for type/tag lookups.
func register_item_defs(defs: Array) -> void:
	for def in defs:
		_item_defs[def.id] = def


## Add an item. Returns actual amount added (may be less if full).
func add_item(id: StringName, count: int = 1, instance_data: Dictionary = {}) -> int:
	if count <= 0:
		return 0
	if _max_slots > 0 and not _stacks.has(id) and _stacks.size() >= _max_slots:
		return 0  # No room for new stack
	var old_count := 0
	if _stacks.has(id):
		old_count = _stacks[id].count
	var max_stack := _get_max_stack(id)
	var new_count := mini(old_count + count, max_stack)
	var added := new_count - old_count
	if added <= 0:
		return 0
	if _stacks.has(id):
		_stacks[id].count = new_count
		if not instance_data.is_empty():
			_stacks[id].instance_data = instance_data
	else:
		_stacks[id] = {"count": new_count, "instance_data": instance_data if not instance_data.is_empty() else null}
	inventory_changed.emit(id, old_count, new_count)
	return added


## Remove items. Returns actual amount removed.
func remove_item(id: StringName, count: int = 1) -> int:
	if not _stacks.has(id) or count <= 0:
		return 0
	var old_count: int = _stacks[id].count
	var removed := mini(count, old_count)
	var new_count := old_count - removed
	if new_count <= 0:
		_stacks.erase(id)
	else:
		_stacks[id].count = new_count
	inventory_changed.emit(id, old_count, new_count)
	return removed


## Check if inventory contains at least count of item.
func has_item(id: StringName, count: int = 1) -> bool:
	if not _stacks.has(id):
		return false
	return _stacks[id].count >= count


## Get count of an item (0 if not present).
func get_count(id: StringName) -> int:
	if not _stacks.has(id):
		return 0
	return _stacks[id].count


## Get a stack entry: {count: int, instance_data: Dictionary or null}
func get_stack(id: StringName) -> Dictionary:
	if not _stacks.has(id):
		return {}
	return _stacks[id].duplicate(true)


## Get all stacks as Dictionary of id -> {count, instance_data}
func get_all_stacks() -> Dictionary:
	return _stacks.duplicate(true)


## Get items filtered by type (requires item defs registered).
func get_by_type(type: StringName) -> Array:
	var result: Array = []
	for id in _stacks:
		if _item_defs.has(id) and _item_defs[id].get("type", &"") == type:
			result.append(id)
	return result


## Get items filtered by tag (requires item defs registered).
func get_by_tag(tag: StringName) -> Array:
	var result: Array = []
	for id in _stacks:
		if _item_defs.has(id) and tag in _item_defs[id].get("tags", []):
			result.append(id)
	return result


## Whether inventory is full (only meaningful when max_slots > 0).
func is_full() -> bool:
	if _max_slots <= 0:
		return false
	return _stacks.size() >= _max_slots


## Number of occupied slots.
func slot_count() -> int:
	return _stacks.size()


# ── Equipment ──

## Equip an item to a slot. Returns previously equipped item ID or empty.
func equip(slot_type: StringName, item_id: StringName) -> StringName:
	if not _stacks.has(item_id):
		return &""
	var prev: StringName = _equipped.get(slot_type, &"")
	_equipped[slot_type] = item_id
	if prev != &"":
		item_unequipped.emit(slot_type, prev)
	item_equipped.emit(slot_type, item_id)
	return prev


## Unequip from a slot. Returns the item ID that was unequipped, or empty.
func unequip(slot_type: StringName) -> StringName:
	if not _equipped.has(slot_type):
		return &""
	var item_id: StringName = _equipped[slot_type]
	_equipped.erase(slot_type)
	item_unequipped.emit(slot_type, item_id)
	return item_id


## Get equipped item ID for a slot, or empty if nothing equipped.
func get_equipped(slot_type: StringName) -> StringName:
	return _equipped.get(slot_type, &"")


## Get all equipped slots as Dictionary of slot_type -> item_id.
func get_all_equipped() -> Dictionary:
	return _equipped.duplicate()


# ── Persistence ──

func to_save_dict() -> Dictionary:
	return {
		"stacks": _stacks.duplicate(true),
		"equipped": _equipped.duplicate(),
	}


func from_save_dict(data: Dictionary) -> void:
	_stacks = data.get("stacks", {}).duplicate(true)
	_equipped = data.get("equipped", {}).duplicate()


func _get_max_stack(id: StringName) -> int:
	if _item_defs.has(id):
		return _item_defs[id].get("max_stack", 9999)
	return 9999
