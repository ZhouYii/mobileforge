extends MFBaseScreen
## Dungeon selection screen. Groups stages by zone, with difficulty tabs.
## Uses MFTabBar for difficulty selection, styled stage cards with
## TosTheme.make_panel(), colored floor effect pill badges, and
## difficulty-colored enter button.

var _game_data: Node
var _economy: MFEconomy
var _ui_router: Node
var _stage_container: VBoxContainer
var _tab_bar: MFTabBar
var _selected_difficulty: String = "normal"
var _btn_feedbacks: Array = []  # Array[MFButtonFeedback] to prevent GC

const DIFFICULTY_COLORS := {
	"normal": Color(0.7, 0.7, 0.7),
	"expert": Color(0.4, 0.7, 1.0),
	"mythical": Color(1.0, 0.5, 0.2),
	"annihilation": Color(1.0, 0.2, 0.2),
}

const DIFFICULTY_LABELS := {
	"normal": "Normal",
	"expert": "Expert",
	"mythical": "Mythical",
	"annihilation": "Annihilation",
}


func setup(game_data: Node, economy: MFEconomy, ui_router: Node) -> void:
	_game_data = game_data
	_economy = economy
	_ui_router = ui_router


func on_enter(_params: Dictionary = {}) -> void:
	_build_ui()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()
	_btn_feedbacks.clear()

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 12
	vbox.offset_right = -12
	vbox.offset_top = 12
	vbox.add_theme_constant_override("separation", 8)
	add_child(vbox)

	# Header row with dark panel
	var header_panel = PanelContainer.new()
	header_panel.add_theme_stylebox_override("panel", TosTheme.make_panel(Color(0.3, 0.3, 0.4)))
	vbox.add_child(header_panel)

	var header_row = HBoxContainer.new()
	header_row.alignment = BoxContainer.ALIGNMENT_CENTER
	header_row.add_theme_constant_override("separation", 12)
	header_panel.add_child(header_row)

	var title = Label.new()
	title.text = "Select Dungeon"
	title.add_theme_font_size_override("font_size", 24)
	title.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))
	title.add_theme_constant_override("shadow_offset_x", 1)
	title.add_theme_constant_override("shadow_offset_y", 1)
	header_row.add_child(title)

	header_row.add_child(Control.new())  # spacer

	# Stamina display
	var stamina_lbl = Label.new()
	var stam := _economy.get_balance("stamina")
	var max_stam := _economy.get_max_stamina()
	stamina_lbl.text = "Stamina: %d/%d" % [stam, max_stam]
	if _economy.is_stamina_overflowed():
		stamina_lbl.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
	stamina_lbl.add_theme_font_size_override("font_size", 14)
	header_row.add_child(stamina_lbl)

	var back_btn = Button.new()
	back_btn.text = "Back"
	back_btn.pressed.connect(func(): _ui_router.pop())
	back_btn.add_theme_stylebox_override("normal", TosTheme.make_panel(Color(0.4, 0.3, 0.3)))
	header_row.add_child(back_btn)

	# Difficulty tab bar (MFTabBar)
	_tab_bar = MFTabBar.new()
	var available_diffs := _get_available_difficulties()
	var active_idx := 0
	for i in range(available_diffs.size()):
		var diff: String = available_diffs[i]
		var color: Color = DIFFICULTY_COLORS.get(diff, Color.WHITE)
		_tab_bar.add_tab(DIFFICULTY_LABELS.get(diff, diff.capitalize()), color)
		if diff == _selected_difficulty:
			active_idx = i
	_tab_bar.set_active(active_idx)
	_tab_bar.tab_selected.connect(func(index: int):
		if index < available_diffs.size():
			_selected_difficulty = available_diffs[index]
			_populate_stages())
	vbox.add_child(_tab_bar)

	# Scrollable stage list
	var scroll = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.custom_minimum_size.y = 400
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	vbox.add_child(scroll)

	_stage_container = VBoxContainer.new()
	_stage_container.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_stage_container.add_theme_constant_override("separation", 6)
	scroll.add_child(_stage_container)

	_populate_stages()


func _populate_stages() -> void:
	for child in _stage_container.get_children():
		child.queue_free()
	_btn_feedbacks.clear()

	var stages = _game_data.get_all_definitions(&"stages")
	print("[DungeonSelect] stages count: ", stages.size(), " selected_diff: ", _selected_difficulty)
	var filtered: Array = []

	for stage_def in stages:
		var diff: String = stage_def.get_string(&"difficulty", "normal")
		print("[DungeonSelect] stage id=", stage_def.id, " diff=", diff)
		if diff != _selected_difficulty:
			continue
		# Check daily availability
		var avail_days: Array = stage_def.get_array(&"available_days", [])
		if not avail_days.is_empty():
			var today_weekday: int = Time.get_date_dict_from_system()["weekday"]
			if today_weekday not in avail_days:
				continue
		filtered.append(stage_def)

	print("[DungeonSelect] filtered count: ", filtered.size())

	if filtered.is_empty():
		var empty_lbl = Label.new()
		empty_lbl.text = "No %s stages available" % DIFFICULTY_LABELS.get(_selected_difficulty, _selected_difficulty)
		empty_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		empty_lbl.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
		_stage_container.add_child(empty_lbl)
		return

	# Sort by id
	filtered.sort_custom(func(a, b): return a.id < b.id)

	var diff_color: Color = DIFFICULTY_COLORS.get(_selected_difficulty, Color.WHITE)

	for stage_def in filtered:
		var stamina_cost: int = stage_def.get_int(&"stamina_cost", 10)
		var turn_limit: int = stage_def.get_int(&"turn_limit", 0)

		var panel = PanelContainer.new()
		panel.custom_minimum_size.y = 60
		# Stage card with difficulty color strip
		var card_style := TosTheme.make_panel(diff_color)
		panel.add_theme_stylebox_override("panel", card_style)
		_stage_container.add_child(panel)

		var hbox = HBoxContainer.new()
		hbox.add_theme_constant_override("separation", 8)
		panel.add_child(hbox)

		# Difficulty color strip (thin vertical bar)
		var strip = ColorRect.new()
		strip.custom_minimum_size = Vector2(4, 0)
		strip.size_flags_vertical = Control.SIZE_EXPAND_FILL
		strip.color = diff_color
		hbox.add_child(strip)

		var info_vbox = VBoxContainer.new()
		info_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		hbox.add_child(info_vbox)

		var name_lbl = Label.new()
		name_lbl.text = stage_def.get_string(&"name")
		name_lbl.add_theme_font_size_override("font_size", 16)
		name_lbl.add_theme_color_override("font_color", diff_color)
		name_lbl.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.4))
		name_lbl.add_theme_constant_override("shadow_offset_x", 1)
		name_lbl.add_theme_constant_override("shadow_offset_y", 1)
		info_vbox.add_child(name_lbl)

		var detail_parts: Array = ["Stamina: %d" % stamina_cost]
		if turn_limit > 0:
			detail_parts.append("%d turns" % turn_limit)
		var avail_days: Array = stage_def.get_array(&"available_days", [])
		if not avail_days.is_empty():
			detail_parts.append("Daily")
		var b_rows: int = stage_def.get_int(&"board_rows", 0)
		var b_cols: int = stage_def.get_int(&"board_cols", 0)
		if b_rows > 0 and b_cols > 0 and (b_rows != 5 or b_cols != 6):
			detail_parts.append("%dx%d board" % [b_rows, b_cols])
		var detail_lbl = Label.new()
		detail_lbl.text = "  ".join(detail_parts)
		detail_lbl.add_theme_font_size_override("font_size", 11)
		info_vbox.add_child(detail_lbl)

		# Show floor effects as colored pill badges
		var floor_effects: Array = stage_def.get_array(&"floor_effects", [])
		if not floor_effects.is_empty():
			var fe_hbox = HBoxContainer.new()
			fe_hbox.add_theme_constant_override("separation", 4)
			info_vbox.add_child(fe_hbox)
			for fe in floor_effects:
				var pill = PanelContainer.new()
				var pill_style := StyleBoxFlat.new()
				pill_style.bg_color = Color(1.0, 0.4, 0.2, 0.2)
				pill_style.corner_radius_top_left = 10
				pill_style.corner_radius_top_right = 10
				pill_style.corner_radius_bottom_left = 10
				pill_style.corner_radius_bottom_right = 10
				pill_style.border_width_bottom = 1
				pill_style.border_color = Color(1.0, 0.5, 0.3, 0.5)
				pill_style.content_margin_left = 6
				pill_style.content_margin_right = 6
				pill_style.content_margin_top = 2
				pill_style.content_margin_bottom = 2
				pill.add_theme_stylebox_override("panel", pill_style)
				var fe_lbl = Label.new()
				fe_lbl.text = _floor_effect_label(str(fe))
				fe_lbl.add_theme_font_size_override("font_size", 10)
				fe_lbl.add_theme_color_override("font_color", Color(1.0, 0.6, 0.3))
				pill.add_child(fe_lbl)
				fe_hbox.add_child(pill)

		# Difficulty-colored enter button
		var enter_btn = Button.new()
		enter_btn.text = "Enter"
		enter_btn.custom_minimum_size = Vector2(80, 44)
		enter_btn.add_theme_font_size_override("font_size", 14)
		var enter_style := StyleBoxFlat.new()
		enter_style.bg_color = Color(diff_color.r * 0.3, diff_color.g * 0.3, diff_color.b * 0.3, 0.9)
		enter_style.corner_radius_top_left = 6
		enter_style.corner_radius_top_right = 6
		enter_style.corner_radius_bottom_left = 6
		enter_style.corner_radius_bottom_right = 6
		enter_style.border_width_bottom = 2
		enter_style.border_color = diff_color
		enter_style.content_margin_left = 10
		enter_style.content_margin_right = 10
		enter_style.content_margin_top = 6
		enter_style.content_margin_bottom = 6
		enter_btn.add_theme_stylebox_override("normal", enter_style)

		var sid: int = stage_def.id
		var scost: int = stamina_cost
		enter_btn.pressed.connect(func(): _on_stage_selected(sid, scost))

		# Button feedback
		var feedback := MFButtonFeedback.new(enter_btn)
		feedback.add_scale(0.93)
		_btn_feedbacks.append(feedback)

		# Disable if not enough stamina
		if not _economy.check_stamina(stamina_cost):
			enter_btn.disabled = true
			enter_btn.text = "Need %d" % stamina_cost
			enter_btn.modulate = Color(0.5, 0.5, 0.5)

		hbox.add_child(enter_btn)

	print("[DungeonSelect] added ", _stage_container.get_child_count(), " stage cards")
	print("[DungeonSelect] screen size=", size, " vbox size=", get_child(0).size if get_child_count() > 0 else "no vbox")
	print("[DungeonSelect] stage_container size=", _stage_container.size, " parent(scroll) size=", _stage_container.get_parent().size if _stage_container.get_parent() else "no parent")


func _on_stage_selected(stage_id: int, stamina_cost: int) -> void:
	if not _economy.check_stamina(stamina_cost):
		return
	_ui_router.push(&"team_select", {"stage_id": stage_id, "stamina_cost": stamina_cost})


func _get_available_difficulties() -> Array:
	var diffs: Dictionary = {}
	var stages = _game_data.get_all_definitions(&"stages")
	for stage_def in stages:
		var diff: String = stage_def.get_string(&"difficulty", "normal")
		diffs[diff] = true

	# Sort by rank order
	var result: Array = []
	for d in ["normal", "expert", "mythical", "annihilation"]:
		if diffs.has(d):
			result.append(d)
	return result


static func _floor_effect_label(effect: String) -> String:
	match effect:
		"no_active_skills": return "No Skills"
		"no_heart_heal": return "No Healing"
		"enemy_hp_regen": return "Enemy Regen"
		"poison_floor": return "Poison Floor"
		"no_skyfall_combos": return "No Skyfall"
		"fixed_move_time": return "Fixed Time"
		_:
			if effect.begins_with("element_restrict_"):
				var elem := int(effect.substr("element_restrict_".length()))
				var elem_names := {1: "Water", 2: "Fire", 3: "Earth", 4: "Light", 5: "Dark"}
				return "%s Only" % elem_names.get(elem, "?")
			return effect
