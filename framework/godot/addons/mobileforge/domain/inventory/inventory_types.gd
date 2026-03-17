class_name MFInventoryTypes extends RefCounted
## Type definitions for the inventory system.

## Item types
const TYPE_MATERIAL := &"material"      # Stackable (count-based)
const TYPE_EQUIPMENT := &"equipment"    # Unique (instance_data)
const TYPE_CONSUMABLE := &"consumable"  # Stackable, usable
const TYPE_KEY_ITEM := &"key_item"      # Unique, non-removable


## Create an item definition dictionary.
static func create_item_def(
	id: StringName,
	name: String,
	type: StringName = TYPE_MATERIAL,
	max_stack: int = 9999,
	tags: Array = [],
	data: Dictionary = {},
) -> Dictionary:
	return {
		"id": id,
		"name": name,
		"type": type,
		"max_stack": max_stack,
		"tags": tags,
		"data": data,
	}
