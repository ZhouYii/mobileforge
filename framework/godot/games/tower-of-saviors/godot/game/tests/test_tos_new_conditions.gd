extends MFTestBase
## Tests for new ToS conditions (ComboGte, ComboLt)

const ComboGteScript = preload("res://game/skill_defs/conditions/combo_gte.gd")
const ComboLtScript = preload("res://game/skill_defs/conditions/combo_lt.gd")


func test_combo_gte_passes_at_min() -> void:
	var cond = TosComboGteCondition.new({"min_combo": 5})
	var ctx = _make_context({"combo_count": 5})
	assert_true(cond.is_valid(ctx), "combo_gte should pass when combo_count >= min_combo")


func test_combo_gte_passes_above_min() -> void:
	var cond = TosComboGteCondition.new({"min_combo": 5})
	var ctx = _make_context({"combo_count": 7})
	assert_true(cond.is_valid(ctx), "combo_gte should pass when combo_count > min_combo")


func test_combo_gte_fails_below_min() -> void:
	var cond = TosComboGteCondition.new({"min_combo": 5})
	var ctx = _make_context({"combo_count": 3})
	assert_false(cond.is_valid(ctx), "combo_gte should fail when combo_count < min_combo")


func test_combo_gte_default_min() -> void:
	var cond = TosComboGteCondition.new({})
	var ctx = _make_context({"combo_count": 1})
	assert_false(cond.is_valid(ctx), "combo_gte default min_combo should be 1")


func test_combo_lt_passes_below_max() -> void:
	var cond = TosComboLtCondition.new({"max_combo": 5})
	var ctx = _make_context({"combo_count": 3})
	assert_true(cond.is_valid(ctx), "combo_lt should pass when combo_count < max_combo")


func test_combo_lt_passes_at_just_below() -> void:
	var cond = TosComboLtCondition.new({"max_combo": 5})
	var ctx = _make_context({"combo_count": 4})
	assert_true(cond.is_valid(ctx), "combo_lt should pass when combo_count = max_combo - 1")


func test_combo_lt_fails_at_max() -> void:
	var cond = TosComboLtCondition.new({"max_combo": 5})
	var ctx = _make_context({"combo_count": 5})
	assert_false(cond.is_valid(ctx), "combo_lt should fail when combo_count >= max_combo")


func test_combo_lt_fails_above_max() -> void:
	var cond = TosComboLtCondition.new({"max_combo": 5})
	var ctx = _make_context({"combo_count": 7})
	assert_false(cond.is_valid(ctx), "combo_lt should fail when combo_count > max_combo")


func test_combo_lt_default_max() -> void:
	var cond = TosComboLtCondition.new({})
	var ctx = _make_context({"combo_count": 0})
	assert_true(cond.is_valid(ctx), "combo_lt default max_combo should be high")


func _make_context(overrides: Dictionary = {}) -> RefCounted:
	var ctx = RefCounted.new()
	ctx.set("combo_count", overrides.get("combo_count", 1))
	return ctx
