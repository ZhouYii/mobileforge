class_name MFComboCalculator extends RefCounted
## Pure math for combo multiplier calculation.
## ToS formula: combo_mult = 1 + (combo_count - 1) * 0.25
## e.g., 1 combo = 1.0x, 2 combos = 1.25x, 5 combos = 2.0x


const BASE_MULTIPLIER := 1.0
const PER_COMBO_BONUS := 0.25


## Calculate the combo multiplier for a given combo count.
static func calculate(combo_count: int) -> float:
	if combo_count <= 0:
		return 0.0
	return BASE_MULTIPLIER + (combo_count - 1) * PER_COMBO_BONUS


## Calculate base gem damage: base_damage = atk * (1 + (gems - 3) * 0.25)
## Where gems = number of gems of this element matched in this combo.
static func gem_damage(atk: float, gems_matched: int) -> float:
	if gems_matched < 1:
		return 0.0
	# Minimum 3 gems for a match
	return atk * (1.0 + maxf(gems_matched - 3, 0) * 0.25)
