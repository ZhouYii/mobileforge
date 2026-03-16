class_name MFTeamTypes extends RefCounted


class Team extends RefCounted:
	## A team of monsters.
	var slots: Array  # Array of MonsterInstance or null (fixed size)
	var max_slots: int

	func _init(p_max_slots: int = 6) -> void:
		max_slots = p_max_slots
		slots = []
		slots.resize(max_slots)
		for i in range(max_slots):
			slots[i] = null

	func get_slot(index: int) -> RefCounted:  # MonsterInstance or null
		if index < 0 or index >= max_slots:
			return null
		return slots[index]

	func set_slot(index: int, monster: RefCounted) -> void:
		if index >= 0 and index < max_slots:
			slots[index] = monster

	func get_filled_count() -> int:
		var count := 0
		for slot in slots:
			if slot != null:
				count += 1
		return count

	func get_leader() -> RefCounted:  # slots[0]
		return slots[0]

	func get_friend() -> RefCounted:  # slots[max_slots - 1]
		return slots[max_slots - 1]

	func to_array() -> Array:  # non-null monsters
		var arr: Array = []
		for slot in slots:
			if slot != null:
				arr.append(slot)
		return arr


class TeamStats extends RefCounted:
	## Aggregated team statistics.
	var total_hp: int
	var total_atk: int
	var total_rec: int
	var total_cost: int
	var member_count: int

	func _init(p_hp: int, p_atk: int, p_rec: int, p_cost: int, p_count: int) -> void:
		total_hp = p_hp
		total_atk = p_atk
		total_rec = p_rec
		total_cost = p_cost
		member_count = p_count
