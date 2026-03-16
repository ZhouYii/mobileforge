class_name MFMonsterTypes extends RefCounted
## Data types for the monster module. NO dependencies.


class MonsterDef extends RefCounted:
	## Immutable definition loaded from JSON.
	var id: int
	var name: String
	var element: int
	var rarity: int
	var max_level: int
	var base_hp: float
	var base_atk: float
	var base_rec: float
	var max_hp: float
	var max_atk: float
	var max_rec: float
	var cost: int
	var active_skill_id: int
	var leader_skill_id: int
	var team_skill_ids: Array[int]
	var evolve_to: int
	var evolve_materials: Array[int]
	var exp_curve: String

	func _init(data: Dictionary) -> void:
		id = data.get("id", 0)
		name = data.get("name", "")
		element = data.get("element", 0)
		rarity = data.get("rarity", 1)
		max_level = data.get("max_level", 1)
		base_hp = data.get("base_hp", 0.0)
		base_atk = data.get("base_atk", 0.0)
		base_rec = data.get("base_rec", 0.0)
		max_hp = data.get("max_hp", 0.0)
		max_atk = data.get("max_atk", 0.0)
		max_rec = data.get("max_rec", 0.0)
		cost = data.get("cost", 1)
		active_skill_id = data.get("active_skill_id", -1)
		leader_skill_id = data.get("leader_skill_id", -1)
		var raw_team_ids = data.get("team_skill_ids", [])
		team_skill_ids = []
		for tid in raw_team_ids:
			team_skill_ids.append(tid)
		evolve_to = data.get("evolve_to", -1)
		var raw_materials = data.get("evolve_materials", [])
		evolve_materials = []
		for mat in raw_materials:
			evolve_materials.append(mat)
		exp_curve = data.get("exp_curve", "standard")


class MonsterInstance extends RefCounted:
	## Mutable player-owned monster.
	var instance_id: int  ## Unique ID for this specific instance
	var def_id: int  ## References MonsterDef.id
	var level: int
	var exp: int
	var skill_level: int
	var plus_hp: int  ## Bonus stats from fusing +eggs
	var plus_atk: int
	var plus_rec: int
	var is_favorite: bool

	func _init(p_instance_id: int, p_def_id: int) -> void:
		instance_id = p_instance_id
		def_id = p_def_id
		level = 1
		exp = 0
		skill_level = 1
		plus_hp = 0
		plus_atk = 0
		plus_rec = 0
		is_favorite = false

	func to_dict() -> Dictionary:
		return {
			"instance_id": instance_id,
			"def_id": def_id,
			"level": level,
			"exp": exp,
			"skill_level": skill_level,
			"plus_hp": plus_hp,
			"plus_atk": plus_atk,
			"plus_rec": plus_rec,
			"is_favorite": is_favorite,
		}

	static func from_dict(data: Dictionary) -> MonsterInstance:
		var inst := MonsterInstance.new(
			data.get("instance_id", 0),
			data.get("def_id", 0)
		)
		inst.level = data.get("level", 1)
		inst.exp = data.get("exp", 0)
		inst.skill_level = data.get("skill_level", 1)
		inst.plus_hp = data.get("plus_hp", 0)
		inst.plus_atk = data.get("plus_atk", 0)
		inst.plus_rec = data.get("plus_rec", 0)
		inst.is_favorite = data.get("is_favorite", false)
		return inst


class MonsterStats extends RefCounted:
	## Calculated stats at a specific level.
	var hp: int
	var atk: int
	var rec: int

	func _init(p_hp: int, p_atk: int, p_rec: int) -> void:
		hp = p_hp
		atk = p_atk
		rec = p_rec
