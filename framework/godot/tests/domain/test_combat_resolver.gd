extends MFTestBase
## Tests for CombatResolver (domain/combat/combat_resolver.gd)
## Uses damage_calc_cases.json test vectors for validation.

const CombatTypesScript = preload("res://addons/mobileforge/domain/combat/combat_types.gd")
const ElementChartScript = preload("res://addons/mobileforge/domain/combat/element_chart.gd")
const CombatResolverScript = preload("res://addons/mobileforge/domain/combat/combat_resolver.gd")

var _chart: MFElementChart
var _resolver: MFCombatResolver


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_chart = MFElementChart.new()
	_resolver = MFCombatResolver.new(_chart)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _make_ctx(atk: float, atk_elem: int, def_elem: int, defense: float,
		gems: int, combo_count: int, combo_index: int = 0) -> RefCounted:
	var ctx = MFCombatTypes.DamageContext.new()
	ctx.attacker_atk = atk
	ctx.attacker_element = atk_elem
	ctx.defender_element = def_elem
	ctx.defender_defense = defense
	ctx.gems_matched = gems
	ctx.combo_count = combo_count
	ctx.combo_index = combo_index
	return ctx


## Track hook call order
var _hook_call_order: Array[String] = []


func _hook_double_damage(ctx: RefCounted) -> void:
	ctx.damage *= 2.0


func _hook_add_100(ctx: RefCounted) -> void:
	ctx.damage += 100.0


func _hook_track_first(ctx: RefCounted) -> void:
	_hook_call_order.append("first")


func _hook_track_second(ctx: RefCounted) -> void:
	_hook_call_order.append("second")


# ---------------------------------------------------------------------------
# Tests — damage_calc_cases test vectors
# ---------------------------------------------------------------------------

func test_basic_3_gem_single_combo() -> void:
	# 1000 ATK, 3 water gems, 1 combo, water vs water (neutral)
	var ctx := _make_ctx(1000.0, 1, 1, 0.0, 3, 1)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 1000, "basic 3-gem single combo should deal 1000 damage")
	assert_eq(result.combo_multiplier, 1.0, "combo multiplier should be 1.0")
	assert_eq(result.element_multiplier, 1.0, "element multiplier should be 1.0")


func test_5_gems_3_combos_advantage() -> void:
	# 1000 ATK, 5 gems, 3 combos, water vs fire (1.5x advantage)
	var ctx := _make_ctx(1000.0, 1, 2, 0.0, 5, 3, 2)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 3375, "5 gems, 3 combos, advantage should deal 3375")
	assert_eq(result.element_multiplier, 1.5, "element multiplier should be 1.5")
	assert_eq(result.combo_multiplier, 1.5, "combo multiplier should be 1.5")


func test_disadvantage_with_defense() -> void:
	# 2000 ATK, 4 gems, 2 combos, fire vs water (0.5x), 500 defense
	var ctx := _make_ctx(2000.0, 2, 1, 500.0, 4, 2, 1)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 1063, "disadvantage with defense should deal 1063")
	assert_eq(result.element_multiplier, 0.5, "element multiplier should be 0.5")
	assert_eq(result.combo_multiplier, 1.25, "combo multiplier should be 1.25")


func test_minimum_damage_1() -> void:
	# Very high defense still gives minimum 1
	var ctx := _make_ctx(100.0, 3, 3, 99999.0, 3, 1)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 1, "minimum damage should be 1")


func test_light_dark_mutual() -> void:
	# Light vs Dark = 1.5x
	var ctx := _make_ctx(1000.0, 4, 5, 0.0, 3, 1)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 1500, "light vs dark should deal 1500")
	assert_eq(result.element_multiplier, 1.5, "element multiplier should be 1.5")

	# Dark vs Light = also 1.5x
	var ctx2 := _make_ctx(1000.0, 5, 4, 0.0, 3, 1)
	var result2 := _resolver.resolve_player_attack(ctx2)
	assert_eq(result2.final_damage, 1500, "dark vs light should also deal 1500")
	assert_eq(result2.element_multiplier, 1.5, "element multiplier should be 1.5")


func test_heart_no_advantage() -> void:
	# Heart vs anything = 1.0x (neutral)
	var ctx := _make_ctx(1000.0, 6, 1, 0.0, 3, 1)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 1000, "heart should deal neutral damage")
	assert_eq(result.element_multiplier, 1.0, "element multiplier should be 1.0")


func test_10_combo_high_gems() -> void:
	# 500 ATK, 8 gems, 10 combos, water vs fire, 100 defense
	var ctx := _make_ctx(500.0, 1, 2, 100.0, 8, 10, 9)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 5384, "10 combo high gems should deal 5384")
	assert_eq(result.combo_multiplier, 3.25, "combo multiplier should be 3.25")
	assert_eq(result.element_multiplier, 1.5, "element multiplier should be 1.5")


# ---------------------------------------------------------------------------
# Tests — hooks
# ---------------------------------------------------------------------------

func test_hook_modifies_damage() -> void:
	# Register a MAIN hook that doubles damage
	_resolver.register_hook(MFCombatTypes.DamageHook.MAIN, _hook_double_damage, 100, "double")
	var ctx := _make_ctx(1000.0, 1, 1, 0.0, 3, 1)
	var result := _resolver.resolve_player_attack(ctx)
	# Base = 1000, combo = 1.0, elem = 1.0, MAIN doubles -> 2000, no defense
	assert_eq(result.final_damage, 2000, "hook should double the damage")
	assert_true(result.hooks_applied.has("double"), "hook name should be in hooks_applied")


func test_hook_unregister() -> void:
	# Register then unregister hook
	_resolver.register_hook(MFCombatTypes.DamageHook.MAIN, _hook_double_damage, 100, "double")
	_resolver.unregister_hook(MFCombatTypes.DamageHook.MAIN, _hook_double_damage)
	var ctx := _make_ctx(1000.0, 1, 1, 0.0, 3, 1)
	var result := _resolver.resolve_player_attack(ctx)
	assert_eq(result.final_damage, 1000, "unregistered hook should not affect damage")
	assert_false(result.hooks_applied.has("double"), "hook name should not be in hooks_applied")


func test_hook_priority() -> void:
	# Two hooks with different priorities fire in correct order
	# Lower priority number fires first
	_hook_call_order.clear()
	_resolver.register_hook(MFCombatTypes.DamageHook.MAIN, _hook_track_second, 200, "second")
	_resolver.register_hook(MFCombatTypes.DamageHook.MAIN, _hook_track_first, 50, "first")
	var ctx := _make_ctx(1000.0, 1, 1, 0.0, 3, 1)
	_resolver.resolve_player_attack(ctx)
	assert_eq(_hook_call_order.size(), 2, "both hooks should fire")
	assert_eq(_hook_call_order[0], "first", "lower priority hook should fire first")
	assert_eq(_hook_call_order[1], "second", "higher priority hook should fire second")
