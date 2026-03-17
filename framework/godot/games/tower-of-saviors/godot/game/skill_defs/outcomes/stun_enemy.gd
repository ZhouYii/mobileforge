class_name TosStunEnemyOutcome extends RefCounted
## Stuns target enemies, causing them to skip their next turn when their countdown reaches 0.
## Unlike bind (which lasts N turns), stun is consumed on the next attack attempt.
## Params: { "target": "all"|"single" (default "all") }


static func execute(params: Dictionary, ctx: RefCounted, _result: RefCounted) -> void:
	var target: String = params.get("target", "all")

	if target == "single":
		for enemy in ctx.enemies:
			if enemy.is_alive and not enemy.has_status("stun"):
				enemy.add_status("stun", 99)  # Long duration; consumed on trigger
				break
	else:
		for enemy in ctx.enemies:
			if enemy.is_alive and not enemy.has_status("stun"):
				enemy.add_status("stun", 99)
