class_name TosAreaDamageOutcome extends RefCounted
## Deals damage to all alive enemies based on caster ATK * multiplier.
## Params: { "multiplier": float (default 1.0) }


static func execute(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var mult: float = float(params.get("multiplier", 1.0))
	var atk: float = 0.0
	if not ctx.team_stats.is_empty() and ctx.team_stats[0] != null:
		atk = ctx.team_stats[0].atk
	for i in range(ctx.enemies.size()):
		if ctx.enemies[i].is_alive:
			result.damage_dealt[i] = int(atk * mult)
