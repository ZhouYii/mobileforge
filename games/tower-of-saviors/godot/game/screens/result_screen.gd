extends MFBaseScreen
## Battle result screen — shows win/loss and rewards.

var _params: Dictionary


func setup(params: Dictionary) -> void:
	_params = params


func on_enter(params: Dictionary = {}) -> void:
	_build_ui()


func _build_ui() -> void:
	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_CENTER)
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	add_child(vbox)

	var won: bool = _params.get("won", false)

	var result_label = Label.new()
	result_label.text = "VICTORY!" if won else "DEFEATED..."
	result_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	result_label.add_theme_font_size_override("font_size", 28)
	vbox.add_child(result_label)

	if won:
		var rewards = _params.get("rewards", [])
		for reward in rewards:
			var reward_label = Label.new()
			reward_label.text = "  %s x%d" % [str(reward.get("type", "")), int(reward.get("count", 0))]
			reward_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			vbox.add_child(reward_label)

	var spacer = Control.new()
	spacer.custom_minimum_size.y = 30
	vbox.add_child(spacer)

	var continue_btn = Button.new()
	continue_btn.text = "Continue"
	continue_btn.custom_minimum_size = Vector2(200, 50)
	continue_btn.pressed.connect(func():
		var router = get_node_or_null("/root/TosGame")
		if router and router._ui_router:
			router._ui_router.navigate(&"title"))
	vbox.add_child(continue_btn)
