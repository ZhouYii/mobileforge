extends MFBaseScreen
## ToS Title Screen — Main menu with navigation to all game screens.

var _vbox: VBoxContainer


func _init() -> void:
	_vbox = VBoxContainer.new()
	_vbox.set_anchors_preset(Control.PRESET_CENTER)
	_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	add_child(_vbox)

	var title = Label.new()
	title.text = "Tower of Saviors"
	title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	title.add_theme_font_size_override("font_size", 32)
	_vbox.add_child(title)

	var spacer = Control.new()
	spacer.custom_minimum_size.y = 40
	_vbox.add_child(spacer)

	_add_nav_button("Dungeon Select", &"dungeon_select")
	_add_nav_button("Gacha", &"gacha")
	_add_nav_button("Monster Box", &"monster_box")
	_add_nav_button("Shop", &"shop")


func _add_nav_button(label: String, screen_name: StringName) -> void:
	var btn = Button.new()
	btn.text = label
	btn.custom_minimum_size = Vector2(200, 50)
	btn.pressed.connect(func(): _navigate_to(screen_name))
	_vbox.add_child(btn)


func _navigate_to(screen_name: StringName) -> void:
	var router = get_node_or_null("/root/TosGame")
	if router and router._ui_router:
		router._ui_router.navigate(screen_name)
