extends MFTestBase
## Tests for ToS skill conditions (combo_above, hp_threshold, elements_matched, team_has_element)

const ComboAboveScript = preload("res://game/skill_defs/conditions/combo_above.gd")
const HpThresholdScript = preload("res://game/skill_defs/conditions/hp_threshold.gd")
const ElementsMatchedScript = preload("res://game/skill_defs/conditions/elements_matched.gd")
const TeamHasElementScript = preload("res://game/skill_defs/conditions/team_has_element.gd")


func test_combo_above_passes_at_threshold() -> void:
	var cond = TosComboAboveCondition.new({"threshold": 5})
	var ctx = _make_context({"combo_count": 5})
	assert_true(cond.is_valid(ctx), "combo_above should pass when combo_count >= threshold")


func test_combo_above_fails_below_threshold() -> void:
	var cond = TosComboAboveCondition.new({"threshold": 5})
	var ctx = _make_context({"combo_count": 3})
	assert_false(cond.is_valid(ctx), "combo_above should fail when combo_count < threshold")


func test_combo_above_default_threshold() -> void:
	var cond = TosComboAboveCondition.new({})
	var ctx = _make_context({"combo_count": 1})
	assert_true(cond.is_valid(ctx), "combo_above with no params should default to threshold 1")


func test_hp_threshold_passes_below() -> void:
	var cond = TosHpThresholdCondition.new({"percent": 0.5})
	var ctx = _make_context({"team_hp": 3000, "max_hp": 10000})
	assert_true(cond.is_valid(ctx), "hp_threshold should pass when HP ratio <= percent")


func test_hp_threshold_fails_above() -> void:
	var cond = TosHpThresholdCondition.new({"percent": 0.3})
	var ctx = _make_context({"team_hp": 8000, "max_hp": 10000})
	assert_false(cond.is_valid(ctx), "hp_threshold should fail when HP ratio > percent")


func test_hp_threshold_handles_zero_max_hp() -> void:
	var cond = TosHpThresholdCondition.new({"percent": 0.5})
	var ctx = _make_context({"team_hp": 0, "max_hp": 0})
	assert_false(cond.is_valid(ctx), "hp_threshold should return false when max_hp = 0")


func test_hp_threshold_exact_boundary() -> void:
	var cond = TosHpThresholdCondition.new({"percent": 0.5})
	var ctx = _make_context({"team_hp": 5000, "max_hp": 10000})
	assert_true(cond.is_valid(ctx), "hp_threshold should pass at exact boundary (HP ratio == percent)")


func test_elements_matched_passes_at_count() -> void:
	var cond = TosElementsMatchedCondition.new({"element": 1, "min_count": 3})
	var ctx = _make_context({"elements_matched": {1: 4, 2: 1}})
	assert_true(cond.is_valid(ctx), "elements_matched should pass when count >= min_count")


func test_elements_matched_fails_below_count() -> void:
	var cond = TosElementsMatchedCondition.new({"element": 1, "min_count": 5})
	var ctx = _make_context({"elements_matched": {1: 3, 2: 2}})
	assert_false(cond.is_valid(ctx), "elements_matched should fail when count < min_count")


func test_elements_matched_missing_element() -> void:
	var cond = TosElementsMatchedCondition.new({"element": 3, "min_count": 1})
	var ctx = _make_context({"elements_matched": {1: 5, 2: 3}})
	assert_false(cond.is_valid(ctx), "elements_matched should fail when element not in matched")


func test_team_has_element_finds_element() -> void:
	var cond = TosTeamHasElementCondition.new({"element": 1})
	var ctx = _make_context({"team": [
		{"element": 2},
		{"element": 1},
		{"element": 3},
	]})
	assert_true(cond.is_valid(ctx), "team_has_element should find element in team")


func test_team_has_element_missing_element() -> void:
	var cond = TosTeamHasElementCondition.new({"element": 5})
	var ctx = _make_context({"team": [
		{"element": 1},
		{"element": 2},
		{"element": 3},
	]})
	assert_false(cond.is_valid(ctx), "team_has_element should fail when no team member has element")


func test_team_has_element_empty_team() -> void:
	var cond = TosTeamHasElementCondition.new({"element": 1})
	var ctx = _make_context({"team": []})
	assert_false(cond.is_valid(ctx), "team_has_element should fail with empty team")


func test_team_has_element_handles_null() -> void:
	var cond = TosTeamHasElementCondition.new({"element": 1})
	var ctx = _make_context({"team": [null, {"element": 2}, null]})
	assert_false(cond.is_valid(ctx), "team_has_element should handle null team members")


func _make_context(overrides: Dictionary = {}) -> RefCounted:
	var ctx = RefCounted.new()
	ctx.set("combo_count", overrides.get("combo_count", 1))
	ctx.set("team_hp", overrides.get("team_hp", 10000))
	ctx.set("max_hp", overrides.get("max_hp", 10000))
	ctx.set("elements_matched", overrides.get("elements_matched", {}))
	ctx.set("team", overrides.get("team", []))
	return ctx
