class_name TosDelayEnemiesOutcome extends RefCounted
## Increases all alive enemies' countdown by the specified number of turns.
## Params: { "turns": int (default 1) }


static func execute(params: Dictionary, ctx: RefCounted, _result: RefCounted) -> void:
	var turns := int(params.get("turns", 1))
	for enemy in ctx.enemies:
		if enemy.is_alive:
			enemy.countdown += turns
