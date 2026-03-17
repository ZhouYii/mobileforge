class_name TosChangeGemElementOutcome extends RefCounted
## Converts all gems of one element to another on the board.
## Params: { "from": int (element ID), "to": int (element ID) }


static func execute(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var from_elem := int(params.get("from", 0))
	var to_elem := int(params.get("to", 0))
	if ctx.board == null:
		return
	for pos in range(ctx.board.config.total_cells()):
		var gem = ctx.board.get_gem(pos)
		if gem != null and gem.element == from_elem:
			ctx.board.change_gem_element(pos, to_elem)
			result.board_changes.append({"pos": pos, "old_element": from_elem, "new_element": to_elem})
