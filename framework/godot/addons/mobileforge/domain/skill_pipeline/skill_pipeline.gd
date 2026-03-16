class_name MFSkillPipeline extends RefCounted
## Orchestrates skill activation/deactivation.
## Manages condition/outcome registries and active persistent outcomes.

var condition_registry: RefCounted  # MFSkillCondition.ConditionRegistry
var outcome_registry: RefCounted  # MFSkillOutcome.OutcomeRegistry
var effect_registry: MFEffectRegistry

var _active_outcomes: Array = []  # Array of {outcome: MFSkillOutcome, skill_id: int}


func _init() -> void:
	condition_registry = MFSkillCondition.ConditionRegistry.new()
	outcome_registry = MFSkillOutcome.OutcomeRegistry.new()
	effect_registry = MFEffectRegistry.new()


## Load a SkillDef from parsed JSON data
func load_skill_def(data: Dictionary) -> RefCounted:  # -> SkillDef
	return MFSkillTypes.SkillDef.new(data)


## Activate a skill. Evaluates conditions, executes outcomes.
## Returns SkillResult with all changes made.
func activate_skill(skill_def: RefCounted, context: RefCounted) -> RefCounted:  # SkillDef, SkillContext -> SkillResult
	var result = MFSkillTypes.SkillResult.new()

	for rule in skill_def.rules:
		# Check all conditions
		var all_met := true
		var condition_instances: Array = []
		for cond_data in rule.conditions:
			var cond = condition_registry.create(cond_data["type"], cond_data.get("params", {}))
			if cond == null or not cond.is_valid(context):
				all_met = false
				break
			condition_instances.append(cond)

		if not all_met:
			continue

		# Execute all outcomes for this rule
		for out_data in rule.outcomes:
			var outcome_type: String = out_data["type"]
			var params: Dictionary = out_data.get("params", {})
			if out_data.has("duration"):
				params["duration"] = out_data["duration"]

			# Try simple effect registry first
			if effect_registry.has_effect(outcome_type):
				effect_registry.execute(outcome_type, params, context, result)
				continue

			# Try outcome registry (complex outcomes)
			var outcome = outcome_registry.create(outcome_type, params)
			if outcome == null:
				continue

			outcome.activate(context, result)

			# If persistent, track it
			if outcome.is_persistent():
				_active_outcomes.append({"outcome": outcome, "skill_id": skill_def.id})

	return result


## Deactivate all outcomes for a specific skill
func deactivate_skill(skill_id: int, context: RefCounted) -> void:
	for i in range(_active_outcomes.size() - 1, -1, -1):
		if _active_outcomes[i]["skill_id"] == skill_id:
			_active_outcomes[i]["outcome"].deactivate(context)
			_active_outcomes.remove_at(i)


## Called at the start of each turn
func process_turn_start(context: RefCounted) -> void:
	for entry in _active_outcomes:
		entry["outcome"].on_turn_start(context)


## Called at the end of each turn. Ticks duration and removes expired outcomes.
func process_turn_end(context: RefCounted) -> void:
	for i in range(_active_outcomes.size() - 1, -1, -1):
		var outcome = _active_outcomes[i]["outcome"]
		outcome.on_turn_end(context)
		if outcome.turns_left > 0:
			outcome.turns_left -= 1
			if outcome.turns_left <= 0:
				outcome.deactivate(context)
				_active_outcomes.remove_at(i)


## Get count of active persistent outcomes
func active_outcome_count() -> int:
	return _active_outcomes.size()


## Clear all active outcomes
func clear_active_outcomes(context: RefCounted) -> void:
	for entry in _active_outcomes:
		entry["outcome"].deactivate(context)
	_active_outcomes.clear()
