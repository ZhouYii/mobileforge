class_name MFCombatTypes extends RefCounted
## Data types for the combat module. NO dependencies.


enum DamageHook {
	PRE_ELEMENT = 0,   ## Before element multiplier
	POST_ELEMENT = 1,  ## After element multiplier
	MAIN = 2,          ## Main calculation
	POST_DEFENSE = 3,  ## After defense subtraction
	CAN_ZERO = 4,      ## Final check, can force damage to 0
}


class DamageContext extends RefCounted:
	## Mutable context passed through the hook pipeline.
	var attacker_element: int
	var defender_element: int
	var base_damage: float
	var damage: float  ## Running total, modified by hooks
	var combo_count: int
	var combo_index: int  ## Which combo this is
	var gems_matched: int  ## Gems of this element matched
	var attacker_atk: float  ## Monster ATK stat
	var defender_defense: float  ## Enemy DEF
	var is_skill: bool  ## Damage from skill vs normal attack
	var extra: Dictionary  ## Arbitrary hook data

	func _init() -> void:
		attacker_element = 0
		defender_element = 0
		base_damage = 0.0
		damage = 0.0
		combo_count = 0
		combo_index = 0
		gems_matched = 0
		attacker_atk = 0.0
		defender_defense = 0.0
		is_skill = false
		extra = {}


class DamageResult extends RefCounted:
	## Immutable output from damage resolution.
	var final_damage: int
	var element_multiplier: float
	var combo_multiplier: float
	var hooks_applied: Array[String]
	var overkill: int  ## Damage beyond target HP

	func _init(p_damage: int, p_elem_mult: float, p_combo_mult: float, p_hooks: Array[String] = []) -> void:
		final_damage = p_damage
		element_multiplier = p_elem_mult
		combo_multiplier = p_combo_mult
		hooks_applied = p_hooks
		overkill = 0
