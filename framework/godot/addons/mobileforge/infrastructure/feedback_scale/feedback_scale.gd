class_name MFFeedbackScale extends RefCounted
## Maps a numeric value to a proportional feedback intensity.
## Bigger damage = more particles, deeper sounds, larger shakes.


## Linear interpolation between min_out and max_out.
static func linear(value: float, min_out: float, max_out: float, max_value: float) -> float:
	if max_value <= 0.0:
		return min_out
	var t := clampf(value / max_value, 0.0, 1.0)
	return lerpf(min_out, max_out, t)


## Logarithmic scaling — diminishing returns for large values.
static func logarithmic(value: float, min_out: float, max_out: float) -> float:
	if value <= 0.0:
		return min_out
	var t := clampf(log(value + 1.0) / log(101.0), 0.0, 1.0)  # log base normalizes to ~0-1 for 0-100
	return lerpf(min_out, max_out, t)


## Tiered: returns output for the highest threshold not exceeded.
## tiers: Array of {threshold: float, output: float}, sorted ascending by threshold.
## Example: [{threshold: 10, output: 0.3}, {threshold: 50, output: 0.6}, {threshold: 100, output: 1.0}]
static func tiered(value: float, tiers: Array) -> float:
	var result: float = 0.0
	for tier in tiers:
		if value >= tier.threshold:
			result = tier.output
		else:
			break
	return result


## Clamp an integer to a range (useful for particle count, etc.).
static func clamp_count(value: int, min_count: int, max_count: int) -> int:
	return clampi(value, min_count, max_count)
