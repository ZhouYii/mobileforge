extends MFTestBase
## Tests for ToS save/load functionality

const SaveManagerScript = preload("res://addons/mobileforge/infrastructure/save_manager/save_manager.gd")
const PlayerStateScript = preload("res://addons/mobileforge/infrastructure/player_state/player_state.gd")
const EventBusScript = preload("res://addons/mobileforge/infrastructure/event_bus/event_bus.gd")


var _temp_dir: String
var _save_manager: RefCounted
var _player_state: Node
var _event_bus: Node


func before_all() -> void:
	_temp_dir = "user://test_saves/"
	DirAccess.make_dir_recursive_absolute(_temp_dir)


func before_each() -> void:
	_event_bus = EventBusScript.new()
	_player_state = PlayerStateScript.new()
	_player_state._event_bus = _event_bus
	_player_state.register_section(&"currencies", {"gems": 100, "coins": 5000, "stamina": 80})
	_player_state.register_section(&"monsters", {})
	_player_state.register_section(&"progress", {"cleared_dungeons": [], "pity_counts": {}})
	
	_save_manager = SaveManagerScript.new(_temp_dir)


func after_each() -> void:
	if _player_state != null:
		_player_state.free()
		_player_state = null
	if _event_bus != null:
		_event_bus.free()
		_event_bus = null
	_save_manager = null


func after_all() -> void:
	OS.move_to_trash(ProjectSettings.globalize_path(_temp_dir))


func test_save_creates_file() -> void:
	_save_manager.save(0, _player_state.serialize())
	
	var path = _temp_dir + "save_0.json"
	assert_true(FileAccess.file_exists(path), "save file should be created")


func test_load_restores_state() -> void:
	_player_state.set_section_key(&"currencies", &"gems", 200)
	_player_state.set_section_key(&"currencies", &"coins", 10000)
	_player_state.set_section_key(&"progress", &"cleared_dungeons", [1, 2, 3])
	
	_save_manager.save(0, _player_state.serialize())
	
	var new_ps = PlayerStateScript.new()
	new_ps._event_bus = _event_bus
	new_ps.register_section(&"currencies", {"gems": 0, "coins": 0, "stamina": 0})
	new_ps.register_section(&"monsters", {})
	new_ps.register_section(&"progress", {"cleared_dungeons": [], "pity_counts": {}})
	
	var data = _save_manager.load(0)
	new_ps.deserialize(data)
	
	assert_eq(new_ps.get_section_key(&"currencies", &"gems"), 200, "gems should be restored")
	assert_eq(new_ps.get_section_key(&"currencies", &"coins"), 10000, "coins should be restored")
	var cleared: Array = new_ps.get_section_key(&"progress", &"cleared_dungeons")
	assert_eq(cleared.size(), 3, "progress should be restored")
	
	new_ps.free()


func test_dirty_flag_set_on_currency_change() -> void:
	_save_manager.track_state(_player_state)
	
	assert_false(_save_manager.is_dirty(), "should start clean")
	
	_player_state.set_section_key(&"currencies", &"gems", 150)
	
	assert_true(_save_manager.is_dirty(), "currency change should mark dirty")


func test_dirty_flag_cleared_after_save() -> void:
	_save_manager.track_state(_player_state)
	
	_player_state.set_section_key(&"currencies", &"gems", 150)
	assert_true(_save_manager.is_dirty(), "should be dirty after change")
	
	_save_manager.save(0, _player_state.serialize())
	
	assert_false(_save_manager.is_dirty(), "should be clean after save")


func test_stamina_refills_over_time() -> void:
	var stamina_config = {"max_stamina": 100, "refill_rate_seconds": 300.0}
	var timer = _make_stamina_timer(stamina_config)
	timer.set_last_update(0.0)
	
	var result = timer.calculate_refill(50, 300.0)
	assert_eq(result.stamina, 51, "should gain 1 stamina after 300 seconds")
	
	result = timer.calculate_refill(51, 900.0)
	assert_eq(result.stamina, 54, "should gain 3 stamina after 900 seconds total")


func test_stamina_caps_at_max() -> void:
	var stamina_config = {"max_stamina": 100, "refill_rate_seconds": 300.0}
	var timer = _make_stamina_timer(stamina_config)
	timer.set_last_update(0.0)
	
	var result = timer.calculate_refill(99, 500.0)
	assert_eq(result.stamina, 100, "should cap at max_stamina")


func test_save_multiple_slots() -> void:
	_player_state.set_section_key(&"currencies", &"gems", 100)
	_save_manager.save(0, _player_state.serialize())
	
	_player_state.set_section_key(&"currencies", &"gems", 200)
	_save_manager.save(1, _player_state.serialize())
	
	_player_state.set_section_key(&"currencies", &"gems", 300)
	_save_manager.save(2, _player_state.serialize())
	
	var data0 = _save_manager.load(0)
	var data1 = _save_manager.load(1)
	var data2 = _save_manager.load(2)
	
	assert_eq(data0.currencies.gems, 100, "slot 0 should have 100 gems")
	assert_eq(data1.currencies.gems, 200, "slot 1 should have 200 gems")
	assert_eq(data2.currencies.gems, 300, "slot 2 should have 300 gems")


func test_load_missing_slot_returns_null() -> void:
	var data = _save_manager.load(999)
	assert_null(data, "missing slot should return null")


func _make_stamina_timer(config: Dictionary) -> RefCounted:
	var timer = RefCounted.new()
	timer.set("config", config)
	timer.set("_last_update", 0.0)
	
	timer.set("set_last_update", func(time: float) -> void:
		timer._last_update = time
	)
	
	timer.set("calculate_refill", func(current_stamina: int, current_time: float) -> Dictionary:
		if timer._last_update <= 0.0:
			timer._last_update = current_time
			return {"stamina": current_stamina, "remainder_seconds": 0.0}
		
		var elapsed: float = current_time - timer._last_update
		var points_gained: int = int(elapsed / timer.config.refill_rate_seconds)
		var remainder: float = fmod(elapsed, timer.config.refill_rate_seconds)
		
		var new_stamina: int = mini(current_stamina + points_gained, timer.config.max_stamina)
		timer._last_update = current_time - remainder
		
		return {"stamina": new_stamina, "remainder_seconds": remainder}
	)
	
	return timer
