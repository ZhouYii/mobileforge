class_name TosAttributeAbsorbShieldOutcome extends RefCounted
## Negates enemy element shields for the current turn.
## When active, attacks against element-shielded enemies bypass the immunity.
## Applied as a buff that the dungeon runner checks.
## Params: { "duration_turns": int (default 1) }


static func execute(params: Dictionary, _ctx: RefCounted, result: RefCounted) -> void:
	var duration := int(params.get("duration_turns", 1))
	result.buffs_applied.append({
		"type": "void_element_shield",
		"turns": duration,
	})
