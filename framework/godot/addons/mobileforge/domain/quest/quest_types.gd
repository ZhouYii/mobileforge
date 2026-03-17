class_name MFQuestTypes extends RefCounted
## Type definitions and factory methods for the quest system.


## Quest categories
const CATEGORY_DAILY := &"daily"
const CATEGORY_WEEKLY := &"weekly"
const CATEGORY_ACHIEVEMENT := &"achievement"
const CATEGORY_STORY := &"story"

## Quest status
const STATUS_LOCKED := &"locked"
const STATUS_ACTIVE := &"active"
const STATUS_COMPLETED := &"completed"
const STATUS_CLAIMED := &"claimed"


## Create a quest definition dictionary.
static func create_quest_def(
	id: StringName,
	name: String,
	category: StringName = CATEGORY_DAILY,
	objectives: Array = [],
	rewards: Array = [],
	prerequisites: Array = [],
	repeatable: bool = false,
) -> Dictionary:
	return {
		"id": id,
		"name": name,
		"category": category,
		"objectives": objectives,
		"rewards": rewards,
		"prerequisites": prerequisites,
		"repeatable": repeatable,
	}


## Create a quest objective dictionary.
## event_name: which EventBus event triggers progress
## filter: optional dict — payload must contain all these key-value pairs to match
## target_count: how many times the event must fire to complete this objective
static func create_objective(
	event_name: StringName,
	target_count: int = 1,
	filter: Dictionary = {},
) -> Dictionary:
	return {
		"event_name": event_name,
		"target_count": target_count,
		"filter": filter,
	}
