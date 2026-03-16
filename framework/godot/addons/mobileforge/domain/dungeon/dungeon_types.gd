class_name MFDungeonTypes extends RefCounted


class DungeonDef extends RefCounted:
	## Loaded from JSON.
	var id: int
	var name: String
	var stamina_cost: int
	var waves: Array  # Array[WaveDef]
	var rewards: Array[Dictionary]  # {type: String, id: int, count: int}

	func _init(data: Dictionary) -> void:
		id = int(data.get("id", 0))
		name = data.get("name", "")
		stamina_cost = int(data.get("stamina_cost", 0))
		waves = []
		for wave_data in data.get("waves", []):
			waves.append(WaveDef.new(wave_data))
		rewards = []
		for r in data.get("rewards", []):
			rewards.append(r)


class WaveDef extends RefCounted:
	## One wave of enemies.
	var enemies: Array[Dictionary]  # each enemy def data

	func _init(data: Dictionary) -> void:
		enemies = []
		for e in data.get("enemies", []):
			enemies.append(e)


class TurnResult extends RefCounted:
	## Output from executing one turn.
	var damage_per_enemy: Dictionary  # enemy_id -> total damage
	var healing: int
	var enemies_killed: Array[int]  # enemy ids
	var enemy_attacks: Array[Dictionary]  # {enemy_id: int, damage: int}
	var wave_cleared: bool
	var battle_ended: bool
	var battle_won: bool
	var rewards: Array[Dictionary]

	func _init() -> void:
		damage_per_enemy = {}
		healing = 0
		enemies_killed = []
		enemy_attacks = []
		wave_cleared = false
		battle_ended = false
		battle_won = false
		rewards = []


class DungeonState extends RefCounted:
	## Current state of a dungeon run.
	var dungeon_def: RefCounted  # DungeonDef
	var current_wave_index: int
	var enemies: Array  # Array[EnemyState]
	var team_hp: int
	var max_hp: int
	var turn_number: int
	var total_combos: int
	var is_active: bool

	func _init() -> void:
		dungeon_def = null
		current_wave_index = 0
		enemies = []
		team_hp = 0
		max_hp = 0
		turn_number = 0
		total_combos = 0
		is_active = false
