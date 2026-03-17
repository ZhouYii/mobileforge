extends Control
## Main entry point for Tower of Saviors. Wires framework modules.

var _game_data: Node  # Framework GameData autoload
var _player_state: Node  # Framework PlayerState autoload
var _event_bus: Node  # Framework EventBus autoload
var _economy: MFEconomy
var _monster_manager: MFMonsterManager
var _skill_pipeline: MFSkillPipeline
var _ui_router: Node  # Framework UIRouter
var _save_manager: Node  # Framework SaveManager autoload
var _stamina_timer: MFStaminaTimer
var _stamina_config: RefCounted  # MFEconomyTypes.StaminaConfig
var _login_bonus: MFLoginBonus
var _helper_provider: MFHelperProvider


func _ready() -> void:
	_event_bus = get_node("/root/EventBus")
	_game_data = get_node("/root/GameData")
	_player_state = get_node("/root/PlayerState")
	_save_manager = get_node_or_null("/root/SaveManager")

	# Register schemas for data validation
	_register_schemas()

	# Load game data (all 9 JSON files)
	_game_data.load_definitions(&"monsters", "res://games/tower-of-saviors/shared/data/monsters.json")
	_game_data.load_definitions(&"skills", "res://games/tower-of-saviors/shared/data/skills.json")
	_game_data.load_definitions(&"leader_skills", "res://games/tower-of-saviors/shared/data/leader_skills.json")
	_game_data.load_definitions(&"stages", "res://games/tower-of-saviors/shared/data/stages.json")
	_game_data.load_definitions(&"gacha_pools", "res://games/tower-of-saviors/shared/data/gacha_pools.json")
	_game_data.load_definitions(&"team_skills", "res://games/tower-of-saviors/shared/data/team_skills.json")
	# gem_modifiers and element_chart are Dictionary-rooted, not Array-rooted.
	# They're consumed directly by MFElementChart (hardcoded defaults) and MFBoardTypes.
	# _game_data.load_definitions expects Array roots, so skip these.
	_game_data.load_definitions(&"loot_tables", "res://games/tower-of-saviors/shared/data/loot_tables.json")
	_game_data.load_definitions(&"event_shops", "res://games/tower-of-saviors/shared/data/event_shops.json")
	_game_data.load_definitions(&"monster_exchange", "res://games/tower-of-saviors/shared/data/monster_exchange.json")

	# Initialize player state sections
	_player_state.register_section(&"currencies", {"gems": 50, "coins": 10000, "stamina": 100, "event_tokens": 0})
	_player_state.register_section(&"monsters", {})
	_player_state.register_section(&"teams", {})
	_player_state.register_section(&"progress", {"rank": 1, "exp": 0})

	# Create domain modules
	_monster_manager = MFMonsterManager.new(func(def_id): return _get_monster_def_data(def_id))
	_economy = MFEconomy.new(_player_state, _event_bus)

	# Grant starter monsters for new players (one of each element)
	var monsters_section = _player_state.get_section(&"monsters")
	if monsters_section != null and monsters_section.keys().is_empty():
		var starters := [16, 17, 18, 19, 20]  # 3★ monsters: one per element
		for sid in starters:
			var inst = _monster_manager.create_instance(sid, 5)  # Start at level 5
			monsters_section.set_value(StringName(str(inst.instance_id)), inst.to_dict())
	_skill_pipeline = MFSkillPipeline.new()

	# Setup stamina timer: 1 stamina per 300s (5 min), max 100
	_stamina_config = MFEconomyTypes.StaminaConfig.new(100, 300.0)
	_stamina_timer = MFStaminaTimer.new(_stamina_config)
	_stamina_timer.set_last_update(Time.get_unix_time_from_system())
	_economy.setup_stamina(_stamina_config)

	# Register ToS-specific skill conditions and outcomes
	TosSkillRegistration.register(_skill_pipeline)

	# Create helper provider (mock friend list from game data)
	var _all_monsters_lookup := func() -> Array:
		return _game_data.get_all_definitions(&"monsters")
	_helper_provider = MFHelperProvider.new(_monster_manager, _all_monsters_lookup, 5)

	# Wire save system
	_setup_save_system()

	# Setup daily login bonus (7-day rotating cycle)
	_login_bonus = MFLoginBonus.new(_player_state)
	_login_bonus.set_schedule([
		{"day": 1, "type": "currency", "currency": "coins", "count": 5000},
		{"day": 2, "type": "currency", "currency": "gems", "count": 1},
		{"day": 3, "type": "currency", "currency": "coins", "count": 10000},
		{"day": 4, "type": "currency", "currency": "gems", "count": 2},
		{"day": 5, "type": "currency", "currency": "coins", "count": 15000},
		{"day": 6, "type": "currency", "currency": "gems", "count": 3},
		{"day": 7, "type": "currency", "currency": "gems", "count": 5},
	])

	# Setup UI router (screens are added as children of this Control)
	_ui_router = preload("res://addons/mobileforge/presentation/router/ui_router.gd").new()
	add_child(_ui_router)
	_ui_router.setup(self, _event_bus)
	_register_screens()

	# Navigate to title screen
	_ui_router.navigate(&"title")


func _register_schemas() -> void:
	var sv = MFSchemaValidator.new()

	# Monster schema
	sv.register_schema(&"monsters", [
		MFSchemaValidator.field("id", "int", true),
		MFSchemaValidator.field("name", "string", true),
		MFSchemaValidator.field("element", "int", true),
		MFSchemaValidator.field("rarity", "int", true),
		MFSchemaValidator.field("max_level", "int", true),
		MFSchemaValidator.field("base_hp", "float", true),
		MFSchemaValidator.field("base_atk", "float", true),
		MFSchemaValidator.field("base_rec", "float", true),
		MFSchemaValidator.field("max_hp", "float", true),
		MFSchemaValidator.field("max_atk", "float", true),
		MFSchemaValidator.field("max_rec", "float", true),
	])

	# Skills schema
	sv.register_schema(&"skills", [
		MFSchemaValidator.field("id", "int", true),
		MFSchemaValidator.field("name", "string", true),
		MFSchemaValidator.field("type", "string", true),
		MFSchemaValidator.field("rules", "array", true),
	])

	# Stages schema
	sv.register_schema(&"stages", [
		MFSchemaValidator.field("id", "int", true),
		MFSchemaValidator.field("name", "string", true),
		MFSchemaValidator.field("stamina_cost", "int", true),
		MFSchemaValidator.field("waves", "array", true),
	])

	# Gacha pools schema
	sv.register_schema(&"gacha_pools", [
		MFSchemaValidator.field("id", "int", true),
		MFSchemaValidator.field("name", "string", true),
		MFSchemaValidator.field("entries", "array", true),
	])

	_game_data.set_schema_validator(sv)


func _get_monster_def_data(def_id: int) -> Dictionary:
	var def = _game_data.get_definition(&"monsters", def_id)
	if def != null:
		return def.raw()
	return {}


func _setup_save_system() -> void:
	if _save_manager == null:
		return

	# Register player state as a saveable
	if _player_state.has_method("get_saveable"):
		_save_manager.register_saveable(&"player_state", _player_state.get_saveable())

	# Try loading existing save
	_save_manager.load_save(0)

	# Listen for state changes to mark dirty
	if _event_bus.has_method("subscribe"):
		_event_bus.subscribe(EventNames.CURRENCY_CHANGED, func(_payload): _save_manager.mark_dirty())
		_event_bus.subscribe(EventNames.MONSTER_ADDED, func(_payload): _save_manager.mark_dirty())
		_event_bus.subscribe(EventNames.BATTLE_WON, func(_payload): _save_manager.mark_dirty())


func _process(delta: float) -> void:
	# Update stamina refill
	var current_stamina := _economy.get_balance("stamina")
	if current_stamina < _stamina_config.max_stamina:
		var now := Time.get_unix_time_from_system()
		var refill := _stamina_timer.calculate_refill(current_stamina, now)
		var new_stamina: int = refill["stamina"]
		if new_stamina > current_stamina:
			_economy.earn("stamina", new_stamina - current_stamina)


func _notification(what: int) -> void:
	# Save on quit
	if what == NOTIFICATION_WM_CLOSE_REQUEST:
		if _save_manager != null:
			_save_manager.save(0)


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
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/title_screen.gd").new()
	screen.setup(_login_bonus, _economy)
	return screen


func _create_dungeon_select() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/dungeon_select_screen.gd").new()
	screen.setup(_game_data, _economy, _ui_router)
	return screen


func _create_team_select(params: Dictionary) -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/team_select_screen.gd").new()
	_helper_provider.refresh()
	screen.setup(_monster_manager, _player_state, _ui_router, params, _helper_provider)
	return screen


func _create_battle_screen(params: Dictionary) -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/battle_screen.gd").new()
	screen.setup(_game_data, _monster_manager, _skill_pipeline, _economy, _event_bus, _ui_router, params, _player_state)
	return screen


func _create_result_screen(params: Dictionary) -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/result_screen.gd").new()
	screen.setup(params, _economy, _monster_manager, _player_state, _game_data, _ui_router)
	return screen


func _create_gacha_screen() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/gacha_screen.gd").new()
	screen.setup(_game_data, _economy, _monster_manager, _player_state, _event_bus, _ui_router)
	return screen


func _create_monster_box_screen() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/monster_box_screen.gd").new()
	screen.setup(_monster_manager, _player_state, _ui_router, _economy)
	return screen


func _create_shop_screen() -> Node:
	var screen = preload("res://games/tower-of-saviors/godot/game/screens/shop_screen.gd").new()
	screen.setup(_economy, _ui_router, _player_state, _monster_manager, _game_data)
	return screen
