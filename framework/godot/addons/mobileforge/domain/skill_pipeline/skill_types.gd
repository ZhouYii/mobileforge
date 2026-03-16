class_name MFSkillTypes extends RefCounted
## Data types for the skill system. NO dependencies.


class SkillDef extends RefCounted:
	## Loaded from JSON, immutable.
	var id: int
	var name: String
	var description: String
	var type: String  # "active", "leader", "team"
	var max_cd: int  # max cooldown (active skills only)
	var min_cd: int  # min cooldown after max skill level
	var max_level: int
	var rules: Array  # Array[SkillRule]

	func _init(data: Dictionary) -> void:
		id = int(data.get("id", 0))
		name = data.get("name", "")
		description = data.get("description", "")
		type = data.get("type", "active")
		max_cd = int(data.get("max_cd", 0))
		min_cd = int(data.get("min_cd", 0))
		max_level = int(data.get("max_level", 1))
		rules = []
		for rule_data in data.get("rules", []):
			rules.append(SkillRule.new(rule_data))


class SkillRule extends RefCounted:
	## One rule within a skill.
	var conditions: Array[Dictionary]  # each: {type: String, params: Dictionary}
	var outcomes: Array[Dictionary]  # each: {type: String, params: Dictionary, duration: int}

	func _init(data: Dictionary) -> void:
		conditions = []
		for c in data.get("conditions", []):
			conditions.append(c)
		outcomes = []
		for o in data.get("outcomes", []):
			outcomes.append(o)


class SkillContext extends RefCounted:
	## Mutable context passed during skill activation.
	var caster_index: int  # team slot of caster
	var team: Array  # Array of MonsterInstance references
	var team_stats: Array  # Array of MonsterStats
	var board: RefCounted  # MFBoardLogic reference (nullable)
	var combat: RefCounted  # MFCombatResolver reference (nullable)
	var enemies: Array  # Array of EnemyState references
	var team_hp: int
	var max_hp: int
	var combo_count: int
	var elements_matched: Dictionary  # element_id -> count
	var turn_number: int
	var extra: Dictionary

	func _init() -> void:
		caster_index = 0
		team = []
		team_stats = []
		board = null
		combat = null
		enemies = []
		team_hp = 0
		max_hp = 0
		combo_count = 0
		elements_matched = {}
		turn_number = 0
		extra = {}


class SkillResult extends RefCounted:
	## Output from skill activation.
	var board_changes: Array[Dictionary]  # {pos: int, old_element: int, new_element: int}
	var damage_dealt: Dictionary  # enemy_index -> damage
	var healing: int
	var buffs_applied: Array[Dictionary]  # {type: String, turns: int, target: String}
	var enemies_killed: Array[int]

	func _init() -> void:
		board_changes = []
		damage_dealt = {}
		healing = 0
		buffs_applied = []
		enemies_killed = []
