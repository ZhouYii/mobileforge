extends MFTestBase
## Tests for SkillPipeline (domain/skill_pipeline/skill_pipeline.gd)
## Covers: condition/outcome/effect registration, skill activation,
## persistent outcome tracking, cooldown calculation.

const SkillTypesScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_types.gd")
const SkillConditionScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_condition.gd")
const SkillOutcomeScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_outcome.gd")
const EffectRegistryScript = preload("res://addons/mobileforge/domain/skill_pipeline/effect_registry.gd")
const SkillCooldownScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_cooldown.gd")
const SkillPipelineScript = preload("res://addons/mobileforge/domain/skill_pipeline/skill_pipeline.gd")

var _pipeline: MFSkillPipeline


# ---------------------------------------------------------------------------
# Test condition/outcome subclasses
# ---------------------------------------------------------------------------

## Always-true condition: is_valid returns true unconditionally.
class AlwaysTrueCondition extends MFSkillCondition:
	func is_valid(context: RefCounted) -> bool:
		return true


## Combo-above condition: checks context.combo_count >= params.threshold.
class ComboAboveCondition extends MFSkillCondition:
	func is_valid(context: RefCounted) -> bool:
		var threshold = get_param("threshold", 0)
		return context.combo_count >= threshold


## Persistent test outcome: tracks activation in result.buffs_applied.
class DamageBuff extends MFSkillOutcome:
	func activate(context: RefCounted, result: RefCounted) -> void:
		result.buffs_applied.append({
			"type": "damage_buff",
			"turns": turns_left,
			"target": "team",
		})


# ---------------------------------------------------------------------------
# Simple effect callback (for effect_registry)
# ---------------------------------------------------------------------------

func _heal_flat_effect(params: Dictionary, context: RefCounted, result: RefCounted) -> void:
	result.healing += int(params.get("amount", 0))


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_pipeline = MFSkillPipeline.new()

	# Register test condition types
	_pipeline.condition_registry.register("always_true", func(params):
		return AlwaysTrueCondition.new(params))
	_pipeline.condition_registry.register("combo_above", func(params):
		return ComboAboveCondition.new(params))

	# Register test simple effect
	_pipeline.effect_registry.register("heal_flat", Callable(self, "_heal_flat_effect"))

	# Register test outcome type
	_pipeline.outcome_registry.register("damage_buff", func(params):
		return DamageBuff.new(params))


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _make_context(combo: int = 0) -> RefCounted:
	var ctx = MFSkillTypes.SkillContext.new()
	ctx.combo_count = combo
	return ctx


func _make_skill_def(rules_data: Array) -> RefCounted:
	return MFSkillTypes.SkillDef.new({
		"id": 1,
		"name": "Test Skill",
		"type": "active",
		"max_cd": 10,
		"min_cd": 5,
		"max_level": 5,
		"rules": rules_data,
	})


# ---------------------------------------------------------------------------
# Tests — registration
# ---------------------------------------------------------------------------

func test_register_condition_type() -> void:
	assert_true(_pipeline.condition_registry.has_type("always_true"),
		"always_true condition should be registered")
	assert_true(_pipeline.condition_registry.has_type("combo_above"),
		"combo_above condition should be registered")
	assert_false(_pipeline.condition_registry.has_type("nonexistent"),
		"nonexistent type should not be registered")


func test_register_outcome_type() -> void:
	assert_true(_pipeline.outcome_registry.has_type("damage_buff"),
		"damage_buff outcome should be registered")
	assert_false(_pipeline.outcome_registry.has_type("nonexistent"),
		"nonexistent outcome should not be registered")


func test_register_simple_effect() -> void:
	assert_true(_pipeline.effect_registry.has_effect("heal_flat"),
		"heal_flat effect should be registered")
	assert_false(_pipeline.effect_registry.has_effect("nonexistent"),
		"nonexistent effect should not be registered")


# ---------------------------------------------------------------------------
# Tests — activation
# ---------------------------------------------------------------------------

func test_activate_skill_simple_effect() -> void:
	# Skill with 1 rule, 1 always_true condition, 1 heal_flat outcome
	var skill_def = _make_skill_def([{
		"conditions": [{"type": "always_true", "params": {}}],
		"outcomes": [{"type": "heal_flat", "params": {"amount": 500}}],
	}])
	var ctx = _make_context()
	var result = _pipeline.activate_skill(skill_def, ctx)
	assert_eq(result.healing, 500, "heal_flat should set healing to 500")


func test_activate_skill_condition_not_met() -> void:
	# combo_above with threshold=5, but context has combo_count=3
	var skill_def = _make_skill_def([{
		"conditions": [{"type": "combo_above", "params": {"threshold": 5}}],
		"outcomes": [{"type": "heal_flat", "params": {"amount": 999}}],
	}])
	var ctx = _make_context(3)
	var result = _pipeline.activate_skill(skill_def, ctx)
	assert_eq(result.healing, 0, "condition not met -> no healing")


func test_activate_skill_condition_met() -> void:
	# combo_above with threshold=5, context has combo_count=5 -> outcome fires
	var skill_def = _make_skill_def([{
		"conditions": [{"type": "combo_above", "params": {"threshold": 5}}],
		"outcomes": [{"type": "heal_flat", "params": {"amount": 750}}],
	}])
	var ctx = _make_context(5)
	var result = _pipeline.activate_skill(skill_def, ctx)
	assert_eq(result.healing, 750, "condition met -> healing should be 750")


# ---------------------------------------------------------------------------
# Tests — persistent outcomes
# ---------------------------------------------------------------------------

func test_persistent_outcome_tracked() -> void:
	# Activate a skill with a damage_buff outcome that has duration=3
	var skill_def = _make_skill_def([{
		"conditions": [{"type": "always_true", "params": {}}],
		"outcomes": [{"type": "damage_buff", "params": {}, "duration": 3}],
	}])
	var ctx = _make_context()
	_pipeline.activate_skill(skill_def, ctx)
	assert_eq(_pipeline.active_outcome_count(), 1,
		"persistent outcome should be tracked")


func test_process_turn_end_ticks_duration() -> void:
	# Duration=3 outcome: after 3 turn ends, it should expire
	var skill_def = _make_skill_def([{
		"conditions": [{"type": "always_true", "params": {}}],
		"outcomes": [{"type": "damage_buff", "params": {}, "duration": 3}],
	}])
	var ctx = _make_context()
	_pipeline.activate_skill(skill_def, ctx)
	assert_eq(_pipeline.active_outcome_count(), 1, "start with 1 active outcome")

	_pipeline.process_turn_end(ctx)
	assert_eq(_pipeline.active_outcome_count(), 1, "after 1 turn, still active (2 left)")

	_pipeline.process_turn_end(ctx)
	assert_eq(_pipeline.active_outcome_count(), 1, "after 2 turns, still active (1 left)")

	_pipeline.process_turn_end(ctx)
	assert_eq(_pipeline.active_outcome_count(), 0, "after 3 turns, outcome should expire")


func test_deactivate_skill() -> void:
	var skill_def = _make_skill_def([{
		"conditions": [{"type": "always_true", "params": {}}],
		"outcomes": [{"type": "damage_buff", "params": {}, "duration": 5}],
	}])
	var ctx = _make_context()
	_pipeline.activate_skill(skill_def, ctx)
	assert_eq(_pipeline.active_outcome_count(), 1, "should have 1 active outcome")

	_pipeline.deactivate_skill(1, ctx)
	assert_eq(_pipeline.active_outcome_count(), 0, "after deactivate, should be 0")


func test_clear_active_outcomes() -> void:
	# Activate multiple persistent outcomes
	var skill_def_a = MFSkillTypes.SkillDef.new({
		"id": 10, "name": "A", "rules": [{
			"conditions": [{"type": "always_true", "params": {}}],
			"outcomes": [{"type": "damage_buff", "params": {}, "duration": 5}],
		}],
	})
	var skill_def_b = MFSkillTypes.SkillDef.new({
		"id": 20, "name": "B", "rules": [{
			"conditions": [{"type": "always_true", "params": {}}],
			"outcomes": [{"type": "damage_buff", "params": {}, "duration": 5}],
		}],
	})
	var ctx = _make_context()
	_pipeline.activate_skill(skill_def_a, ctx)
	_pipeline.activate_skill(skill_def_b, ctx)
	assert_eq(_pipeline.active_outcome_count(), 2, "should have 2 active outcomes")

	_pipeline.clear_active_outcomes(ctx)
	assert_eq(_pipeline.active_outcome_count(), 0, "after clear, should be 0")


# ---------------------------------------------------------------------------
# Tests — cooldown
# ---------------------------------------------------------------------------

func test_skill_cooldown_calculate() -> void:
	# calculate_cd(max_cd=10, min_cd=5, skill_level=1) = max(10+1-1, 5) = 10
	assert_eq(MFSkillCooldown.calculate_cd(10, 5, 1), 10,
		"skill_level=1: cd should be 10")
	# calculate_cd(max_cd=10, min_cd=5, skill_level=6) = max(10+1-6, 5) = max(5, 5) = 5
	assert_eq(MFSkillCooldown.calculate_cd(10, 5, 6), 5,
		"skill_level=6: cd should be min_cd=5")
	# calculate_cd(max_cd=10, min_cd=5, skill_level=10) = max(10+1-10, 5) = max(1, 5) = 5
	assert_eq(MFSkillCooldown.calculate_cd(10, 5, 10), 5,
		"skill_level=10: cd should clamp to min_cd=5")


func test_skill_cooldown_tick() -> void:
	# tick(3) = max(3-1, 0) = 2
	assert_eq(MFSkillCooldown.tick(3), 2, "tick(3) should return 2")
	# tick(1) = max(1-1, 0) = 0
	assert_eq(MFSkillCooldown.tick(1), 0, "tick(1) should return 0")
	# tick(0) = max(0-1, 0) = 0
	assert_eq(MFSkillCooldown.tick(0), 0, "tick(0) should return 0 (floor)")
