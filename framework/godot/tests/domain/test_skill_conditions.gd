extends MFTestBase
## Tests for SkillCondition base class and ConditionRegistry
## (domain/skill_pipeline/skill_condition.gd)

const SkillTypesScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_types.gd")
const SkillConditionScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_condition.gd")


# ---------------------------------------------------------------------------
# Test condition subclasses
# ---------------------------------------------------------------------------

## Custom condition: checks context.combo_count >= params.min_combo.
class MinComboCondition extends MFSkillCondition:
	func is_valid(context: RefCounted) -> bool:
		var min_combo = get_param("min_combo", 1)
		return context.combo_count >= min_combo


## Tracks on_activate calls.
class TrackingCondition extends MFSkillCondition:
	var activate_count: int = 0

	func is_valid(context: RefCounted) -> bool:
		return true

	func on_activate(context: RefCounted) -> void:
		activate_count += 1


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _make_context(combo: int = 0) -> RefCounted:
	var ctx = MFSkillTypes.SkillContext.new()
	ctx.combo_count = combo
	return ctx


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_base_condition_default_false() -> void:
	# The base class is_valid returns false by default
	var cond = MFSkillCondition.new()
	var ctx = _make_context()
	assert_false(cond.is_valid(ctx),
		"base MFSkillCondition.is_valid should return false")


func test_custom_condition() -> void:
	# MinComboCondition with min_combo=3
	var cond = MinComboCondition.new({"min_combo": 3})

	var ctx_low = _make_context(2)
	assert_false(cond.is_valid(ctx_low),
		"combo 2 should not satisfy min_combo=3")

	var ctx_exact = _make_context(3)
	assert_true(cond.is_valid(ctx_exact),
		"combo 3 should satisfy min_combo=3")

	var ctx_high = _make_context(5)
	assert_true(cond.is_valid(ctx_high),
		"combo 5 should satisfy min_combo=3")


func test_condition_registry_register_and_create() -> void:
	var registry = MFSkillCondition.ConditionRegistry.new()
	registry.register("min_combo", func(params):
		return MinComboCondition.new(params))

	assert_true(registry.has_type("min_combo"),
		"min_combo should be registered")

	var cond = registry.create("min_combo", {"min_combo": 2})
	assert_not_null(cond, "create should return a condition instance")
	# Verify it works
	var ctx = _make_context(2)
	assert_true(cond.is_valid(ctx),
		"created condition should validate context correctly")


func test_condition_registry_unknown_type_returns_null() -> void:
	var registry = MFSkillCondition.ConditionRegistry.new()
	var cond = registry.create("nonexistent_type", {})
	assert_null(cond, "unknown type should return null")


func test_condition_get_param() -> void:
	var cond = MinComboCondition.new({"min_combo": 7, "extra_key": "hello"})
	# get_param should return the correct values
	assert_eq(cond.get_param("min_combo", 0), 7,
		"should retrieve min_combo param")
	assert_eq(cond.get_param("extra_key", ""), "hello",
		"should retrieve extra_key param")
	assert_eq(cond.get_param("missing_key", 42), 42,
		"missing key should return default")


func test_condition_on_activate_called() -> void:
	var cond = TrackingCondition.new()
	var ctx = _make_context()

	assert_eq(cond.activate_count, 0, "activate_count should start at 0")

	cond.on_activate(ctx)
	assert_eq(cond.activate_count, 1, "on_activate should increment count")

	cond.on_activate(ctx)
	assert_eq(cond.activate_count, 2, "second on_activate should increment again")
