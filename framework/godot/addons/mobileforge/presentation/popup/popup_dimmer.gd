class_name MFPopupDimmer extends ColorRect
## Semi-transparent background blocker for popups.

func _init() -> void:
	color = Color(0, 0, 0, 0.5)
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP  # Block clicks through
