class_name TosOrbSpawnOutcome extends RefCounted
## Creates gems of a specific element at random positions on the board.
## Iconic ToS mechanic — skills like "spawn 5 water orbs" replace random gems.
## Params: { "element": int, "count": int (default 3) }


static func execute(params: Dictionary, ctx: RefCounted, result: RefCounted) -> void:
	var target_element := int(params.get("element", 1))
	var count := int(params.get("count", 3))

	if ctx.board == null:
		return

	var total_cells: int = ctx.board.config.total_cells()
	if total_cells <= 0:
		return

	# Collect positions that are NOT already the target element
	var candidates: Array[int] = []
	for pos in range(total_cells):
		var gem = ctx.board.get_gem(pos)
		if gem != null and gem.element != target_element:
			candidates.append(pos)

	if candidates.is_empty():
		return

	# Shuffle and pick up to count positions
	var rng := RandomNumberGenerator.new()
	rng.randomize()
	var spawned := 0
	# Fisher-Yates shuffle on candidates
	for i in range(candidates.size() - 1, 0, -1):
		var j := rng.randi() % (i + 1)
		var tmp := candidates[i]
		candidates[i] = candidates[j]
		candidates[j] = tmp

	for i in range(mini(count, candidates.size())):
		var pos: int = candidates[i]
		var old_gem = ctx.board.get_gem(pos)
		var old_element: int = old_gem.element if old_gem != null else 0
		ctx.board.change_gem_element(pos, target_element)
		result.board_changes.append({
			"pos": pos,
			"old_element": old_element,
			"new_element": target_element,
		})
		spawned += 1
