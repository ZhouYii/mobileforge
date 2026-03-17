extends Node
## Singleton autoload. Loads and provides read-only access to JSON game definitions.
##
## API:
##   load_definitions(type: StringName, path: String) -> Error
##   get_definition(type: StringName, id: int) -> MFDataTypes.Definition
##   get_all_definitions(type: StringName) -> Array[MFDataTypes.Definition]
##   has_definition(type: StringName, id: int) -> bool
##   get_definition_count(type: StringName) -> int
##   clear_type(type: StringName) -> void
##   clear_all() -> void
##
## Internal: Dictionary[StringName, Dictionary[int, MFDataTypes.Definition]]
## load_definitions reads a JSON file, expects Array of objects, each with "id" field.
## Creates Definition wrappers, stores in lookup by id.

## type_name -> { id -> Definition }
var _tables: Dictionary = {}
var _schema_validator: MFSchemaValidator = null


## Set a schema validator instance. When set, definitions are validated on load.
func set_schema_validator(validator: MFSchemaValidator) -> void:
	_schema_validator = validator


## Get the current schema validator (may be null).
func get_schema_validator() -> MFSchemaValidator:
	return _schema_validator


func load_definitions(type: StringName, path: String) -> Error:
	if not FileAccess.file_exists(path):
		push_error("GameData: file not found: %s" % path)
		return ERR_FILE_NOT_FOUND

	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		push_error("GameData: could not open file: %s" % path)
		return ERR_FILE_NOT_FOUND

	var text := file.get_as_text()
	file.close()

	var parsed = JSON.parse_string(text)
	if parsed == null:
		push_error("GameData: JSON parse error in file: %s" % path)
		return ERR_PARSE_ERROR

	if not parsed is Array:
		push_error("GameData: expected Array at root of file: %s" % path)
		return ERR_PARSE_ERROR

	var lookup: Dictionary = {}
	for entry in parsed:
		if not entry is Dictionary:
			continue
		var def := MFDataTypes.Definition.new(entry)
		lookup[def.id] = def

	_tables[type] = lookup

	# Validate against schema if one is registered
	if _schema_validator != null and _schema_validator.has_schema(type):
		var raw_entries: Array = []
		for entry in parsed:
			if entry is Dictionary:
				raw_entries.append(entry)
		var validation = _schema_validator.validate_definitions(type, raw_entries)
		if not validation.is_valid:
			for error in validation.errors:
				push_warning("GameData schema validation [%s]: %s" % [type, error])

	return OK


func get_definition(type: StringName, id: int) -> MFDataTypes.Definition:
	if not _tables.has(type):
		return null
	var lookup: Dictionary = _tables[type]
	if not lookup.has(id):
		return null
	return lookup[id] as MFDataTypes.Definition


func get_all_definitions(type: StringName) -> Array:
	if not _tables.has(type):
		return []
	var lookup: Dictionary = _tables[type]
	return lookup.values()


func has_definition(type: StringName, id: int) -> bool:
	if not _tables.has(type):
		return false
	return _tables[type].has(id)


func get_definition_count(type: StringName) -> int:
	if not _tables.has(type):
		return 0
	return _tables[type].size()


func clear_type(type: StringName) -> void:
	_tables.erase(type)


func clear_all() -> void:
	_tables.clear()
