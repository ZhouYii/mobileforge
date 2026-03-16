extends RefCounted
class_name MFTestBase
## Lightweight test base class for MobileForge tests.
## Each test file extends this class and defines test_* methods.
## The run_all() method discovers and runs them automatically.

var _passed := 0
var _failed := 0
var _current_test := ""
var _suite_failures: Array[String] = []


# ---------------------------------------------------------------------------
# Assertions
# ---------------------------------------------------------------------------

func assert_eq(actual: Variant, expected: Variant, msg := "") -> void:
	if typeof(actual) == typeof(expected) and actual == expected:
		_passed += 1
	elif str(actual) == str(expected):
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected '%s' (%s), got '%s' (%s)" % [
			_current_test, str(expected), type_string(typeof(expected)),
			str(actual), type_string(typeof(actual))
		]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_ne(actual: Variant, expected: Variant, msg := "") -> void:
	if typeof(actual) != typeof(expected) or actual != expected:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected value to differ from '%s'" % [
			_current_test, str(expected)
		]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_true(condition: bool, msg := "") -> void:
	if condition:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected true" % _current_test
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_false(condition: bool, msg := "") -> void:
	if not condition:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected false" % _current_test
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_null(value: Variant, msg := "") -> void:
	if value == null:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected null, got '%s'" % [_current_test, str(value)]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_not_null(value: Variant, msg := "") -> void:
	if value != null:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected non-null" % _current_test
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_gt(actual: Variant, expected: Variant, msg := "") -> void:
	if actual > expected:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected %s > %s" % [_current_test, str(actual), str(expected)]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_gte(actual: Variant, expected: Variant, msg := "") -> void:
	if actual >= expected:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected %s >= %s" % [_current_test, str(actual), str(expected)]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_has(dict: Dictionary, key: Variant, msg := "") -> void:
	if dict.has(key):
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: dictionary missing key '%s'" % [_current_test, str(key)]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_not_has(dict: Dictionary, key: Variant, msg := "") -> void:
	if not dict.has(key):
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: dictionary should not have key '%s'" % [_current_test, str(key)]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


func assert_size(collection: Variant, expected_size: int, msg := "") -> void:
	var actual_size: int = collection.size() if collection != null else -1
	if actual_size == expected_size:
		_passed += 1
	else:
		_failed += 1
		var err_msg := "[FAIL] %s: expected size %d, got %d" % [_current_test, expected_size, actual_size]
		if msg != "":
			err_msg += " - " + msg
		_suite_failures.append(err_msg)
		printerr(err_msg)


# ---------------------------------------------------------------------------
# Lifecycle hooks (override in subclasses)
# ---------------------------------------------------------------------------

func before_each() -> void:
	pass


func after_each() -> void:
	pass


func before_all() -> void:
	pass


func after_all() -> void:
	pass


# ---------------------------------------------------------------------------
# Runner
# ---------------------------------------------------------------------------

func run_all() -> Dictionary:
	var methods: Array[String] = []
	for method in get_method_list():
		if method.name.begins_with("test_"):
			methods.append(method.name)
	methods.sort()

	var script_path: String = ""
	if get_script() != null:
		script_path = get_script().resource_path.get_file()
	if script_path == "":
		script_path = "UnknownSuite"

	print("  Suite: %s (%d tests)" % [script_path, methods.size()])

	before_all()

	for method_name in methods:
		_current_test = method_name
		var fail_before := _failed
		before_each()
		call(method_name)
		after_each()
		if _failed == fail_before:
			print("    [PASS] %s" % method_name)
		else:
			print("    [FAIL] %s" % method_name)

	after_all()

	print("    -- %d passed, %d failed --" % [_passed, _failed])
	return {"passed": _passed, "failed": _failed}
