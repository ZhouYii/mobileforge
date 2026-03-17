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
## Supports limit break: levels beyond def.max_level gain a bonus of 10% of
## (max_stat - base_stat) per 10 extra levels.
static func calculate(def: RefCounted, instance: RefCounted) -> RefCounted:  ## MonsterDef, MonsterInstance -> MonsterStats
	var curve_exp: float = CURVE_EXPONENTS.get(def.exp_curve, 1.0)
	var capped_level := mini(instance.level, def.max_level)
	var level_ratio: float = 0.0
	if def.max_level > 1:
		level_ratio = float(capped_level - 1) / float(def.max_level - 1)
	var ratio_curved: float = pow(level_ratio, curve_exp)

	var hp_base: float = def.base_hp + (def.max_hp - def.base_hp) * ratio_curved
	var atk_base: float = def.base_atk + (def.max_atk - def.base_atk) * ratio_curved
	var rec_base: float = def.base_rec + (def.max_rec - def.base_rec) * ratio_curved

	# Limit break bonus: +10% of stat range per 10 levels beyond max
	var over_levels := maxi(instance.level - def.max_level, 0)
	if over_levels > 0:
		var bonus_ratio := float(over_levels) / 10.0 * 0.1  # 10% per 10 levels
		hp_base += (def.max_hp - def.base_hp) * bonus_ratio
		atk_base += (def.max_atk - def.base_atk) * bonus_ratio
		rec_base += (def.max_rec - def.base_rec) * bonus_ratio

	var hp: int = int(hp_base) + int(instance.plus_hp)
	var atk: int = int(atk_base) + int(instance.plus_atk)
	var rec: int = int(rec_base) + int(instance.plus_rec)

	return MFMonsterTypes.MonsterStats.new(hp, atk, rec)


## Calculate experience needed for next level.
## ToS formula varies by curve, but a simple version:
## exp_needed = base_exp * level * curve_multiplier
static func exp_for_level(level: int, curve: String = "standard") -> int:
	var base := 100
	var multiplier: float = CURVE_EXPONENTS.get(curve, 1.0)
	return int(base * level * multiplier)
