extends MFBaseScreen
## Team selection before entering a dungeon.

var _monster_manager: MFMonsterManager
var _player_state: Node
var _ui_router: Node
var _params: Dictionary


func setup(monster_manager: MFMonsterManager, player_state: Node, ui_router: Node, params: Dictionary) -> void:
	_monster_manager = monster_manager
	_player_state = player_state
	_ui_router = ui_router
	_params = params


func on_enter(params: Dictionary = {}) -> void:
	_build_ui()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.offset_left = 20
	vbox.offset_right = -20
	vbox.offset_top = 20
	add_child(vbox)

	var header = Label.new()
	header.text = "Select Team"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	header.add_theme_font_size_override("font_size", 24)
	vbox.add_child(header)

	# TODO: Team slot UI with monster selection
	var info = Label.new()
	info.text = "Stage: %d\n(Team selection UI placeholder)" % _params.get("stage_id", 0)
	vbox.add_child(info)

	var start_btn = Button.new()
	start_btn.text = "Enter Dungeon"
	start_btn.custom_minimum_size.y = 50
	start_btn.pressed.connect(_on_start_battle)
	vbox.add_child(start_btn)

	var back_btn = Button.new()
	back_btn.text = "Back"
	back_btn.pressed.connect(func(): _ui_router.pop())
	vbox.add_child(back_btn)


func _on_start_battle() -> void:
	_ui_router.navigate(&"battle", _params)
