class_name TosGravityDamageOutcome extends RefCounted
## Deals damage to all alive enemies equal to a percentage of their max HP.
## Iconic ToS mechanic — bypasses ATK entirely, scales with enemy health.
## Params: { "percent": float (default 0.3 = 30%), "target": "all"|"single" (default "all") }


static func execute(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var pct := float(params.get("percent", 0.3))
	var target: String = params.get("target", "all")

	if target == "single":
		# Target the highest HP enemy
		var best_idx := -1
		var best_hp := -1
		for i in range(ctx.enemies.size()):
			if ctx.enemies[i].is_alive and ctx.enemies[i].hp > best_hp:
				best_hp = ctx.enemies[i].hp
				best_idx = i
		if best_idx >= 0:
			result.damage_dealt[best_idx] = int(ctx.enemies[best_idx].max_hp * pct)
	else:
		for i in range(ctx.enemies.size()):
			if ctx.enemies[i].is_alive:
				result.damage_dealt[i] = int(ctx.enemies[i].max_hp * pct)
