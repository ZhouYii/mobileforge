extends Node
## Main entry point for Tower of Saviors. Wires framework modules.

var _game_data: Node  # Framework GameData autoload
var _player_state: Node  # Framework PlayerState autoload
var _event_bus: Node  # Framework EventBus autoload
var _economy: MFEconomy
var _monster_manager: MFMonsterManager
var _skill_pipeline: MFSkillPipeline
var _ui_router: Node  # Framework UIRouter


func _ready() -> void:
	_event_bus = get_node("/root/EventBus")
	_game_data = get_node("/root/GameData")
	_player_state = get_node("/root/PlayerState")

	# Load game data
	_game_data.load_definitions(&"monsters", "res://games/tower-of-saviors/shared/data/monsters.json")
	_game_data.load_definitions(&"skills", "res://games/tower-of-saviors/shared/data/skills.json")
	_game_data.load_definitions(&"leader_skills", "res://games/tower-of-saviors/shared/data/leader_skills.json")
	_game_data.load_definitions(&"stages", "res://games/tower-of-saviors/shared/data/stages.json")
	_game_data.load_definitions(&"gacha_pools", "res://games/tower-of-saviors/shared/data/gacha_pools.json")

	# Initialize player state sections
	_player_state.register_section(&"currencies", {"gems": 50, "coins": 10000, "stamina": 100})
	_player_state.register_section(&"monsters", {})
	_player_state.register_section(&"teams", {})
	_player_state.register_section(&"progress", {"rank": 1, "exp": 0})

	# Create domain modules
	_monster_manager = MFMonsterManager.new(func(def_id): return _get_monster_def_data(def_id))
	_economy = MFEconomy.new(_player_state, _event_bus)
	_skill_pipeline = MFSkillPipeline.new()

	# Register ToS-specific skill conditions and outcomes
	_register_skill_types()

	# Setup UI router
	_ui_router = preload("res://addons/mobileforge/presentation/router/ui_router.gd").new()
	add_child(_ui_router)
	_ui_router.setup(self, _event_bus)
	_register_screens()

	# Navigate to title screen
	_ui_router.navigate(&"title")


func _get_monster_def_data(def_id: int) -> Dictionary:
	var def = _game_data.get_definition(&"monsters", def_id)
	if def != null:
		return def.raw()
	return {}


func _register_skill_types() -> void:
	# Register conditions
	_skill_pipeline.condition_registry.register("always_true", func(params):
		return _AlwaysTrueCondition.new(params))
	_skill_pipeline.condition_registry.register("combo_above", func(params):
		return _ComboAboveCondition.new(params))
	_skill_pipeline.condition_registry.register("hp_below", func(params):
		return _HpBelowCondition.new(params))

	# Register simple effects
	_skill_pipeline.effect_registry.register("area_damage", func(params, ctx, result):
		var mult: float = float(params.get("multiplier", 1.0))
		for i in range(ctx.enemies.size()):
			var enemy = ctx.enemies[i]
			if enemy.is_alive:
				var dmg := int(ctx.team_stats[0].atk * mult) if not ctx.team_stats.is_empty() else 0
				result.damage_dealt[i] = dmg)

	_skill_pipeline.effect_registry.register("heal_flat", func(params, ctx, result):
		result.healing += int(params.get("amount", 0)))

	_skill_pipeline.effect_registry.register("heal_percent", func(params, ctx, result):
		var pct: float = float(params.get("percent", 0.0))
		result.healing += int(ctx.max_hp * pct))


func _register_screens() -> void:
	_ui_router.register(&"title", func(params): return _create_title_screen())
	_ui_router.register(&"dungeon_select", func(params): return _create_dungeon_select())
	_ui_router.register(&"team_select", func(params): return _create_team_select(params))
	_ui_router.register(&"battle", func(params): return _create_battle_screen(params))
	_ui_router.register(&"result", func(params): return _create_result_screen(params))
	_ui_router.register(&"gacha", func(params): return _create_gacha_screen())
	_ui_router.register(&"monster_box", func(params): return _create_monster_box_screen())
	_ui_router.register(&"shop", func(params): return _create_shop_screen())


func _create_title_screen() -> Node:
	return preload("res://games/tower-of-saviors/godot/game/screens/title_screen.gd").new()


func _create_dungeon_select() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/dungeon_select_screen.gd").new()
	screen.setup(_game_data, _economy, _ui_router)
	return screen


func _create_team_select(params: Dictionary) -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/team_select_screen.gd").new()
	screen.setup(_monster_manager, _player_state, _ui_router, params)
	return screen


func _create_battle_screen(params: Dictionary) -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/battle_screen.gd").new()
	screen.setup(_game_data, _monster_manager, _skill_pipeline, _economy, _event_bus, _ui_router, params)
	return screen


func _create_result_screen(params: Dictionary) -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/result_screen.gd").new()
	screen.setup(params)
	return screen


func _create_gacha_screen() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/gacha_screen.gd").new()
	screen.setup(_game_data, _economy, _monster_manager, _player_state, _event_bus, _ui_router)
	return screen


func _create_monster_box_screen() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/monster_box_screen.gd").new()
	screen.setup(_monster_manager, _player_state, _ui_router)
	return screen


func _create_shop_screen() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/shop_screen.gd").new()
	screen.setup(_economy, _ui_router)
	return screen


# ---- Inline Skill Conditions (simple ones) ----

class _AlwaysTrueCondition extends MFSkillCondition:
	func is_valid(_context: RefCounted) -> bool:
		return true


class _ComboAboveCondition extends MFSkillCondition:
	func is_valid(context: RefCounted) -> bool:
		var threshold := int(get_param("threshold", 1))
		return context.combo_count >= threshold


class _HpBelowCondition extends MFSkillCondition:
	func is_valid(context: RefCounted) -> bool:
		var pct := float(get_param("percent", 0.5))
		if context.max_hp <= 0:
			return false
		return float(context.team_hp) / float(context.max_hp) <= pct
