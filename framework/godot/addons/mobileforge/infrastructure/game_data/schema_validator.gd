class_name MFSchemaValidator extends RefCounted
## Validates loaded JSON data against schema rules.
## Called optionally by GameData after loading definitions.
##
## Supports: required fields, type checks, enum ranges, min/max values.
## Does NOT enforce JSON Schema spec — this is a lightweight game-specific validator.

## Validation result with errors list.
class ValidationResult extends RefCounted:
	var is_valid: bool = true
	var errors: Array[String] = []

	func add_error(path: String, message: String) -> void:
		is_valid = false
		errors.append("%s: %s" % [path, message])


## Schema rule definition.
## Each rule specifies a field name, expected type, and optional constraints.
class FieldRule extends RefCounted:
	var field_name: String = ""
	var required: bool = false
	var expected_type: String = ""  # "int", "float", "string", "bool", "array", "dict"
	var min_value: float = -INF
	var max_value: float = INF
	var enum_values: Array = []  # If non-empty, value must be one of these
	var min_length: int = 0  # For strings and arrays

	func _init(name: String, type: String = "", req: bool = false) -> void:
		field_name = name
		expected_type = type
		required = req


var _schemas: Dictionary = {}  # type_name -> Array[FieldRule]


## Register a schema for a definition type.
func register_schema(type_name: StringName, rules: Array) -> void:
	_schemas[type_name] = rules


## Validate a single definition dictionary against a registered schema.
func validate_definition(type_name: StringName, data: Dictionary) -> ValidationResult:
	var result := ValidationResult.new()

	if not _schemas.has(type_name):
		# No schema registered — pass by default
		return result

	var rules: Array = _schemas[type_name]
	for rule_ref in rules:
		var rule: FieldRule = rule_ref
		_validate_field(data, rule, type_name, result)

	return result


## Validate an array of definitions (typical for GameData.load_definitions output).
func validate_definitions(type_name: StringName, definitions: Array) -> ValidationResult:
	var result := ValidationResult.new()

	if not _schemas.has(type_name):
		return result

	for i in range(definitions.size()):
		var entry_result := validate_definition(type_name, definitions[i])
		if not entry_result.is_valid:
			for error in entry_result.errors:
				result.add_error("[%d]" % i, error)

	return result


## Check if a schema is registered for a type.
func has_schema(type_name: StringName) -> bool:
	return _schemas.has(type_name)


## Remove all registered schemas.
func clear() -> void:
	_schemas.clear()


## Create a FieldRule for fluent schema definition.
static func field(name: String, type: String = "", required: bool = false) -> FieldRule:
	return FieldRule.new(name, type, required)


func _validate_field(data: Dictionary, rule: FieldRule, context: String, result: ValidationResult) -> void:
	var path := "%s.%s" % [context, rule.field_name]

	# Required check
	if rule.required and not data.has(rule.field_name):
		result.add_error(path, "required field missing")
		return

	if not data.has(rule.field_name):
		return  # Optional field not present — OK

	var value = data[rule.field_name]

	# Type check
	if rule.expected_type != "":
		if not _check_type(value, rule.expected_type):
			result.add_error(path, "expected type '%s', got '%s'" % [
				rule.expected_type, typeof(value)])
			return

	# Numeric range check
	if rule.expected_type in ["int", "float"] and value is float or value is int:
		var num_val := float(value)
		if num_val < rule.min_value:
			result.add_error(path, "value %s below minimum %s" % [value, rule.min_value])
		if num_val > rule.max_value:
			result.add_error(path, "value %s above maximum %s" % [value, rule.max_value])

	# Enum check
	if not rule.enum_values.is_empty():
		if not rule.enum_values.has(value):
			result.add_error(path, "value '%s' not in enum %s" % [value, rule.enum_values])

	# Length check (strings and arrays)
	if rule.min_length > 0:
		if value is String and value.length() < rule.min_length:
			result.add_error(path, "string length %d below minimum %d" % [value.length(), rule.min_length])
		elif value is Array and value.size() < rule.min_length:
			result.add_error(path, "array size %d below minimum %d" % [value.size(), rule.min_length])


func _check_type(value: Variant, expected: String) -> bool:
	match expected:
		"int":
			return value is int or value is float
		"float":
			return value is float or value is int
		"string":
			return value is String
		"bool":
			return value is bool
		"array":
			return value is Array
		"dict":
			return value is Dictionary
	return true  # Unknown type — pass
