extends MFBaseScreen
## Team selection before entering a dungeon.
## Shows player's monster collection in a scrollable grid using TosCard.
## 5 team slots displayed at top (TosCard instances, empty = dashed border + "+").
## Tap monster -> assign to next empty slot. Tap filled slot -> remove.
## Team stats summary row (HP/ATK/REC). Helper section with "Friend" badge.
## "Enter Dungeon" button element-colored with MFButtonFeedback.

const MAX_TEAM_SIZE := 5

var _monster_manager: MFMonsterManager
var _player_state: Node
var _ui_router: Node
var _params: Dictionary

var _selected_ids: Array[int] = []  # Monster IDs in team slots
var _start_btn: Button
var _start_feedback: MFButtonFeedback
var _slot_container: HBoxContainer
var _slot_cards: Array = []  # Array[TosCard]
var _grid: GridContainer
var _stats_label: Label

# Helper selection
var _helper_provider: MFHelperProvider
var _available_helpers: Array = []  # Array[HelperEntry]
var _selected_helper_id: int = -1
var _helper_container: HBoxContainer
var _helper_cards: Array = []  # Array[TosCard]


func setup(monster_manager: MFMonsterManager, player_state: Node, ui_router: Node, params: Dictionary, helper_provider: MFHelperProvider = null) -> void:
	_monster_manager = monster_manager
	_player_state = player_state
	_ui_router = ui_router
	_params = params
	_helper_provider = helper_provider
	if _helper_provider != null:
		_available_helpers = _helper_provider.get_available_helpers()
	else:
		_available_helpers = []


func on_enter(params: Dictionary = {}) -> void:
	_build_ui()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()
	_slot_cards.clear()
	_helper_cards.clear()

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 12
	vbox.offset_right = -12
	vbox.offset_top = 12
	vbox.add_theme_constant_override("separation", 8)
	add_child(vbox)

	# Header with dark panel
	var header_panel = PanelContainer.new()
	header_panel.add_theme_stylebox_override("panel", TosTheme.make_panel(Color(0.3, 0.3, 0.4)))
	vbox.add_child(header_panel)
	var header = Label.new()
	header.text = "Select Team \u2014 Stage %d" % _params.get("stage_id", 0)
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 22)
	header.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))
	header.add_theme_constant_override("shadow_offset_x", 1)
	header.add_theme_constant_override("shadow_offset_y", 1)
	header_panel.add_child(header)

	# Team slots (5 TosCard instances)
	_slot_container = HBoxContainer.new()
	_slot_container.alignment = BoxContainer.ALIGNMENT_CENTER
	_slot_container.add_theme_constant_override("separation", 6)
	vbox.add_child(_slot_container)

	for i in range(MAX_TEAM_SIZE):
		var card := TosCard.new()
		card.set_empty("+")
		card.card_pressed.connect(_on_slot_card_pressed.bind(i))
		_slot_container.add_child(card)
		_slot_cards.append(card)

	# Team stats summary
	_stats_label = Label.new()
	_stats_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_stats_label.add_theme_font_size_override("font_size", 12)
	_stats_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.8))
	vbox.add_child(_stats_label)
	_refresh_stats()

	# Separator
	var sep = HSeparator.new()
	vbox.add_child(sep)

	# Friend helper selection
	if not _available_helpers.is_empty():
		var helper_panel = PanelContainer.new()
		helper_panel.add_theme_stylebox_override("panel", TosTheme.make_panel(Color(0.2, 0.4, 0.6)))
		vbox.add_child(helper_panel)

		var helper_vbox = VBoxContainer.new()
		helper_vbox.add_theme_constant_override("separation", 6)
		helper_panel.add_child(helper_vbox)

		var helper_label = Label.new()
		helper_label.text = "Choose Friend Helper"
		helper_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		helper_label.add_theme_font_size_override("font_size", 16)
		helper_vbox.add_child(helper_label)

		_helper_container = HBoxContainer.new()
		_helper_container.alignment = BoxContainer.ALIGNMENT_CENTER
		_helper_container.add_theme_constant_override("separation", 6)
		helper_vbox.add_child(_helper_container)
		_build_helper_cards()

		var sep2 = HSeparator.new()
		vbox.add_child(sep2)

	# Monster collection label
	var coll_label = Label.new()
	coll_label.text = "Your Monsters"
	coll_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	coll_label.add_theme_font_size_override("font_size", 16)
	vbox.add_child(coll_label)

	# Scrollable monster grid
	var scroll = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.custom_minimum_size.y = 300
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	vbox.add_child(scroll)

	_grid = GridContainer.new()
	_grid.columns = 5
	_grid.add_theme_constant_override("h_separation", 6)
	_grid.add_theme_constant_override("v_separation", 6)
	scroll.add_child(_grid)

	_populate_monster_grid()

	# Stagger-animate the grid
	var grid_cards: Array = []
	for child in _grid.get_children():
		if child is Control:
			grid_cards.append(child)
	if not grid_cards.is_empty():
		MFUIAnim.stagger_fade_in(grid_cards, 0.15, 0.03)

	# Buttons row
	var btn_row = HBoxContainer.new()
	btn_row.alignment = BoxContainer.ALIGNMENT_CENTER
	btn_row.add_theme_constant_override("separation", 16)
	vbox.add_child(btn_row)

	_start_btn = Button.new()
	_start_btn.text = "Enter Dungeon"
	_start_btn.custom_minimum_size = Vector2(160, 48)
	_start_btn.disabled = true
	_start_btn.pressed.connect(_on_start_battle)
	# Element-colored enter button (use first team member's element or default)
	var btn_style := TosTheme.make_button_style(0)
	_start_btn.add_theme_stylebox_override("normal", btn_style)
	_start_btn.add_theme_font_size_override("font_size", 16)
	_start_feedback = MFButtonFeedback.new(_start_btn)
	_start_feedback.add_scale(0.93)
	btn_row.add_child(_start_btn)

	var back_btn = Button.new()
	back_btn.text = "Back"
	back_btn.custom_minimum_size = Vector2(80, 48)
	back_btn.pressed.connect(func(): _ui_router.pop())
	back_btn.add_theme_stylebox_override("normal", TosTheme.make_panel(Color(0.4, 0.3, 0.3)))
	btn_row.add_child(back_btn)


func _populate_monster_grid() -> void:
	for child in _grid.get_children():
		child.queue_free()

	# Get player's owned monsters
	var owned_section = _player_state.get_section(&"monsters")
	var owned_keys: Array = owned_section.keys() if owned_section != null else []
	if owned_keys.is_empty():
		var lbl = Label.new()
		lbl.text = "No monsters owned.\nPull from Gacha first!"
		_grid.add_child(lbl)
		return

	for key in owned_keys:
		var data = owned_section.get_value(StringName(str(key)))
		if data == null or not data is Dictionary:
			continue
		var mid := int(data.get("def_id", -1))
		if mid < 0:
			continue
		var def = _monster_manager.get_def(mid)
		if def == null:
			continue

		var card := TosCard.new()
		card.bind_def(def, mid)
		card.card_pressed.connect(_on_monster_pressed)

		# Dim if already in team
		if mid in _selected_ids:
			card.set_dimmed(true)

		_grid.add_child(card)


func _on_monster_pressed(monster_id: int) -> void:
	# Don't add duplicates
	if monster_id in _selected_ids:
		return
	if _selected_ids.size() >= MAX_TEAM_SIZE:
		return

	_selected_ids.append(monster_id)
	_refresh_slots()
	_refresh_grid_dimming()


func _on_slot_card_pressed(_monster_id: int, slot_index: int) -> void:
	if slot_index < _selected_ids.size():
		_selected_ids.remove_at(slot_index)
		_refresh_slots()
		_refresh_grid_dimming()


func _refresh_slots() -> void:
	for i in range(MAX_TEAM_SIZE):
		var card: TosCard = _slot_cards[i]
		if i < _selected_ids.size():
			var mid: int = _selected_ids[i]
			var def = _monster_manager.get_def(mid)
			if def != null:
				card.bind_def(def, mid)
			else:
				card.set_empty("?")
		else:
			card.set_empty("+")

	_start_btn.disabled = _selected_ids.is_empty()

	# Update enter button color based on first team member's element
	if not _selected_ids.is_empty():
		var first_def = _monster_manager.get_def(_selected_ids[0])
		if first_def != null:
			var btn_style := TosTheme.make_button_style(first_def.element)
			_start_btn.add_theme_stylebox_override("normal", btn_style)

	_refresh_stats()


func _refresh_stats() -> void:
	if _selected_ids.is_empty():
		_stats_label.text = ""
		return

	var total_hp := 0
	var total_atk := 0
	var total_rec := 0
	# Look up real player-owned instances for accurate stats
	var section = _player_state.get_section(&"monsters")
	var owned_data: Dictionary = section.to_dict() if section != null else {}
	for mid in _selected_ids:
		var inst = _find_owned_instance(owned_data, mid)
		if inst == null:
			inst = _monster_manager.create_instance(mid)
		if inst != null:
			var stats = _monster_manager.get_stats(inst)
			if stats != null:
				total_hp += stats.hp
				total_atk += stats.atk
				total_rec += stats.rec

	_stats_label.text = "HP: %d  |  ATK: %d  |  REC: %d" % [total_hp, total_atk, total_rec]


func _find_owned_instance(owned_data: Dictionary, def_id: int) -> RefCounted:
	for key in owned_data:
		var data = owned_data[key]
		if data is Dictionary and int(data.get("def_id", -1)) == def_id:
			return MFMonsterTypes.MonsterInstance.from_dict(data)
	return null


func _refresh_grid_dimming() -> void:
	for child in _grid.get_children():
		if child is TosCard:
			var card: TosCard = child
			card.set_dimmed(card._monster_id in _selected_ids)


func _on_start_battle() -> void:
	var battle_params := Dictionary(_params)
	battle_params["team_ids"] = _selected_ids.duplicate()
	if _selected_helper_id >= 0:
		battle_params["helper_id"] = _selected_helper_id
	_ui_router.navigate(&"battle", battle_params)


func get_selected_team() -> Array[int]:
	return _selected_ids.duplicate()


func _build_helper_cards() -> void:
	_helper_cards.clear()
	if _helper_container == null:
		return
	for child in _helper_container.get_children():
		child.queue_free()

	for entry in _available_helpers:
		var card := TosCard.new()
		card.bind_def(entry.monster_def, entry.monster_id)
		card.card_pressed.connect(_on_helper_pressed)
		_helper_container.add_child(card)
		_helper_cards.append(card)

	_refresh_helper_selection()


func _on_helper_pressed(monster_id: int) -> void:
	if _selected_helper_id == monster_id:
		_selected_helper_id = -1  # Deselect
	else:
		_selected_helper_id = monster_id
	_refresh_helper_selection()


func _refresh_helper_selection() -> void:
	for card in _helper_cards:
		if card is TosCard:
			if card._monster_id == _selected_helper_id:
				card.set_selected(true)
				card.set_dimmed(false)
			else:
				card.set_selected(false)
				card.set_dimmed(true)
