extends MFTestBase
## Tests for PlayerState (infrastructure/player_state/player_state.gd)
## PlayerState depends on EventBus for change notifications.
## We create both in tests and wire them up manually (no autoload needed).

const EventBusScript = preload("res://addons/mobileforge/infrastructure/event_bus/event_bus.gd")
const PlayerStateScript = preload("res://addons/mobileforge/infrastructure/player_state/player_state.gd")

var _bus: Node
var _ps: Node

# Tracking for event callbacks
var _events_received: Array[Dictionary] = []
var _event_count := 0


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_bus = EventBusScript.new()
	_ps = PlayerStateScript.new()
	# Wire up the EventBus reference that PlayerState normally gets from the scene tree.
	# PlayerState stores it in _event_bus after _ready(), but we set it directly.
	_ps._event_bus = _bus
	_events_received.clear()
	_event_count = 0


func after_each() -> void:
	if _ps != null:
		_ps.free()
		_ps = null
	if _bus != null:
		_bus.free()
		_bus = null


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _on_state_changed(payload: Dictionary) -> void:
	_events_received.append(payload)
	_event_count += 1


func _on_state_loaded(_payload: Dictionary) -> void:
	_event_count += 1


# ---------------------------------------------------------------------------
# Tests: Section Registration
# ---------------------------------------------------------------------------

func test_register_section() -> void:
	var section = _ps.register_section(&"inventory", {"gold": 100})
	assert_not_null(section, "register_section should return a section")
	assert_true(_ps.has_section(&"inventory"), "section should exist after registration")


func test_register_section_returns_existing_on_duplicate() -> void:
	var s1 = _ps.register_section(&"settings", {"volume": 0.8})
	var s2 = _ps.register_section(&"settings", {"volume": 0.5})
	# Should return the same instance, not create a new one
	assert_true(s1 == s2, "duplicate register should return same section instance")
	# Original data should be preserved
	assert_eq(s1.get_value(&"volume"), 0.8, "original data should be preserved")


func test_get_section() -> void:
	_ps.register_section(&"profile", {"name": "Player1"})
	var section = _ps.get_section(&"profile")
	assert_not_null(section, "get_section should return the registered section")
	assert_eq(section.get_value(&"name"), "Player1")


func test_get_section_nonexistent() -> void:
	var section = _ps.get_section(&"does_not_exist")
	assert_null(section, "get_section for nonexistent section should return null")


func test_has_section() -> void:
	assert_false(_ps.has_section(&"combat"), "section should not exist before registration")
	_ps.register_section(&"combat", {})
	assert_true(_ps.has_section(&"combat"), "section should exist after registration")


# ---------------------------------------------------------------------------
# Tests: Value Access
# ---------------------------------------------------------------------------

func test_set_and_get_value() -> void:
	_ps.register_section(&"stats", {"hp": 100, "mp": 50})
	_ps.set_value(&"stats", &"hp", 80)
	assert_eq(_ps.get_value(&"stats", &"hp"), 80, "value should be updated")
	assert_eq(_ps.get_value(&"stats", &"mp"), 50, "unmodified value should remain")


func test_get_value_with_default() -> void:
	_ps.register_section(&"prefs", {})
	var val = _ps.get_value(&"prefs", &"language", "en")
	assert_eq(val, "en", "should return default when key does not exist")


func test_get_value_nonexistent_section() -> void:
	var val = _ps.get_value(&"ghost_section", &"key", "fallback")
	assert_eq(val, "fallback", "should return default for nonexistent section")


func test_set_value_nonexistent_section() -> void:
	# Setting a value on a section that does not exist should log an error
	# but not crash. We just verify no crash occurs.
	_ps.set_value(&"ghost", &"key", "value")
	assert_true(true, "set_value on nonexistent section should not crash")


# ---------------------------------------------------------------------------
# Tests: State Change Events
# ---------------------------------------------------------------------------

func test_state_changed_event_emitted() -> void:
	_bus.subscribe(EventNames.STATE_CHANGED, _on_state_changed)
	_ps.register_section(&"wallet", {"coins": 0})
	_ps.set_value(&"wallet", &"coins", 100)

	assert_eq(_event_count, 1, "STATE_CHANGED event should be emitted once")
	assert_gt(_events_received.size(), 0, "should have received event data")

	var payload: Dictionary = _events_received[0]
	assert_has(payload, "section", "payload should have section key")
	assert_has(payload, "key", "payload should have key key")
	assert_has(payload, "old_value", "payload should have old_value key")
	assert_has(payload, "new_value", "payload should have new_value key")
	assert_eq(payload["section"], &"wallet", "event section name")
	assert_eq(payload["key"], &"coins", "event key")
	assert_eq(payload["new_value"], 100, "new_value should be 100")


func test_state_changed_event_tracks_old_value() -> void:
	_bus.subscribe(EventNames.STATE_CHANGED, _on_state_changed)
	_ps.register_section(&"exp", {"xp": 0})
	_ps.set_value(&"exp", &"xp", 50)
	_ps.set_value(&"exp", &"xp", 120)

	assert_eq(_event_count, 2, "two STATE_CHANGED events expected")
	# First change: 0 -> 50
	var first = _events_received[0]
	assert_eq(first["old_value"], 0, "first change old_value")
	assert_eq(first["new_value"], 50, "first change new_value")
	# Second change: 50 -> 120
	var second = _events_received[1]
	assert_eq(second["old_value"], 50, "second change old_value")
	assert_eq(second["new_value"], 120, "second change new_value")


func test_no_event_without_event_bus() -> void:
	# Simulate missing EventBus
	_ps._event_bus = null
	_ps.register_section(&"solo", {"val": 1})
	_ps.set_value(&"solo", &"val", 2)
	# Should not crash and value should still be updated
	assert_eq(_ps.get_value(&"solo", &"val"), 2, "value should update even without EventBus")


# ---------------------------------------------------------------------------
# Tests: Serialization
# ---------------------------------------------------------------------------

func test_to_save_dict() -> void:
	_ps.register_section(&"progress", {"level": 5, "stage": 3})
	_ps.register_section(&"settings", {"music": true})

	var save := _ps.to_save_dict()
	assert_has(save, "progress", "save dict should have progress section")
	assert_has(save, "settings", "save dict should have settings section")
	assert_eq(save["progress"]["level"], 5)
	assert_eq(save["progress"]["stage"], 3)
	assert_eq(save["settings"]["music"], true)


func test_from_save_dict_updates_existing() -> void:
	_ps.register_section(&"progress", {"level": 1, "stage": 1})
	var saved := {"progress": {"level": 10, "stage": 7}}
	_ps.from_save_dict(saved)

	assert_eq(_ps.get_value(&"progress", &"level"), 10, "level should be updated from save")
	assert_eq(_ps.get_value(&"progress", &"stage"), 7, "stage should be updated from save")


func test_from_save_dict_creates_new_sections() -> void:
	var saved := {"new_section": {"foo": "bar"}}
	_ps.from_save_dict(saved)

	assert_true(_ps.has_section(&"new_section"), "new section should be created from save data")
	assert_eq(_ps.get_value(&"new_section", &"foo"), "bar")


func test_from_save_dict_emits_state_loaded() -> void:
	_bus.subscribe(EventNames.STATE_LOADED, _on_state_loaded)
	var saved := {"data": {"x": 1}}
	_ps.from_save_dict(saved)
	assert_eq(_event_count, 1, "STATE_LOADED event should be emitted")


func test_save_and_restore_roundtrip() -> void:
	_ps.register_section(&"hero", {"name": "Aria", "level": 25, "hp": 999})
	_ps.register_section(&"flags", {"tutorial_done": true, "first_clear": false})
	_ps.set_value(&"hero", &"level", 26)

	var saved := _ps.to_save_dict()

	# Create a fresh PlayerState and restore
	var ps2 := PlayerStateScript.new()
	ps2._event_bus = _bus
	ps2.from_save_dict(saved)

	assert_eq(ps2.get_value(&"hero", &"name"), "Aria", "name restored")
	assert_eq(ps2.get_value(&"hero", &"level"), 26, "updated level restored")
	assert_eq(ps2.get_value(&"hero", &"hp"), 999, "hp restored")
	assert_eq(ps2.get_value(&"flags", &"tutorial_done"), true, "flag restored")
	assert_eq(ps2.get_value(&"flags", &"first_clear"), false, "flag restored")

	ps2.free()


func test_to_save_dict_is_deep_copy() -> void:
	_ps.register_section(&"nested", {"items": [1, 2, 3]})
	var saved := _ps.to_save_dict()
	# Mutating the save dict should not affect the original state
	saved["nested"]["items"].append(4)
	var original_items = _ps.get_value(&"nested", &"items")
	assert_eq(original_items.size(), 3, "original state should not be affected by save dict mutation")


# ---------------------------------------------------------------------------
# Tests: Clear
# ---------------------------------------------------------------------------

func test_clear_all() -> void:
	_ps.register_section(&"a", {"x": 1})
	_ps.register_section(&"b", {"y": 2})
	_ps.clear_all()
	assert_false(_ps.has_section(&"a"), "section a should not exist after clear")
	assert_false(_ps.has_section(&"b"), "section b should not exist after clear")


# ---------------------------------------------------------------------------
# Tests: StateSection Direct
# ---------------------------------------------------------------------------

func test_section_set_value_returns_old() -> void:
	var section = _ps.register_section(&"test_ret", {"score": 100})
	var old = section.set_value(&"score", 200)
	assert_eq(old, 100, "set_value should return the old value")
	assert_eq(section.get_value(&"score"), 200, "new value should be stored")


func test_section_has_key() -> void:
	var section = _ps.register_section(&"keys", {"present": true})
	assert_true(section.has_key(&"present"), "has_key for existing key")
	assert_false(section.has_key(&"absent"), "has_key for missing key")


func test_section_erase() -> void:
	var section = _ps.register_section(&"erasable", {"temp": 42})
	assert_true(section.has_key(&"temp"))
	var erased := section.erase(&"temp")
	assert_true(erased, "erase should return true for existing key")
	assert_false(section.has_key(&"temp"), "key should be gone after erase")


func test_section_keys() -> void:
	var section = _ps.register_section(&"multi_key", {"a": 1, "b": 2, "c": 3})
	var k = section.keys()
	assert_eq(k.size(), 3, "should have 3 keys")


func test_section_clear() -> void:
	var section = _ps.register_section(&"clearme", {"x": 1, "y": 2})
	section.clear()
	assert_eq(section.keys().size(), 0, "section should be empty after clear")
	assert_null(section.get_value(&"x"), "cleared key should return null (default)")


func test_section_to_dict_and_from_dict() -> void:
	var section = _ps.register_section(&"serde", {"a": 10, "b": "hello"})
	var d := section.to_dict()
	assert_has(d, "a")
	assert_eq(d["a"], 10)

	section.from_dict({"a": 99, "c": "new"})
	assert_eq(section.get_value(&"a"), 99, "from_dict should overwrite")
	assert_eq(section.get_value(&"c"), "new", "from_dict should add new keys")
	assert_false(section.has_key(&"b"), "from_dict should replace entire data, removing old keys")


func test_section_initial_data_is_deep_copied() -> void:
	var init_data := {"list": [1, 2, 3]}
	var section = _ps.register_section(&"deep_copy", init_data)
	# Mutating the original dict should not affect the section
	init_data["list"].append(4)
	var section_list = section.get_value(&"list")
	assert_eq(section_list.size(), 3, "section should not be affected by external mutation of init data")
