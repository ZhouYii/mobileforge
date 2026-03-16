class_name MFLoadingOverlay extends ColorRect
## Full-screen loading spinner overlay.

var _label: Label

func _init() -> void:
	color = Color(0, 0, 0, 0.7)
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP

	_label = Label.new()
	_label.text = "Loading..."
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.set_anchors_preset(Control.PRESET_CENTER)
	add_child(_label)

func set_message(text: String) -> void:
	_label.text = text
