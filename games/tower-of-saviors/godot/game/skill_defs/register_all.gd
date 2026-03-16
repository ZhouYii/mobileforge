class_name TosSkillRegistration extends RefCounted
## Registers all ToS-specific skill conditions and outcomes with the framework.


static func register(pipeline: MFSkillPipeline) -> void:
	# Conditions
	pipeline.condition_registry.register("always_true", func(params):
		return AlwaysTrueCondition.new(params))
	pipeline.condition_registry.register("combo_above", func(params):
		return ComboAboveCondition.new(params))
	pipeline.condition_registry.register("hp_below", func(params):
		return HpBelowCondition.new(params))
	pipeline.condition_registry.register("elements_matched", func(params):
		return ElementsMatchedCondition.new(params))
	pipeline.condition_registry.register("team_has_element", func(params):
		return TeamHasElementCondition.new(params))

	# Simple effects
	pipeline.effect_registry.register("area_damage", _area_damage)
	pipeline.effect_registry.register("heal_flat", _heal_flat)
	pipeline.effect_registry.register("heal_percent", _heal_percent)
	pipeline.effect_registry.register("change_gem_element", _change_gem_element)
	pipeline.effect_registry.register("delay_enemies", _delay_enemies)


static func _area_damage(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var mult: float = float(params.get("multiplier", 1.0))
	var atk: float = 0.0
	if not ctx.team_stats.is_empty() and ctx.team_stats[0] != null:
		atk = ctx.team_stats[0].atk
	for i in range(ctx.enemies.size()):
		if ctx.enemies[i].is_alive:
			result.damage_dealt[i] = int(atk * mult)


static func _heal_flat(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	result.healing += int(params.get("amount", 0))


static func _heal_percent(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	result.healing += int(ctx.max_hp * float(params.get("percent", 0.0)))


static func _change_gem_element(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var from_elem := int(params.get("from", 0))
	var to_elem := int(params.get("to", 0))
	if ctx.board == null:
		return
	for pos in range(ctx.board.config.total_cells()):
		var gem = ctx.board.get_gem(pos)
		if gem != null and gem.element == from_elem:
			ctx.board.change_gem_element(pos, to_elem)
			result.board_changes.append({"pos": pos, "old_element": from_elem, "new_element": to_elem})


static func _delay_enemies(params: Dictionary, ctx: RefCounted, _result: RefCounted) -> void:
	var turns := int(params.get("turns", 1))
	for enemy in ctx.enemies:
		if enemy.is_alive:
			enemy.countdown += turns


# ---- Condition Classes ----

class AlwaysTrueCondition extends MFSkillCondition:
	func is_valid(_ctx: RefCounted) -> bool:
		return true


class ComboAboveCondition extends MFSkillCondition:
	func is_valid(ctx: RefCounted) -> bool:
		return ctx.combo_count >= int(get_param("threshold", 1))


class HpBelowCondition extends MFSkillCondition:
	func is_valid(ctx: RefCounted) -> bool:
		if ctx.max_hp <= 0: return false
		return float(ctx.team_hp) / float(ctx.max_hp) <= float(get_param("percent", 0.5))


class ElementsMatchedCondition extends MFSkillCondition:
	func is_valid(ctx: RefCounted) -> bool:
		var elem := int(get_param("element", 0))
		var min_count := int(get_param("min_count", 1))
		return ctx.elements_matched.get(elem, 0) >= min_count


class TeamHasElementCondition extends MFSkillCondition:
	func is_valid(ctx: RefCounted) -> bool:
		var elem := int(get_param("element", 0))
		for monster in ctx.team:
			if monster != null and monster.get("element") == elem:
				return true
		return false
