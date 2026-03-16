class_name MFEnemyCharacteristics extends RefCounted
## Special enemy behaviors from ToS.
## Each characteristic is a static check function.


## OnlyLv2CanDamage: enemy can only be damaged if attacker combo count >= 2
static func only_combo_can_damage(combo_count: int, required: int = 2) -> bool:
	return combo_count >= required


## ComboShield: enemy takes 0 damage unless combo count >= threshold
static func combo_shield(combo_count: int, threshold: int) -> bool:
	return combo_count >= threshold


## ElementShield: enemy is immune to certain elements
static func element_shield(attacker_element: int, immune_elements: Array[int]) -> bool:
	return attacker_element not in immune_elements


## DamageAbsorb: enemy absorbs damage of certain elements (heals instead)
static func damage_absorb(attacker_element: int, absorb_elements: Array[int]) -> bool:
	return attacker_element in absorb_elements


## DamageReduction: fixed percentage damage reduction
static func apply_damage_reduction(damage: float, reduction_percent: float) -> float:
	return damage * (1.0 - clampf(reduction_percent, 0.0, 1.0))


## DamageCap: maximum damage per hit
static func apply_damage_cap(damage: float, cap: float) -> float:
	return minf(damage, cap)


## Resolve: enemy takes full damage, no modifications
static func resolve_hit(damage: int) -> int:
	return damage
