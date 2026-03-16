extends MFTestBase
## Tests for SaveManager (infrastructure/save_manager/save_manager.gd)
## Tests SaveFormat envelope creation/validation, SaveMigrator chain migration,
## ISaveable interface round-trips, and SaveManager initial state.

const SaveFormatScript = preload("res://addons/mobileforge/infrastructure/save_manager/save_format.gd")
const SaveMigratorScript = preload("res://addons/mobileforge/infrastructure/save_manager/save_migrator.gd")
const SaveManagerScript = preload("res://addons/mobileforge/infrastructure/save_manager/save_manager.gd")

var _format: RefCounted
var _migrator: RefCounted
var _manager: Node


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_format = SaveFormatScript.new()
	_migrator = SaveMigratorScript.new()
	_manager = SaveManagerScript.new()


func after_each() -> void:
	if _manager != null:
		_manager.free()
		_manager = null


# ---------------------------------------------------------------------------
# Tests: SaveFormat
# ---------------------------------------------------------------------------

func test_save_format_create_envelope() -> void:
	var data := {"progress": {"level": 5}, "inventory": {"gold": 100}}
	var envelope: Dictionary = _format.create_envelope(data, 1)

	assert_has(envelope, "version", "envelope should have version field")
	assert_has(envelope, "timestamp", "envelope should have timestamp field")
	assert_has(envelope, "checksum", "envelope should have checksum field")
	assert_has(envelope, "data", "envelope should have data field")
	assert_eq(envelope["version"], 1, "version should be 1")
	assert_true(envelope["timestamp"] > 0, "timestamp should be positive")
	assert_true(envelope["checksum"] is String, "checksum should be a string")
	assert_eq(envelope["data"]["progress"]["level"], 5, "data should be preserved in envelope")


func test_save_format_validate() -> void:
	var data := {"settings": {"volume": 0.8}}
	var envelope: Dictionary = _format.create_envelope(data, 1)

	var is_valid: bool = _format.validate(envelope)
	assert_true(is_valid, "freshly created envelope should validate successfully")


func test_save_format_tampered() -> void:
	var data := {"settings": {"volume": 0.8}}
	var envelope: Dictionary = _format.create_envelope(data, 1)

	# Tamper with the data after envelope creation
	envelope["data"]["settings"]["volume"] = 0.0

	var is_valid: bool = _format.validate(envelope)
	assert_false(is_valid, "tampered envelope should fail validation")


# ---------------------------------------------------------------------------
# Tests: SaveMigrator
# ---------------------------------------------------------------------------

func test_save_migrator_chain() -> void:
	# Register v1->v2 and v2->v3 migration steps
	_migrator.register(1, 2, func(data: Dictionary) -> Dictionary:
		# Migration: rename "coins" to "gold"
		if data.has("coins"):
			data["gold"] = data["coins"]
			data.erase("coins")
		return data
	)
	_migrator.register(2, 3, func(data: Dictionary) -> Dictionary:
		# Migration: add "gems" field defaulting to 0
		data["gems"] = data.get("gems", 0)
		return data
	)

	var old_data := {"coins": 500}
	var migrated: Dictionary = _migrator.migrate(old_data, 1, 3)

	assert_false(migrated.has("coins"), "coins should be removed after v1->v2 migration")
	assert_has(migrated, "gold", "gold should exist after v1->v2 migration")
	assert_eq(migrated["gold"], 500, "gold value should be preserved from coins")
	assert_has(migrated, "gems", "gems should exist after v2->v3 migration")
	assert_eq(migrated["gems"], 0, "gems should default to 0")


# ---------------------------------------------------------------------------
# Tests: ISaveable interface round-trip
# ---------------------------------------------------------------------------

func test_saveable_interface() -> void:
	# Create a mock object that implements save_to_dict / load_from_dict
	var saveable := MockSaveable.new()
	saveable.name = "TestHero"
	saveable.level = 10

	# Save
	var saved: Dictionary = saveable.save_to_dict()
	assert_has(saved, "name")
	assert_has(saved, "level")
	assert_eq(saved["name"], "TestHero")
	assert_eq(saved["level"], 10)

	# Restore into a new instance
	var restored := MockSaveable.new()
	restored.load_from_dict(saved)
	assert_eq(restored.name, "TestHero", "name should be restored")
	assert_eq(restored.level, 10, "level should be restored")


# ---------------------------------------------------------------------------
# Tests: SaveManager
# ---------------------------------------------------------------------------

func test_save_manager_has_save() -> void:
	assert_false(_manager.has_save(), "initially there should be no save")


# ---------------------------------------------------------------------------
# Mock saveable for round-trip test
# ---------------------------------------------------------------------------

class MockSaveable extends RefCounted:
	var name: String = ""
	var level: int = 0

	func save_to_dict() -> Dictionary:
		return {"name": name, "level": level}

	func load_from_dict(data: Dictionary) -> void:
		name = data.get("name", "")
		level = data.get("level", 0)
