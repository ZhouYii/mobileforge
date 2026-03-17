class_name TosVoidDamageAbsorbOutcome extends RefCounted
## Negates enemy damage absorption for the current turn.
## When active, attacks that would heal an enemy with damage_absorb
## deal damage normally instead.
## Applied as a buff that the dungeon runner checks.
## Params: { "duration_turns": int (default 1) }


static func execute(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	var duration := int(params.get("duration_turns", 1))
	result.buffs_applied.append({
		"type": "void_damage_absorb",
		"turns": duration,
	})
