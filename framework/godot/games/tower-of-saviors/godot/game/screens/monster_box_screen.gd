extends MFBaseScreen
## Monster collection/inventory screen. Shows all owned monsters in a grid.
## Supports detail view, fusion (level up), and evolution.

var _monster_manager: MFMonsterManager
var _player_state: Node
var _economy: MFEconomy
var _ui_router: Node
var _grid: GridContainer
var _detail_panel: VBoxContainer
var _monsters: Array = []  # Array of MonsterInstance
var _selected_instance: RefCounted = null  # Currently selected monster
var _fusion_mode: bool = false  # True when selecting a fodder monster
var _plus_mode: bool = false  # True when selecting a +stat fodder
var _skill_mode: bool = false  # True when selecting a skill-up fodder
var _inherit_mode: bool = false  # True when selecting a skill inheritance donor


func setup(monster_manager: MFMonsterManager, player_state: Node, ui_router: Node, economy: MFEconomy = null) -> void:
	_monster_manager = monster_manager
	_player_state = player_state
	_ui_router = ui_router
	_economy = economy


func on_enter(_params: Dictionary = {}) -> void:
	_monsters = _load_owned_monsters()
	_build_ui()


func _load_owned_monsters() -> Array:
	var monsters: Array = []
	var section = _player_state.get_section(&"monsters")
	if section == null:
		return monsters
	var data: Dictionary = section.to_dict()
	for key in data:
		var entry = data[key]
		if entry is Dictionary:
			var inst = MFMonsterTypes.MonsterInstance.from_dict(entry)
			monsters.append(inst)
	return monsters


func _save_monster(instance: RefCounted) -> void:
	var section = _player_state.get_section(&"monsters")
	if section != null:
		section.set_value(StringName(str(instance.instance_id)), instance.to_dict())


func _remove_monster(instance: RefCounted) -> void:
	var section = _player_state.get_section(&"monsters")
	if section != null:
		section.erase(StringName(str(instance.instance_id)))


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	var main = VBoxContainer.new()
	main.set_anchors_preset(Control.PRESET_FULL_RECT)
	main.offset_left = 10
	main.offset_right = -10
	main.offset_top = 10
	add_child(main)

	# Header
	var header = HBoxContainer.new()
	main.add_child(header)

	var title = Label.new()
	title.text = "Monster Box (%d)" % _monsters.size()
	title.add_theme_font_size_override("font_size", 24)
	header.add_child(title)

	header.add_child(Control.new())

	var back_btn = Button.new()
	back_btn.text = "Back"
	back_btn.pressed.connect(func(): _ui_router.pop())
	header.add_child(back_btn)

	# Monster grid
	var scroll = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	main.add_child(scroll)

	_grid = GridContainer.new()
	_grid.columns = 5
	scroll.add_child(_grid)

	for inst in _monsters:
		var card = _create_monster_card(inst)
		_grid.add_child(card)

	# Detail panel (bottom)
	_detail_panel = VBoxContainer.new()
	_detail_panel.custom_minimum_size.y = 150
	main.add_child(_detail_panel)


func _create_monster_card(instance: RefCounted) -> Control:
	var def = _monster_manager.get_def(instance.def_id)
	var stats = _monster_manager.get_stats(instance)

	var panel = PanelContainer.new()
	panel.custom_minimum_size = Vector2(65, 85)

	var vbox = VBoxContainer.new()
	panel.add_child(vbox)

	if def != null:
		var name_lbl = Label.new()
		name_lbl.text = def.name
		name_lbl.add_theme_font_size_override("font_size", 10)
		name_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		vbox.add_child(name_lbl)

		var lv_lbl = Label.new()
		var plus_total := _monster_manager.get_plus_total(instance)
		if plus_total > 0:
			lv_lbl.text = "Lv.%d +%d" % [instance.level, plus_total]
		else:
			lv_lbl.text = "Lv.%d" % instance.level
		lv_lbl.add_theme_font_size_override("font_size", 9)
		lv_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		vbox.add_child(lv_lbl)
	else:
		var id_lbl = Label.new()
		id_lbl.text = "#%d" % instance.def_id
		id_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		vbox.add_child(id_lbl)

	# Make clickable
	var btn = Button.new()
	btn.custom_minimum_size = Vector2(65, 85)
	btn.text = ""
	btn.modulate.a = 0.01  # Nearly invisible overlay
	btn.pressed.connect(func(): _on_card_clicked(instance))

	var container = Control.new()
	container.custom_minimum_size = Vector2(65, 85)
	container.add_child(panel)
	container.add_child(btn)

	return container


func _on_card_clicked(instance: RefCounted) -> void:
	if (_fusion_mode or _plus_mode or _skill_mode or _inherit_mode) and _selected_instance != null:
		# In selection mode — this card is the fodder/donor
		if instance.instance_id == _selected_instance.instance_id:
			return  # Can't use self
		if _inherit_mode:
			_do_inherit(_selected_instance, instance)
		elif _skill_mode:
			_do_skill_fuse(_selected_instance, instance)
		elif _plus_mode:
			_do_plus_fuse(_selected_instance, instance)
		else:
			_do_fusion(_selected_instance, instance)
		return

	# Normal mode — show detail
	_selected_instance = instance
	var def = _monster_manager.get_def(instance.def_id)
	var stats = _monster_manager.get_stats(instance)
	_show_detail(instance, def, stats)


func _show_detail(instance: RefCounted, def: RefCounted, stats: RefCounted) -> void:
	for child in _detail_panel.get_children():
		child.queue_free()
	_fusion_mode = false
	_plus_mode = false
	_skill_mode = false
	_inherit_mode = false

	if def == null:
		return

	var name_label = Label.new()
	name_label.text = "%s  Lv.%d" % [def.name, instance.level]
	name_label.add_theme_font_size_override("font_size", 18)
	_detail_panel.add_child(name_label)

	if stats != null:
		var stats_label = Label.new()
		stats_label.text = "HP: %d  ATK: %d  REC: %d" % [stats.hp, stats.atk, stats.rec]
		_detail_panel.add_child(stats_label)

	var plus_total := _monster_manager.get_plus_total(instance)
	var info_parts: Array = ["Element: %s" % _element_name(def.element), "Rarity: %d★" % def.rarity, "Cost: %d" % def.cost]
	if plus_total > 0:
		info_parts.append("+%d (+%d/+%d/+%d)" % [plus_total, instance.plus_hp, instance.plus_atk, instance.plus_rec])
	var info = Label.new()
	info.text = "  ".join(info_parts)
	_detail_panel.add_child(info)

	# Skill info
	if def.active_skill_id >= 0:
		var skill_text := "Skill Lv.%d" % instance.skill_level
		if instance.inherited_skill_id >= 0:
			skill_text += "  [Inherited: Skill #%d]" % instance.inherited_skill_id
		var skill_info = Label.new()
		skill_info.text = skill_text
		skill_info.add_theme_font_size_override("font_size", 11)
		_detail_panel.add_child(skill_info)

	# Awakening display
	if not def.awakening_slots.is_empty():
		var awaken_parts: Array = []
		for i in range(def.awakening_slots.size()):
			if i < instance.awakenings.size():
				awaken_parts.append(MFMonsterManager.awakening_name(instance.awakenings[i]))
			else:
				awaken_parts.append("?")
		var awaken_lbl = Label.new()
		awaken_lbl.text = "Awakenings: [%s]" % " | ".join(awaken_parts)
		awaken_lbl.add_theme_font_size_override("font_size", 10)
		_detail_panel.add_child(awaken_lbl)

	# Action buttons row
	var btn_row = HBoxContainer.new()
	btn_row.add_theme_constant_override("separation", 8)
	_detail_panel.add_child(btn_row)

	# Fuse (Level Up) button
	var fuse_btn = Button.new()
	fuse_btn.text = "Fuse (Level Up)"
	fuse_btn.custom_minimum_size = Vector2(110, 36)
	fuse_btn.pressed.connect(func(): _enter_fusion_mode(instance))
	btn_row.add_child(fuse_btn)

	# Evolve button
	if _monster_manager.can_evolve(instance):
		var evolve_btn = Button.new()
		evolve_btn.text = "Evolve!"
		evolve_btn.custom_minimum_size = Vector2(80, 36)
		evolve_btn.pressed.connect(func(): _do_evolve(instance))
		btn_row.add_child(evolve_btn)
	elif def.evolve_to >= 0:
		var evolve_hint = Label.new()
		evolve_hint.text = "Evolve at Lv.%d" % def.max_level
		evolve_hint.add_theme_font_size_override("font_size", 11)
		btn_row.add_child(evolve_hint)

	# Limit break button
	if _monster_manager.can_limit_break(instance):
		var lb_btn = Button.new()
		var effective_max: int = instance.get_effective_max_level(def.max_level)
		lb_btn.text = "Limit Break (%d→%d)" % [effective_max, effective_max + 10]
		lb_btn.custom_minimum_size = Vector2(130, 36)
		lb_btn.pressed.connect(func(): _do_limit_break(instance))
		btn_row.add_child(lb_btn)

	# Plus stat button (fuse a monster to add +stats)
	var plus_btn = Button.new()
	if plus_total < 297:
		plus_btn.text = "+Stat (%d/297)" % plus_total
		plus_btn.custom_minimum_size = Vector2(110, 36)
		plus_btn.pressed.connect(func(): _enter_plus_mode(instance))
		btn_row.add_child(plus_btn)

	# Skill Up button
	if def.active_skill_id >= 0 and instance.skill_level < 10:
		var skill_btn = Button.new()
		skill_btn.text = "Skill Up (Lv.%d)" % instance.skill_level
		skill_btn.custom_minimum_size = Vector2(110, 36)
		skill_btn.pressed.connect(func(): _enter_skill_mode(instance))
		btn_row.add_child(skill_btn)

	# Awaken button
	if _monster_manager.can_awaken(instance):
		var awaken_btn = Button.new()
		var next_idx: int = instance.awakenings.size()
		var next_name: String = MFMonsterManager.awakening_name(def.awakening_slots[next_idx])
		awaken_btn.text = "Awaken (%s)" % next_name
		awaken_btn.custom_minimum_size = Vector2(130, 36)
		awaken_btn.pressed.connect(func(): _do_awaken(instance))
		btn_row.add_child(awaken_btn)

	# Skill inherit button
	if instance.inherited_skill_id < 0:
		var inherit_btn = Button.new()
		inherit_btn.text = "Inherit Skill"
		inherit_btn.custom_minimum_size = Vector2(100, 36)
		inherit_btn.pressed.connect(func(): _enter_inherit_mode(instance))
		btn_row.add_child(inherit_btn)
	else:
		var remove_inherit_btn = Button.new()
		remove_inherit_btn.text = "Remove Inherit"
		remove_inherit_btn.custom_minimum_size = Vector2(110, 36)
		remove_inherit_btn.pressed.connect(func():
			_monster_manager.remove_inherited_skill(instance)
			_save_monster(instance)
			_show_detail(instance, def, stats))
		btn_row.add_child(remove_inherit_btn)

	# Favorite toggle
	var fav_btn = Button.new()
	fav_btn.text = "★ Fav" if instance.is_favorite else "☆ Fav"
	fav_btn.custom_minimum_size = Vector2(60, 36)
	fav_btn.pressed.connect(func():
		instance.is_favorite = not instance.is_favorite
		_save_monster(instance)
		fav_btn.text = "★ Fav" if instance.is_favorite else "☆ Fav")
	btn_row.add_child(fav_btn)


func _enter_fusion_mode(base: RefCounted) -> void:
	_fusion_mode = true
	_selected_instance = base

	for child in _detail_panel.get_children():
		child.queue_free()

	var prompt = Label.new()
	prompt.text = "Select a monster to fuse into %s" % _monster_manager.get_def(base.def_id).name
	prompt.add_theme_font_size_override("font_size", 16)
	_detail_panel.add_child(prompt)

	var cancel_btn = Button.new()
	cancel_btn.text = "Cancel"
	cancel_btn.custom_minimum_size = Vector2(80, 36)
	cancel_btn.pressed.connect(func():
		_fusion_mode = false
		var def = _monster_manager.get_def(base.def_id)
		var stats = _monster_manager.get_stats(base)
		_show_detail(base, def, stats))
	_detail_panel.add_child(cancel_btn)


func _do_fusion(base: RefCounted, fodder: RefCounted) -> void:
	_fusion_mode = false
	var old_level: int = base.level
	var exp_gained: int = _monster_manager.fuse(base, fodder)

	# Remove fodder from inventory
	_remove_monster(fodder)
	_monsters.erase(fodder)

	# Save updated base
	_save_monster(base)

	# Show result
	for child in _detail_panel.get_children():
		child.queue_free()

	var result_text = Label.new()
	var levels_up: int = base.level - old_level
	if levels_up > 0:
		result_text.text = "Fused! +%d EXP, leveled up %d time(s) → Lv.%d" % [exp_gained, levels_up, base.level]
	else:
		result_text.text = "Fused! +%d EXP (Lv.%d)" % [exp_gained, base.level]
	result_text.add_theme_font_size_override("font_size", 14)
	_detail_panel.add_child(result_text)

	# Rebuild grid to reflect changes
	_rebuild_grid()


func _enter_plus_mode(base: RefCounted) -> void:
	_plus_mode = true
	_fusion_mode = false
	_selected_instance = base

	for child in _detail_panel.get_children():
		child.queue_free()

	var base_def = _monster_manager.get_def(base.def_id)
	var prompt = Label.new()
	prompt.text = "Select a monster to sacrifice for +stats on %s" % (base_def.name if base_def else "?")
	prompt.add_theme_font_size_override("font_size", 16)
	_detail_panel.add_child(prompt)

	var hint = Label.new()
	hint.text = "Current: +%d (+%d/+%d/+%d) — Max: +297" % [
		_monster_manager.get_plus_total(base), base.plus_hp, base.plus_atk, base.plus_rec]
	hint.add_theme_font_size_override("font_size", 12)
	_detail_panel.add_child(hint)

	var cancel_btn = Button.new()
	cancel_btn.text = "Cancel"
	cancel_btn.custom_minimum_size = Vector2(80, 36)
	cancel_btn.pressed.connect(func():
		_plus_mode = false
		var def = _monster_manager.get_def(base.def_id)
		var stats = _monster_manager.get_stats(base)
		_show_detail(base, def, stats))
	_detail_panel.add_child(cancel_btn)


func _do_plus_fuse(base: RefCounted, fodder: RefCounted) -> void:
	_plus_mode = false

	# Transfer plus stats from fodder + add base amounts
	# Each fodder contributes: its own plus stats + 1 per stat from sacrifice
	var hp_add: int = fodder.plus_hp + 1
	var atk_add: int = fodder.plus_atk + 1
	var rec_add: int = fodder.plus_rec + 1

	var added := _monster_manager.add_plus_stats(base, hp_add, atk_add, rec_add)

	# Remove fodder
	_remove_monster(fodder)
	_monsters.erase(fodder)

	# Save updated base
	_save_monster(base)

	# Show result
	for child in _detail_panel.get_children():
		child.queue_free()

	var result_text = Label.new()
	result_text.text = "+Stats added! HP+%d ATK+%d REC+%d → Total +%d" % [
		added["hp"], added["atk"], added["rec"], _monster_manager.get_plus_total(base)]
	result_text.add_theme_font_size_override("font_size", 14)
	_detail_panel.add_child(result_text)

	_rebuild_grid()


func _enter_skill_mode(base: RefCounted) -> void:
	_skill_mode = true
	_fusion_mode = false
	_plus_mode = false
	_selected_instance = base

	for child in _detail_panel.get_children():
		child.queue_free()

	var base_def = _monster_manager.get_def(base.def_id)
	var prompt = Label.new()
	prompt.text = "Select fodder to skill up %s (Skill Lv.%d)" % [
		base_def.name if base_def else "?", base.skill_level]
	prompt.add_theme_font_size_override("font_size", 16)
	_detail_panel.add_child(prompt)

	var hint = Label.new()
	hint.text = "Same skill = guaranteed. Same element = chance-based."
	hint.add_theme_font_size_override("font_size", 11)
	_detail_panel.add_child(hint)

	var cancel_btn = Button.new()
	cancel_btn.text = "Cancel"
	cancel_btn.custom_minimum_size = Vector2(80, 36)
	cancel_btn.pressed.connect(func():
		_skill_mode = false
		var def = _monster_manager.get_def(base.def_id)
		var stats = _monster_manager.get_stats(base)
		_show_detail(base, def, stats))
	_detail_panel.add_child(cancel_btn)


func _do_skill_fuse(base: RefCounted, fodder: RefCounted) -> void:
	_skill_mode = false

	var old_level: int = base.skill_level
	var success: bool = _monster_manager.level_up_skill(base, fodder)

	# Remove fodder regardless of success
	_remove_monster(fodder)
	_monsters.erase(fodder)

	# Save base
	_save_monster(base)

	for child in _detail_panel.get_children():
		child.queue_free()

	var result_text = Label.new()
	if success:
		result_text.text = "Skill Up! Lv.%d → Lv.%d" % [old_level, base.skill_level]
		result_text.add_theme_color_override("font_color", Color(0.3, 1.0, 0.3))
	else:
		result_text.text = "Skill Up failed... (Lv.%d)" % base.skill_level
		result_text.add_theme_color_override("font_color", Color(0.8, 0.4, 0.4))
	result_text.add_theme_font_size_override("font_size", 14)
	_detail_panel.add_child(result_text)

	_rebuild_grid()


func _do_awaken(instance: RefCounted) -> void:
	# Cost: 5000 coins per awakening
	var cost := 5000
	if _economy != null and not _economy.can_afford("coins", cost):
		for child in _detail_panel.get_children():
			child.queue_free()
		var fail_lbl = Label.new()
		fail_lbl.text = "Not enough coins! Need %d" % cost
		fail_lbl.add_theme_color_override("font_color", Color(0.8, 0.4, 0.4))
		_detail_panel.add_child(fail_lbl)
		return

	if _economy != null:
		_economy.spend("coins", cost)

	# Use a dummy material (awakening doesn't consume a monster, just coins)
	var dummy = _monster_manager.create_instance(101)  # Elemental Shard as placeholder
	var awakening_id := _monster_manager.awaken(instance, dummy)

	if awakening_id < 0:
		return

	_save_monster(instance)

	for child in _detail_panel.get_children():
		child.queue_free()

	var result_text = Label.new()
	result_text.text = "Awakened! Unlocked: %s" % MFMonsterManager.awakening_name(awakening_id)
	result_text.add_theme_font_size_override("font_size", 14)
	result_text.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
	_detail_panel.add_child(result_text)

	_rebuild_grid()


func _enter_inherit_mode(base: RefCounted) -> void:
	_inherit_mode = true
	_fusion_mode = false
	_plus_mode = false
	_skill_mode = false
	_selected_instance = base

	for child in _detail_panel.get_children():
		child.queue_free()

	var base_def = _monster_manager.get_def(base.def_id)
	var prompt = Label.new()
	prompt.text = "Select a donor to inherit skill onto %s" % (base_def.name if base_def else "?")
	prompt.add_theme_font_size_override("font_size", 16)
	_detail_panel.add_child(prompt)

	var hint = Label.new()
	hint.text = "Donor is consumed. Its active skill becomes the inherited skill."
	hint.add_theme_font_size_override("font_size", 11)
	_detail_panel.add_child(hint)

	var cancel_btn = Button.new()
	cancel_btn.text = "Cancel"
	cancel_btn.custom_minimum_size = Vector2(80, 36)
	cancel_btn.pressed.connect(func():
		_inherit_mode = false
		var def = _monster_manager.get_def(base.def_id)
		var stats = _monster_manager.get_stats(base)
		_show_detail(base, def, stats))
	_detail_panel.add_child(cancel_btn)


func _do_inherit(base: RefCounted, donor: RefCounted) -> void:
	_inherit_mode = false

	var inherited := _monster_manager.inherit_skill(base, donor)
	if inherited < 0:
		for child in _detail_panel.get_children():
			child.queue_free()
		var fail_lbl = Label.new()
		fail_lbl.text = "Cannot inherit: donor has no skill or already inherited"
		fail_lbl.add_theme_color_override("font_color", Color(0.8, 0.4, 0.4))
		_detail_panel.add_child(fail_lbl)
		return

	# Remove donor
	_remove_monster(donor)
	_monsters.erase(donor)

	# Save base
	_save_monster(base)

	for child in _detail_panel.get_children():
		child.queue_free()

	var result_text = Label.new()
	result_text.text = "Skill inherited! Gained Skill #%d" % inherited
	result_text.add_theme_font_size_override("font_size", 14)
	result_text.add_theme_color_override("font_color", Color(0.3, 0.8, 1.0))
	_detail_panel.add_child(result_text)

	_rebuild_grid()


func _do_limit_break(instance: RefCounted) -> void:
	var new_max := _monster_manager.apply_limit_break(instance)
	if new_max < 0:
		return

	_save_monster(instance)

	for child in _detail_panel.get_children():
		child.queue_free()

	var def = _monster_manager.get_def(instance.def_id)
	var result_text = Label.new()
	result_text.text = "Limit Break! %s max level → %d" % [def.name if def else "?", new_max]
	result_text.add_theme_font_size_override("font_size", 16)
	result_text.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
	_detail_panel.add_child(result_text)

	_rebuild_grid()


func _do_evolve(instance: RefCounted) -> void:
	var old_name := ""
	var old_def = _monster_manager.get_def(instance.def_id)
	if old_def != null:
		old_name = old_def.name

	var new_def_id := _monster_manager.evolve(instance)
	if new_def_id < 0:
		return

	# Save evolved monster
	_save_monster(instance)

	# Show result
	for child in _detail_panel.get_children():
		child.queue_free()

	var new_def = _monster_manager.get_def(new_def_id)
	var new_name: String = new_def.name if new_def != null else "#%d" % new_def_id

	var result_text = Label.new()
	result_text.text = "%s evolved into %s!" % [old_name, new_name]
	result_text.add_theme_font_size_override("font_size", 16)
	_detail_panel.add_child(result_text)

	# Rebuild grid
	_rebuild_grid()


func _rebuild_grid() -> void:
	for child in _grid.get_children():
		child.queue_free()
	for inst in _monsters:
		var card = _create_monster_card(inst)
		_grid.add_child(card)


func _element_name(elem: int) -> String:
	match elem:
		1: return "Water"
		2: return "Fire"
		3: return "Grass"
		4: return "Light"
		5: return "Dark"
		6: return "Heart"
		_: return "None"
