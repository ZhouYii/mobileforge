class_name DamagePipeline
extends RefCounted

## Multi-phase damage resolution pipeline with interceptor slots.
##
## Synthesized from 36/60 analyzed combat-oriented Unity mobile games.
## Primary sources: RAID: Shadow Legends (Entitas ECS 13-system effect pipeline:
## AddAppliedEffectSystem, AddBlockEffectSystem, AddEffectNullifiedSystem,
## AddTakeHitToModelEffectsSystem, IEffectProcessor with 16+ implementations,
## CritSkillEffectProcessor implementing IModifySkillDamage), AQ Battle Gems
## (14-phase CombatPhases enum: ValidateCaster -> TableLookup -> PreDamage ->
## Damage -> PostDamage -> Final, symmetric for caster/target), Cookie Run:
## Kingdom (BattleAbilityContainer.BattleAbilityEffectMultipliers for scaling),
## Archero (CustomJsonActionVO command-based damage dispatch with
## SyncFlashHitVO and SocketSyncExplodeEffect).
##
## Architecture:
##   A DamageEvent resource flows through four phases in order:
##     PRE_PROCESS  -> armor reduction, resistance checks, shields absorb
##     CALCULATE    -> base damage, crit roll, elemental multiplier
##     APPLY        -> HP reduction, overkill tracking, death check
##     POST_PROCESS -> lifesteal, on-hit procs, reflect damage, XP/combo
##
##   Each phase has an array of interceptor Callables. Interceptors receive the
##   DamageEvent, may modify it, and may cancel further processing by setting
##   event.cancelled = true.
##
## Usage:
##   var pipeline := DamagePipeline.new()
##   pipeline.add_interceptor(DamagePipeline.Phase.PRE_PROCESS, _apply_armor)
##   pipeline.add_interceptor(DamagePipeline.Phase.CALCULATE, _roll_crit)
##   pipeline.add_interceptor(DamagePipeline.Phase.APPLY, _reduce_hp)
##   pipeline.add_interceptor(DamagePipeline.Phase.POST_PROCESS, _lifesteal)
##   var event := pipeline.process(attacker_stats, defender_stats, 100.0)


## Emitted when a DamageEvent starts processing.
signal damage_started(event: DamageEvent)

## Emitted when a DamageEvent completes all phases (or is cancelled).
signal damage_resolved(event: DamageEvent)

## Emitted when an entity's HP reaches zero.
signal lethal_damage(event: DamageEvent)


## Pipeline phases in execution order.
enum Phase {
	PRE_PROCESS,   ## Armor, resistance, damage type checks, shields.
	CALCULATE,     ## Base damage computation, crit, element, ability scaling.
	APPLY,         ## HP subtraction, shield absorption, overkill.
	POST_PROCESS,  ## Lifesteal, on-hit effects, reflect, combo tracking.
}

## Damage element types. Archero uses elemental interactions; RAID uses affinity.
enum Element {
	NONE,
	FIRE,
	ICE,
	LIGHTNING,
	POISON,
	HOLY,
	DARK,
}

## Damage delivery categories.
enum DamageType {
	PHYSICAL,
	MAGICAL,
	TRUE,  ## Ignores all resistance (RAID: Shadow Legends "ignore defense" pattern).
	DOT,   ## Damage over time tick (RAID: DamageOverTimeSkillEffect).
}


## Data resource that flows through the pipeline. Mutable by interceptors.
## Modeled after RAID's SkillEffectData / AQ Battle Gems' combat event data.
class DamageEvent:
	## Attacker stat block (read from, not modified).
	var attacker_stats: StatBlock
	## Defender stat block (read from, modified by APPLY phase for HP).
	var defender_stats: StatBlock
	## Attacker node reference for position/callbacks.
	var attacker_node: Node = null
	## Defender node reference for position/callbacks.
	var defender_node: Node = null

	## Raw base damage before any modification.
	var base_damage: float = 0.0
	## Damage after PRE_PROCESS (armor etc. applied).
	var mitigated_damage: float = 0.0
	## Final damage after CALCULATE (crit, element applied).
	var final_damage: float = 0.0
	## Actual HP removed in APPLY (may differ if shield absorbed some).
	var applied_damage: float = 0.0

	## Element of this damage instance.
	var element: int = Element.NONE
	## Damage delivery type.
	var damage_type: int = DamageType.PHYSICAL

	## Whether a critical hit occurred.
	var is_critical: bool = false
	## Critical damage multiplier (e.g., 1.5 = 150% of base).
	var crit_multiplier: float = 1.5

	## Elemental effectiveness multiplier (e.g., 1.25 for strong, 0.75 for weak).
	var element_multiplier: float = 1.0

	## Amount absorbed by shields before HP loss.
	var shield_absorbed: float = 0.0
	## Amount of lifesteal healing applied to attacker.
	var lifesteal_amount: float = 0.0
	## Amount of damage reflected back to attacker.
	var reflected_damage: float = 0.0

	## Whether the damage was lethal (defender HP reached 0).
	var is_lethal: bool = false
	## Whether the event was cancelled (stops further phase processing).
	var cancelled: bool = false
	## Reason for cancellation (e.g., "immune", "evaded", "blocked").
	var cancel_reason: String = ""

	## Whether the damage was fully blocked (AQ Battle Gems: AddBlockEffectSystem).
	var is_blocked: bool = false
	## Whether the target evaded (RAID: EvadeEffect).
	var is_evaded: bool = false

	## Overkill damage (damage beyond what was needed to reach 0 HP).
	var overkill: float = 0.0

	## Source ability/skill name for logging and on-hit effect matching.
	var source_ability: StringName = &""

	## Arbitrary metadata for game-specific interceptors.
	var metadata: Dictionary = {}


## Interceptor record for internal bookkeeping.
class _Interceptor:
	var callable: Callable
	var priority: int
	var id: int


## Interceptors per phase, keyed by Phase enum value.
var _interceptors: Dictionary = {}

## Next interceptor ID for removal.
var _next_interceptor_id: int = 1


func _init() -> void:
	_interceptors[Phase.PRE_PROCESS] = []
	_interceptors[Phase.CALCULATE] = []
	_interceptors[Phase.APPLY] = []
	_interceptors[Phase.POST_PROCESS] = []


## Register an interceptor for a pipeline phase.
## [param phase] Which phase to hook into.
## [param callable] A Callable that accepts a DamageEvent. Signature: func(event: DamageEvent) -> void
## [param priority] Lower runs first. Default 0.
## Returns an interceptor ID for removal.
func add_interceptor(phase: Phase, callable: Callable, priority: int = 0) -> int:
	var interceptor := _Interceptor.new()
	interceptor.callable = callable
	interceptor.priority = priority
	interceptor.id = _next_interceptor_id
	_next_interceptor_id += 1
	_interceptors[phase].append(interceptor)
	# Keep sorted by priority.
	_interceptors[phase].sort_custom(func(a: _Interceptor, b: _Interceptor) -> bool:
		return a.priority < b.priority)
	return interceptor.id


## Remove an interceptor by its ID.
func remove_interceptor(interceptor_id: int) -> void:
	for phase_key: int in _interceptors:
		var arr: Array = _interceptors[phase_key]
		for i in range(arr.size() - 1, -1, -1):
			if arr[i].id == interceptor_id:
				arr.remove_at(i)
				return


## Remove all interceptors for a specific phase.
func clear_phase(phase: Phase) -> void:
	_interceptors[phase].clear()


## Remove all interceptors from all phases.
func clear_all() -> void:
	for phase_key: int in _interceptors:
		_interceptors[phase_key].clear()


## Process a damage event through all pipeline phases.
## Returns the completed DamageEvent for inspection.
func process(attacker_stats: StatBlock, defender_stats: StatBlock,
		base_damage: float, element: int = Element.NONE,
		damage_type: int = DamageType.PHYSICAL) -> DamageEvent:
	var event := DamageEvent.new()
	event.attacker_stats = attacker_stats
	event.defender_stats = defender_stats
	event.base_damage = base_damage
	event.mitigated_damage = base_damage
	event.element = element
	event.damage_type = damage_type

	damage_started.emit(event)

	# Run each phase in order.
	var phase_order: Array[int] = [
		Phase.PRE_PROCESS, Phase.CALCULATE, Phase.APPLY, Phase.POST_PROCESS
	]
	for phase: int in phase_order:
		if event.cancelled:
			break
		_run_phase(phase, event)

	# Check for lethal damage.
	if event.is_lethal and not event.cancelled:
		lethal_damage.emit(event)

	damage_resolved.emit(event)
	return event


## Create and process a damage event with node references attached.
func process_with_nodes(attacker_stats: StatBlock, defender_stats: StatBlock,
		base_damage: float, attacker_node: Node, defender_node: Node,
		element: int = Element.NONE,
		damage_type: int = DamageType.PHYSICAL) -> DamageEvent:
	var event := process(attacker_stats, defender_stats, base_damage, element, damage_type)
	event.attacker_node = attacker_node
	event.defender_node = defender_node
	return event


func _run_phase(phase: int, event: DamageEvent) -> void:
	var interceptor_list: Array = _interceptors[phase]
	for interceptor: _Interceptor in interceptor_list:
		if event.cancelled:
			return
		interceptor.callable.call(event)


# ---------------------------------------------------------------------------
# Built-in interceptor implementations (convenience methods).
# Games can use these directly or write custom ones.
# ---------------------------------------------------------------------------

## Standard armor reduction interceptor for PRE_PROCESS.
## Uses the formula: mitigation = armor / (armor + constant).
## RAID: Shadow Legends uses defense stat to reduce incoming damage.
static func armor_reduction(event: DamageEvent, armor_stat: StringName = &"armor",
		constant: float = 100.0) -> void:
	if event.damage_type == DamageType.TRUE:
		return  # True damage ignores armor.
	var armor: float = event.defender_stats.get_stat(armor_stat)
	var reduction: float = armor / (armor + constant)
	event.mitigated_damage = event.base_damage * (1.0 - reduction)


## Standard critical hit interceptor for CALCULATE.
## Rolls against attacker's crit_chance stat. Cookie Run: Kingdom pattern
## (BattleAbilityIncreaseCriticalDamage / BattleAbilityIncreaseCriticalDamageByStat).
static func crit_roll(event: DamageEvent, crit_chance_stat: StringName = &"crit_chance",
		crit_damage_stat: StringName = &"crit_damage") -> void:
	var crit_chance: float = event.attacker_stats.get_stat(crit_chance_stat)
	var crit_dmg: float = event.attacker_stats.get_stat(crit_damage_stat)
	if crit_dmg > 0.0:
		event.crit_multiplier = crit_dmg
	if randf() < crit_chance:
		event.is_critical = true
		event.final_damage = event.mitigated_damage * event.crit_multiplier
	else:
		event.final_damage = event.mitigated_damage


## Standard HP reduction interceptor for APPLY.
## Handles shields and death detection.
static func apply_hp(event: DamageEvent, hp_stat: StringName = &"hp",
		shield_stat: StringName = &"shield") -> void:
	var damage_to_apply := event.final_damage
	# Shield absorption first.
	var current_shield: float = event.defender_stats.get_stat(shield_stat)
	if current_shield > 0.0:
		var absorbed := minf(current_shield, damage_to_apply)
		event.shield_absorbed = absorbed
		damage_to_apply -= absorbed
		event.defender_stats.set_base(shield_stat, current_shield - absorbed)
	# HP reduction.
	var current_hp: float = event.defender_stats.get_stat(hp_stat)
	event.applied_damage = minf(current_hp, damage_to_apply)
	event.overkill = maxf(damage_to_apply - current_hp, 0.0)
	event.defender_stats.set_base(hp_stat, maxf(current_hp - damage_to_apply, 0.0))
	event.is_lethal = current_hp - damage_to_apply <= 0.0


## Standard lifesteal interceptor for POST_PROCESS.
## Archero pattern: healing on hit based on attacker's lifesteal stat.
static func apply_lifesteal(event: DamageEvent,
		lifesteal_stat: StringName = &"lifesteal",
		hp_stat: StringName = &"hp", max_hp_stat: StringName = &"max_hp") -> void:
	var lifesteal_pct: float = event.attacker_stats.get_stat(lifesteal_stat)
	if lifesteal_pct <= 0.0:
		return
	var heal := event.applied_damage * lifesteal_pct
	var current_hp: float = event.attacker_stats.get_stat(hp_stat)
	var max_hp: float = event.attacker_stats.get_stat(max_hp_stat)
	var actual_heal := minf(heal, max_hp - current_hp)
	event.attacker_stats.set_base(hp_stat, current_hp + actual_heal)
	event.lifesteal_amount = actual_heal
