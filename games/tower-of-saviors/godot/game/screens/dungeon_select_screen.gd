extends MFBaseScreen
## Dungeon selection screen. Lists available stages.

var _game_data: Node
var _economy: MFEconomy
var _ui_router: Node
var _stage_list: VBoxContainer


func setup(game_data: Node, economy: MFEconomy, ui_router: Node) -> void:
	_game_data = game_data
	_economy = economy
	_ui_router = ui_router


func on_enter(_params: Dictionary = {}) -> void:
	_build_ui()


func _build_ui() -> void:
	# Clear existing
	for child in get_children():
		child.queue_free()

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 20
	vbox.offset_right = -20
	vbox.offset_top = 20
	add_child(vbox)

	var header = Label.new()
	header.text = "Select Dungeon"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 24)
	vbox.add_child(header)

	var spacer = Control.new()
	spacer.custom_minimum_size.y = 20
	vbox.add_child(spacer)

	# List dungeons
	var stages = _game_data.get_all_definitions(&"stages")
	for stage_def in stages:
		var btn = Button.new()
		var stamina_cost := stage_def.get_int(&"stamina_cost", 10)
		btn.text = "%s (Stamina: %d)" % [stage_def.get_string(&"name"), stamina_cost]
		btn.custom_minimum_size.y = 50
		var stage_id := stage_def.id
		btn.pressed.connect(func(): _on_stage_selected(stage_id, stamina_cost))
		vbox.add_child(btn)


func _on_stage_selected(stage_id: int, stamina_cost: int) -> void:
	if not _economy.check_stamina(stamina_cost):
		# TODO: show insufficient stamina popup
		return
	_ui_router.push(&"team_select", {"stage_id": stage_id, "stamina_cost": stamina_cost})
