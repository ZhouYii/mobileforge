class_name MFStatCalculator extends RefCounted
## Calculates monster stats at a given level.
## Formula: stat = base + (max - base) * ((level - 1) / (max_level - 1)) ^ curve_exp
## Curve exponents: standard=1.0, slow=1.5, fast=0.7, super_slow=2.0


const CURVE_EXPONENTS := {
	"standard": 1.0,
	"slow": 1.5,
	"fast": 0.7,
	"super_slow": 2.0,
}


## Calculate the stats for a monster instance given its definition.
static func calculate(def: RefCounted, instance: RefCounted) -> RefCounted:  ## MonsterDef, MonsterInstance -> MonsterStats
	var curve_exp: float = CURVE_EXPONENTS.get(def.exp_curve, 1.0)
	var level_ratio: float = 0.0
	if def.max_level > 1:
		level_ratio = float(instance.level - 1) / float(def.max_level - 1)
	var ratio_curved: float = pow(level_ratio, curve_exp)

	var hp := int(def.base_hp + (def.max_hp - def.base_hp) * ratio_curved) + instance.plus_hp
	var atk := int(def.base_atk + (def.max_atk - def.base_atk) * ratio_curved) + instance.plus_atk
	var rec := int(def.base_rec + (def.max_rec - def.base_rec) * ratio_curved) + instance.plus_rec

	return MFMonsterTypes.MonsterStats.new(hp, atk, rec)


## Calculate experience needed for next level.
## ToS formula varies by curve, but a simple version:
## exp_needed = base_exp * level * curve_multiplier
static func exp_for_level(level: int, curve: String = "standard") -> int:
	var base := 100
	var multiplier: float = CURVE_EXPONENTS.get(curve, 1.0)
	return int(base * level * multiplier)
