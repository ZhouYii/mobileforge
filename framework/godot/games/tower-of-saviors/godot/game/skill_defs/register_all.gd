class_name TosSkillRegistration extends RefCounted
## Registers all ToS-specific skill conditions and outcomes with the framework.


static func register(pipeline: MFSkillPipeline) -> void:
	_register_conditions(pipeline)
	_register_simple_effects(pipeline)
	_register_persistent_outcomes(pipeline)


static func _register_conditions(pipeline: MFSkillPipeline) -> void:
	pipeline.condition_registry.register("always_true", func(params):
		return AlwaysTrueCondition.new(params))
	pipeline.condition_registry.register("combo_above", func(params):
		return TosComboAboveCondition.new(params))
	pipeline.condition_registry.register("hp_below", func(params):
		return TosHpThresholdCondition.new(params))
	pipeline.condition_registry.register("elements_matched", func(params):
		return TosElementsMatchedCondition.new(params))
	pipeline.condition_registry.register("team_has_element", func(params):
		return TosTeamHasElementCondition.new(params))
	# New conditions for skills.json
	pipeline.condition_registry.register("combo_gte", func(params):
		return TosComboGteCondition.new(params))
	pipeline.condition_registry.register("combo_lt", func(params):
		return TosComboLtCondition.new(params))


static func _register_simple_effects(pipeline: MFSkillPipeline) -> void:
	# Existing effects
	pipeline.effect_registry.register("area_damage", TosAreaDamageOutcome.execute)
	pipeline.effect_registry.register("heal_flat", _heal_flat)
	pipeline.effect_registry.register("heal_percent", _heal_percent)
	pipeline.effect_registry.register("change_gem_element", TosChangeGemElementOutcome.execute)
	pipeline.effect_registry.register("delay_enemies", TosDelayEnemiesOutcome.execute)

	# New simple effects
	pipeline.effect_registry.register("single_target_damage", _single_target_damage)
	pipeline.effect_registry.register("gem_conversion", _gem_conversion)
	pipeline.effect_registry.register("self_damage", _self_damage)
	pipeline.effect_registry.register("element_change", _element_change)
	pipeline.effect_registry.register("rec_buff", _rec_buff)
	pipeline.effect_registry.register("lifesteal", _lifesteal)

	# Status effect skills
	pipeline.effect_registry.register("bind", TosBindEnemyOutcome.execute)
	pipeline.effect_registry.register("stun", TosStunEnemyOutcome.execute)
	pipeline.effect_registry.register("gravity_damage", TosGravityDamageOutcome.execute)

	# Board manipulation skills
	pipeline.effect_registry.register("orb_spawn", TosOrbSpawnOutcome.execute)

	# Shield-piercing skills
	pipeline.effect_registry.register("void_damage_absorb", TosVoidDamageAbsorbOutcome.execute)
	pipeline.effect_registry.register("void_element_shield", TosAttributeAbsorbShieldOutcome.execute)

	# Team skill effects (passive stat multipliers)
	pipeline.effect_registry.register("element_atk_mult", _element_atk_mult)
	pipeline.effect_registry.register("element_hp_mult", _element_hp_mult)
	pipeline.effect_registry.register("element_rec_mult", _element_rec_mult)


static func _register_persistent_outcomes(pipeline: MFSkillPipeline) -> void:
	pipeline.outcome_registry.register("heal_over_time", func(params):
		return TosHealOverTimeOutcome.new(params))
	pipeline.outcome_registry.register("atk_buff", func(params):
		return TosAtkBuffOutcome.new(params))
	pipeline.outcome_registry.register("defense_buff", func(params):
		return TosDefenseBuffOutcome.new(params))
	pipeline.outcome_registry.register("combo_scaling_atk", func(params):
		return TosComboScalingAtkOutcome.new(params))
	pipeline.outcome_registry.register("poison_dot", func(params):
		return TosPoisonDotOutcome.new(params))
	pipeline.outcome_registry.register("counter_attack", func(params):
		return TosCounterAttackOutcome.new(params))


# ---- Simple Effect Implementations ----

static func _heal_flat(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	result.healing += int(params.get("amount", 0))


static func _heal_percent(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	result.healing += int(ctx.max_hp * float(params.get("percent", 0.0)))


static func _single_target_damage(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var mult := float(params.get("multiplier", 1.0))
	var atk := 0.0
	if not ctx.team_stats.is_empty() and ctx.team_stats[0] != null:
		atk = ctx.team_stats[0].atk

	# Find target: "highest_hp" selects the enemy with highest current HP
	var target_strategy: String = params.get("target", "highest_hp")
	var target_idx := -1
	var best_hp := -1

	for i in range(ctx.enemies.size()):
		if ctx.enemies[i].is_alive:
			if target_strategy == "highest_hp":
				if ctx.enemies[i].hp > best_hp:
					best_hp = ctx.enemies[i].hp
					target_idx = i
			elif target_idx < 0:
				target_idx = i  # fallback: first alive

	if target_idx >= 0:
		result.damage_dealt[target_idx] = int(atk * mult)


static func _gem_conversion(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	# Alias wrapping change_gem_element with from_element/to_element param names
	var from_elem := int(params.get("from_element", params.get("from", 0)))
	var to_elem := int(params.get("to_element", params.get("to", 0)))
	if ctx.board == null:
		return
	for pos in range(ctx.board.config.total_cells()):
		var gem = ctx.board.get_gem(pos)
		if gem != null and gem.element == from_elem:
			ctx.board.change_gem_element(pos, to_elem)
			result.board_changes.append({"pos": pos, "old_element": from_elem, "new_element": to_elem})


static func _self_damage(params: Dictionary, ctx: RefCounted, _result: RefCounted) -> void:
	var pct := float(params.get("hp_percent", 0.0))
	var dmg := int(ctx.max_hp * pct)
	ctx.team_hp = maxi(ctx.team_hp - dmg, 1)  # Don't kill yourself


static func _element_change(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	# Temporary element change for a monster. Tracked via buffs_applied.
	var to_elem := int(params.get("to_element", 0))
	var duration := int(params.get("duration_turns", 1))
	result.buffs_applied.append({
		"type": "element_change", "to_element": to_elem, "turns": duration
	})


static func _rec_buff(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	var mult := float(params.get("multiplier", 1.0))
	var duration := int(params.get("duration_turns", 1))
	result.buffs_applied.append({
		"type": "rec_buff", "multiplier": mult, "turns": duration
	})


static func _lifesteal(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	var pct := float(params.get("percent_of_damage", 0.3))
	# Heal based on total damage dealt by previous outcomes in this activation
	var total_dmg := 0
	for idx in result.damage_dealt:
		total_dmg += result.damage_dealt[idx]
	result.healing += int(total_dmg * pct)


static func _element_atk_mult(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	result.buffs_applied.append({
		"type": "element_atk_mult",
		"element": int(params.get("element", 0)),
		"multiplier": float(params.get("multiplier", 1.0)),
	})


static func _element_hp_mult(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	result.buffs_applied.append({
		"type": "element_hp_mult",
		"element": int(params.get("element", 0)),
		"multiplier": float(params.get("multiplier", 1.0)),
	})


static func _element_rec_mult(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	result.buffs_applied.append({
		"type": "element_rec_mult",
		"element": int(params.get("element", 0)),
		"multiplier": float(params.get("multiplier", 1.0)),
	})


# ---- Condition Classes (only trivial ones remain inline) ----

class AlwaysTrueCondition extends MFSkillCondition:
	func is_valid(_ctx: RefCounted) -> bool:
		return true
