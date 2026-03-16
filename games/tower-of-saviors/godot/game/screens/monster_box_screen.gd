extends MFBaseScreen
## Monster collection/inventory screen. Shows all owned monsters in a grid.

var _monster_manager: MFMonsterManager
var _player_state: Node
var _ui_router: Node
var _grid: GridContainer
var _detail_panel: VBoxContainer
var _monsters: Array = []  # Array of MonsterInstance (simulated)

func setup(monster_manager: MFMonsterManager, player_state: Node, ui_router: Node) -> void:
	_monster_manager = monster_manager
	_player_state = player_state
	_ui_router = ui_router

func on_enter(_params: Dictionary = {}) -> void:
	# For now, create some sample monsters to display
	_monsters = _create_sample_monsters()
	_build_ui()

func _create_sample_monsters() -> Array:
	var monsters: Array = []
	# Create one of each base monster (ids 1-10) at various levels
	for id in range(1, 11):
		var inst = _monster_manager.create_instance(id, (id * 10) % 99 + 1)
		monsters.append(inst)
	return monsters

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
	_detail_panel.custom_minimum_size.y = 120
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
	btn.pressed.connect(func(): _show_detail(instance, def, stats))

	var container = Control.new()
	container.custom_minimum_size = Vector2(65, 85)
	container.add_child(panel)
	container.add_child(btn)

	return container

func _show_detail(instance: RefCounted, def: RefCounted, stats: RefCounted) -> void:
	for child in _detail_panel.get_children():
		child.queue_free()

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

	var info = Label.new()
	info.text = "Element: %s  Rarity: %d  Cost: %d" % [
		_element_name(def.element), def.rarity, def.cost]
	_detail_panel.add_child(info)

	if def.evolve_to >= 0:
		var evolve_label = Label.new()
		evolve_label.text = "Can evolve to: #%d" % def.evolve_to
		_detail_panel.add_child(evolve_label)

func _element_name(elem: int) -> String:
	match elem:
		1: return "Water"
		2: return "Fire"
		3: return "Grass"
		4: return "Light"
		5: return "Dark"
		6: return "Heart"
		_: return "None"
