class_name MFConfirmDialog extends MFBasePopup
## Builder-pattern confirmation dialog.
## Fluent API: set_title().set_message().add_button(...).show_via(popup_stack)
## Inspired by ToS DialogBuilder — simple, chainable, animated.

var _panel: PanelContainer
var _vbox: VBoxContainer
var _title_label: Label
var _message_label: Label
var _button_row: HBoxContainer
var _buttons: Array = []  # Array of {text, callback, style, node}

var _anim_duration: float = 0.25


func _init() -> void:
	set_anchors_preset(PRESET_CENTER)

	_panel = PanelContainer.new()
	var style := StyleBoxFlat.new()
	style.bg_color = Color(0.12, 0.12, 0.16, 0.95)
	style.corner_radius_top_left = 8
	style.corner_radius_top_right = 8
	style.corner_radius_bottom_left = 8
	style.corner_radius_bottom_right = 8
	style.border_width_bottom = 2
	style.border_width_top = 2
	style.border_width_left = 1
	style.border_width_right = 1
	style.border_color = Color(0.35, 0.35, 0.4)
	style.content_margin_left = 24
	style.content_margin_right = 24
	style.content_margin_top = 20
	style.content_margin_bottom = 20
	_panel.add_theme_stylebox_override("panel", style)
	add_child(_panel)

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 12)
	_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_panel.add_child(_vbox)

	_title_label = Label.new()
	_title_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_title_label.add_theme_font_size_override("font_size", 22)
	_title_label.visible = false
	_vbox.add_child(_title_label)

	_message_label = Label.new()
	_message_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_message_label.add_theme_font_size_override("font_size", 14)
	_message_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_message_label.custom_minimum_size.x = 280
	_message_label.visible = false
	_vbox.add_child(_message_label)

	_button_row = HBoxContainer.new()
	_button_row.alignment = BoxContainer.ALIGNMENT_CENTER
	_button_row.add_theme_constant_override("separation", 12)
	_vbox.add_child(_button_row)


## Builder: set dialog title
func set_title(text: String) -> MFConfirmDialog:
	_title_label.text = text
	_title_label.visible = true
	return self


## Builder: set dialog message body
func set_message(text: String) -> MFConfirmDialog:
	_message_label.text = text
	_message_label.visible = true
	return self


## Builder: add a button with callback
## style: "normal", "primary", "danger"
func add_button(text: String, callback: Callable = Callable(), style: String = "normal", min_width: float = 100.0) -> MFConfirmDialog:
	var btn := Button.new()
	btn.text = text
	btn.custom_minimum_size = Vector2(min_width, 40)
	btn.add_theme_font_size_override("font_size", 14)

	# Style the button
	var btn_style := StyleBoxFlat.new()
	btn_style.corner_radius_top_left = 6
	btn_style.corner_radius_top_right = 6
	btn_style.corner_radius_bottom_left = 6
	btn_style.corner_radius_bottom_right = 6
	btn_style.content_margin_left = 16
	btn_style.content_margin_right = 16
	btn_style.content_margin_top = 8
	btn_style.content_margin_bottom = 8
	match style:
		"primary":
			btn_style.bg_color = Color(0.2, 0.5, 0.9)
			btn_style.border_color = Color(0.3, 0.6, 1.0)
		"danger":
			btn_style.bg_color = Color(0.8, 0.2, 0.2)
			btn_style.border_color = Color(1.0, 0.3, 0.3)
		_:
			btn_style.bg_color = Color(0.25, 0.25, 0.3)
			btn_style.border_color = Color(0.4, 0.4, 0.45)
	btn_style.border_width_bottom = 2
	btn.add_theme_stylebox_override("normal", btn_style)

	btn.pressed.connect(func():
		if callback.is_valid():
			callback.call()
		dismiss(text))

	_button_row.add_child(btn)
	_buttons.append({"text": text, "callback": callback, "style": style, "node": btn})
	return self


## Show via a popup stack
func show_via(popup_stack: Node, popup_id: StringName = &"confirm", priority: int = 10) -> void:
	popup_stack.show(popup_id, func(_p): return self, {}, priority)


## Animated entrance
func on_show(params: Dictionary = {}) -> void:
	MFUIAnim.pop_in(self, _anim_duration)
