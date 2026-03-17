extends MFTestBase
## Tests for team selection screen functionality

const TeamSelectScreenScript = preload("res://game/screens/team_select_screen.gd")


func test_select_monster_adds_to_team() -> void:
	var vm = _make_team_select_vm()
	
	vm.select_monster(101)
	
	assert_eq(vm.selected_ids.size(), 1, "should have 1 selected monster")
	assert_eq(vm.selected_ids[0], 101, "selected monster should be 101")


func test_cannot_exceed_max_team_size() -> void:
	var vm = _make_team_select_vm()
	
	for i in range(5):
		vm.select_monster(100 + i)
	assert_eq(vm.selected_ids.size(), 5, "should have 5 selected monsters")
	
	var result = vm.select_monster(200)
	assert_false(result, "6th selection should be rejected")
	assert_eq(vm.selected_ids.size(), 5, "should still have 5 selected monsters")


func test_cannot_add_duplicate() -> void:
	var vm = _make_team_select_vm()
	
	var result1 = vm.select_monster(101)
	assert_true(result1, "first selection should succeed")
	
	var result2 = vm.select_monster(101)
	assert_false(result2, "duplicate selection should be rejected")
	assert_eq(vm.selected_ids.size(), 1, "should still have 1 selected monster")


func test_remove_slot_removes_monster() -> void:
	var vm = _make_team_select_vm()
	
	vm.select_monster(101)
	vm.select_monster(102)
	vm.select_monster(103)
	assert_eq(vm.selected_ids.size(), 3, "should have 3 selected monsters")
	
	vm.remove_slot(1)
	
	assert_eq(vm.selected_ids.size(), 2, "should have 2 selected monsters after removal")
	assert_eq(vm.selected_ids[0], 101, "slot 0 should still be 101")
	assert_eq(vm.selected_ids[1], 103, "slot 1 should now be 103 (shifted)")


func test_start_battle_passes_team_ids() -> void:
	var vm = _make_team_select_vm()
	
	vm.select_monster(101)
	vm.select_monster(102)
	vm.select_monster(103)
	
	var params = vm.get_battle_params()
	
	assert_eq(params.team_ids.size(), 3, "battle params should have 3 team IDs")
	assert_eq(params.team_ids[0], 101, "first team ID should be 101")
	assert_eq(params.team_ids[1], 102, "second team ID should be 102")
	assert_eq(params.team_ids[2], 103, "third team ID should be 103")


func test_clear_resets_selection() -> void:
	var vm = _make_team_select_vm()
	
	vm.select_monster(101)
	vm.select_monster(102)
	
	vm.clear_selection()
	
	assert_eq(vm.selected_ids.size(), 0, "selection should be cleared")


func test_reorder_monster() -> void:
	var vm = _make_team_select_vm()
	
	vm.select_monster(101)
	vm.select_monster(102)
	vm.select_monster(103)
	
	vm.reorder_monster(0, 2)
	
	assert_eq(vm.selected_ids[0], 102, "slot 0 should now be 102")
	assert_eq(vm.selected_ids[1], 103, "slot 1 should now be 103")
	assert_eq(vm.selected_ids[2], 101, "slot 2 should now be 101")


func _make_team_select_vm() -> RefCounted:
	var vm = RefCounted.new()
	vm.set("selected_ids", [])
	vm.set("max_team_size", 5)
	
	vm.set("select_monster", func(monster_id: int) -> bool:
		if vm.selected_ids.has(monster_id):
			return false
		if vm.selected_ids.size() >= vm.max_team_size:
			return false
		vm.selected_ids.append(monster_id)
		return true
	)
	
	vm.set("remove_slot", func(slot_index: int) -> void:
		if slot_index >= 0 and slot_index < vm.selected_ids.size():
			vm.selected_ids.remove_at(slot_index)
	)
	
	vm.set("clear_selection", func() -> void:
		vm.selected_ids.clear()
	)
	
	vm.set("reorder_monster", func(from_index: int, to_index: int) -> void:
		if from_index < 0 or from_index >= vm.selected_ids.size():
			return
		if to_index < 0 or to_index >= vm.selected_ids.size():
			return
		var monster_id = vm.selected_ids[from_index]
		vm.selected_ids.remove_at(from_index)
		vm.selected_ids.insert(to_index, monster_id)
	)
	
	vm.set("get_battle_params", func() -> Dictionary:
		return {"team_ids": vm.selected_ids.duplicate()}
	)
	
	return vm
