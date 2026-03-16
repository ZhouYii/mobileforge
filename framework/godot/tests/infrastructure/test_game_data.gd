extends MFTestBase
## Tests for GameData (infrastructure/game_data/game_data.gd)
## Uses temporary JSON files written to user://mf_test_data/ for file-based tests.

const GameDataScript = preload("res://addons/mobileforge/infrastructure/game_data/game_data.gd")

var _gd: Node
var _test_dir := "user://mf_test_data"


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_all() -> void:
	DirAccess.make_dir_recursive_absolute(_test_dir)


func before_each() -> void:
	_gd = GameDataScript.new()


func after_each() -> void:
	if _gd != null:
		_gd.free()
		_gd = null


func after_all() -> void:
	# Clean up temp files
	_remove_test_files()


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _write_json(filename: String, content: String) -> String:
	var path := _test_dir.path_join(filename)
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file != null:
		file.store_string(content)
		file.close()
	return path


func _remove_test_files() -> void:
	var dir := DirAccess.open(_test_dir)
	if dir == null:
		return
	dir.list_dir_begin()
	var fname := dir.get_next()
	while fname != "":
		if not dir.current_is_dir():
			dir.remove(fname)
		fname = dir.get_next()
	dir.list_dir_end()
	DirAccess.remove_absolute(_test_dir)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_load_valid_json() -> void:
	var json_text := '[{"id":1,"name":"Sword","atk":10},{"id":2,"name":"Shield","def":5}]'
	var path := _write_json("items.json", json_text)
	var err := _gd.load_definitions(&"items", path)
	assert_eq(err, OK, "load should return OK")
	assert_eq(_gd.get_definition_count(&"items"), 2, "should have 2 definitions")


func test_get_definition_by_id() -> void:
	var json_text := '[{"id":10,"name":"Fireball","damage":100}]'
	var path := _write_json("skills.json", json_text)
	_gd.load_definitions(&"skills", path)

	var def = _gd.get_definition(&"skills", 10)
	assert_not_null(def, "definition with id 10 should exist")
	assert_eq(def.id, 10, "definition id")
	assert_eq(def.get_string(&"name"), "Fireball", "definition name field")
	assert_eq(def.get_int(&"damage"), 100, "definition damage field")


func test_has_definition() -> void:
	var json_text := '[{"id":1,"name":"A"},{"id":2,"name":"B"}]'
	var path := _write_json("has_test.json", json_text)
	_gd.load_definitions(&"has_test", path)

	assert_true(_gd.has_definition(&"has_test", 1), "has id 1")
	assert_true(_gd.has_definition(&"has_test", 2), "has id 2")
	assert_false(_gd.has_definition(&"has_test", 999), "does not have id 999")


func test_get_all_definitions() -> void:
	var json_text := '[{"id":1},{"id":2},{"id":3}]'
	var path := _write_json("all_defs.json", json_text)
	_gd.load_definitions(&"all", path)

	var defs = _gd.get_all_definitions(&"all")
	assert_eq(defs.size(), 3, "should return all 3 definitions")

	# Verify all ids present
	var ids: Array[int] = []
	for d in defs:
		ids.append(d.id)
	ids.sort()
	assert_eq(ids[0], 1)
	assert_eq(ids[1], 2)
	assert_eq(ids[2], 3)


func test_load_nonexistent_file() -> void:
	var err := _gd.load_definitions(&"missing", "user://does_not_exist_12345.json")
	assert_eq(err, ERR_FILE_NOT_FOUND, "should return ERR_FILE_NOT_FOUND")
	assert_eq(_gd.get_definition_count(&"missing"), 0, "no definitions loaded")


func test_load_malformed_json() -> void:
	var path := _write_json("bad.json", "this is not json {{{")
	var err := _gd.load_definitions(&"bad", path)
	assert_eq(err, ERR_PARSE_ERROR, "should return ERR_PARSE_ERROR for malformed JSON")


func test_load_non_array_root() -> void:
	var path := _write_json("object_root.json", '{"id":1,"name":"not an array"}')
	var err := _gd.load_definitions(&"obj_root", path)
	assert_eq(err, ERR_PARSE_ERROR, "should return ERR_PARSE_ERROR when root is not Array")


func test_clear_type() -> void:
	var json_text := '[{"id":1}]'
	var path := _write_json("clearable.json", json_text)
	_gd.load_definitions(&"clearable", path)
	assert_eq(_gd.get_definition_count(&"clearable"), 1, "1 definition before clear")

	_gd.clear_type(&"clearable")
	assert_eq(_gd.get_definition_count(&"clearable"), 0, "0 definitions after clear_type")
	assert_false(_gd.has_definition(&"clearable", 1), "definition no longer exists")


func test_clear_all() -> void:
	var path_a := _write_json("ca_a.json", '[{"id":1}]')
	var path_b := _write_json("ca_b.json", '[{"id":2}]')
	_gd.load_definitions(&"type_a", path_a)
	_gd.load_definitions(&"type_b", path_b)
	assert_eq(_gd.get_definition_count(&"type_a"), 1)
	assert_eq(_gd.get_definition_count(&"type_b"), 1)

	_gd.clear_all()
	assert_eq(_gd.get_definition_count(&"type_a"), 0, "type_a cleared")
	assert_eq(_gd.get_definition_count(&"type_b"), 0, "type_b cleared")


func test_get_definition_unknown_type() -> void:
	var def = _gd.get_definition(&"nonexistent_type", 1)
	assert_null(def, "should return null for unknown type")


func test_get_definition_unknown_id() -> void:
	var path := _write_json("known_type.json", '[{"id":1}]')
	_gd.load_definitions(&"known", path)
	var def = _gd.get_definition(&"known", 999)
	assert_null(def, "should return null for unknown id in valid type")


func test_get_all_definitions_unknown_type() -> void:
	var defs = _gd.get_all_definitions(&"ghost_type")
	assert_eq(defs.size(), 0, "should return empty array for unknown type")


func test_definition_field_accessors() -> void:
	var json_text := '[{"id":5,"name":"Potion","price":50,"weight":0.5,"tags":["consumable","healing"],"meta":{"rarity":"common"}}]'
	var path := _write_json("accessors.json", json_text)
	_gd.load_definitions(&"acc", path)

	var def = _gd.get_definition(&"acc", 5)
	assert_not_null(def)
	assert_eq(def.get_string(&"name"), "Potion")
	assert_eq(def.get_int(&"price"), 50)
	assert_eq(def.get_float(&"weight"), 0.5)
	assert_eq(def.get_array(&"tags").size(), 2)
	assert_true(def.has_field(&"meta"))
	assert_false(def.has_field(&"nonexistent"))

	var meta := def.get_dict(&"meta")
	assert_has(meta, "rarity")
	assert_eq(meta.get("rarity"), "common")

	# Defaults
	assert_eq(def.get_int(&"missing_int", -1), -1, "default int")
	assert_eq(def.get_string(&"missing_str", "fallback"), "fallback", "default string")
	assert_eq(def.get_float(&"missing_float", 3.14), 3.14, "default float")
	assert_eq(def.get_array(&"missing_arr", [1]).size(), 1, "default array")

	# raw()
	var raw := def.raw()
	assert_has(raw, "id")
	assert_has(raw, "name")


func test_entries_without_id_default_to_zero() -> void:
	var json_text := '[{"name":"no_id_entry"}]'
	var path := _write_json("no_id.json", json_text)
	_gd.load_definitions(&"noid", path)

	var def = _gd.get_definition(&"noid", 0)
	assert_not_null(def, "entry without id field should default to id 0")
	assert_eq(def.get_string(&"name"), "no_id_entry")


func test_non_dict_entries_skipped() -> void:
	# Array contains a mix of dicts and non-dicts; non-dicts should be skipped.
	var json_text := '[{"id":1,"name":"valid"}, 42, "string_entry", {"id":2,"name":"also_valid"}]'
	var path := _write_json("mixed.json", json_text)
	var err := _gd.load_definitions(&"mixed", path)
	assert_eq(err, OK, "load should succeed, skipping non-dict entries")
	assert_eq(_gd.get_definition_count(&"mixed"), 2, "only dict entries loaded")
