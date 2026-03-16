class_name MFEnemyTypes extends RefCounted


class EnemyState extends RefCounted:
	## Runtime state of one enemy.
	var id: int  # unique id within the wave
	var def_data: Dictionary  # raw definition data
	var name: String
	var element: int
	var hp: int
	var max_hp: int
	var atk: float
	var defense: float
	var countdown: int  # turns until next attack
	var max_countdown: int
	var status_effects: Array[Dictionary]  # {type: String, turns: int, data: Variant}
	var is_alive: bool:
		get:
			return hp > 0

	func _init(data: Dictionary) -> void:
		def_data = data
		name = data.get("name", "Enemy")
		element = int(data.get("element", 0))
		hp = int(data.get("hp", 1))
		max_hp = hp
		atk = float(data.get("atk", 0.0))
		defense = float(data.get("defense", 0.0))
		countdown = int(data.get("countdown", 1))
		max_countdown = countdown
		status_effects = []

	func take_damage(amount: int) -> int:
		## Returns actual damage dealt (clamped to hp).
		var actual := mini(amount, hp)
		hp -= actual
		return actual

	func heal(amount: int) -> void:
		hp = mini(hp + amount, max_hp)

	func has_status(type: String) -> bool:
		for status in status_effects:
			if status["type"] == type:
				return true
		return false

	func add_status(type: String, turns: int, data: Variant = null) -> void:
		status_effects.append({"type": type, "turns": turns, "data": data})

	func remove_status(type: String) -> void:
		for i in range(status_effects.size() - 1, -1, -1):
			if status_effects[i]["type"] == type:
				status_effects.remove_at(i)


class EnemyAction extends RefCounted:
	## What an enemy does on its turn.
	var type: String  # "attack", "skill", "buff", "debuff"
	var damage: int
	var target: String  # "all", "single", "random"
	var extra: Dictionary

	func _init(p_type: String, p_damage: int = 0, p_target: String = "all") -> void:
		type = p_type
		damage = p_damage
		target = p_target
		extra = {}
