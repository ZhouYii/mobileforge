class_name TosBindEnemyOutcome extends RefCounted
## Prevents target enemies from acting for N turns.
## Adds a "bind" status effect. Bound enemies skip their attack turn.
## Params: { "turns": int (default 2), "target": "all"|"single" (default "all") }


static func execute(params: Dictionary, ctx: RefCounted, _result: RefCounted) -> void:
	var turns := int(params.get("turns", 2))
	var target: String = params.get("target", "all")

	if target == "single":
		# Bind the first alive enemy
		for enemy in ctx.enemies:
			if enemy.is_alive and not enemy.has_status("bind"):
				enemy.add_status("bind", turns)
				break
	else:
		# Bind all alive enemies
		for enemy in ctx.enemies:
			if enemy.is_alive and not enemy.has_status("bind"):
				enemy.add_status("bind", turns)
