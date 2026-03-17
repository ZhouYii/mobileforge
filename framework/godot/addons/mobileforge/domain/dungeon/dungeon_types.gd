class_name MFDungeonTypes extends RefCounted


class DungeonDef extends RefCounted:
	## Loaded from JSON.
	var id: int
	var name: String
	var zone: String  ## Dungeon zone/group (e.g. "Aqua Temple") for grouping difficulties
	var difficulty: String  ## "normal", "expert", "mythical", "annihilation"
	var difficulty_rank: int  ## Numeric sort order: 0=normal, 1=expert, 2=mythical, 3=annihilation
	var stamina_cost: int
	var turn_limit: int  ## 0 = no limit; >0 = lose if turn_number exceeds this
	var board_rows: int  ## 0 = use default (5). Common: 5, 6, 7
	var board_cols: int  ## 0 = use default (6). Common: 6
	var available_days: Array[int]  ## Weekdays this dungeon is available (0=Sun..6=Sat). Empty = always.
	var floor_effects: Array[String]  ## Active modifiers for this dungeon
	var waves: Array  # Array[WaveDef]
	var rewards: Array[Dictionary]  # {type: String, id: int, count: int}

	## Known floor effects:
	##   "no_active_skills" — active skills cannot be used
	##   "no_heart_heal" — heart gem matches don't heal
	##   "enemy_hp_regen" — enemies recover 5% max HP per turn
	##   "element_restrict_X" — only element X deals damage (1-5)
	##   "fixed_move_time" — reduces gem move time (visual only in real ToS)
	##   "no_skyfall_combos" — gems don't cascade (no skyfall after initial match)
	##   "poison_floor" — team takes 5% max HP damage per turn

	func _init(data: Dictionary) -> void:
		id = int(data.get("id", 0))
		name = data.get("name", "")
		zone = data.get("zone", "")
		difficulty = data.get("difficulty", "normal")
		difficulty_rank = _difficulty_to_rank(difficulty)
		stamina_cost = int(data.get("stamina_cost", 0))
		turn_limit = int(data.get("turn_limit", 0))
		board_rows = int(data.get("board_rows", 0))
		board_cols = int(data.get("board_cols", 0))
		available_days = []
		for d in data.get("available_days", []):
			available_days.append(int(d))
		floor_effects = []
		for fe in data.get("floor_effects", []):
			floor_effects.append(str(fe))
		waves = []
		for wave_data in data.get("waves", []):
			waves.append(WaveDef.new(wave_data))
		rewards = []
		for r in data.get("rewards", []):
			rewards.append(r)

	## Check if a specific floor effect is active.
	func has_floor_effect(effect: String) -> bool:
		return effect in floor_effects

	## Check if this dungeon is available today (or always if no day restriction).
	func is_available_today() -> bool:
		if available_days.is_empty():
			return true
		var today_weekday: int = Time.get_date_dict_from_system()["weekday"]
		return today_weekday in available_days

	## Check if this is a daily rotating dungeon.
	func is_daily() -> bool:
		return not available_days.is_empty()

	static func _difficulty_to_rank(diff: String) -> int:
		match diff:
			"normal": return 0
			"expert": return 1
			"mythical": return 2
			"annihilation": return 3
			_: return 0


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
	var turn_limit_exceeded: bool  ## True if the dungeon's turn limit was exceeded
	var rewards: Array[Dictionary]
	var exp_gained: int  ## Total EXP earned from this dungeon clear

	func _init() -> void:
		damage_per_enemy = {}
		healing = 0
		enemies_killed = []
		enemy_attacks = []
		wave_cleared = false
		battle_ended = false
		battle_won = false
		turn_limit_exceeded = false
		rewards = []
		exp_gained = 0


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
