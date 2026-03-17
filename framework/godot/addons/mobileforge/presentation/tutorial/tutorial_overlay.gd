extends CanvasLayer
class_name MFTutorialOverlay
## Presentation-side tutorial overlay.
## Shows spotlight cutout, hand pointer, and instruction text.

var _spotlight: ColorRect
var _hand: TextureRect
var _text_label: Label
var _tracker: MFTutorialTracker
var _is_active: bool = false


func _ready() -> void:
	layer = 100  # Above everything

	_spotlight = ColorRect.new()
	_spotlight.color = Color(0, 0, 0, 0.6)
	_spotlight.set_anchors_preset(Control.PRESET_FULL_RECT)
	_spotlight.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(_spotlight)

	_text_label = Label.new()
	_text_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_text_label.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_text_label.position.y = -80
	add_child(_text_label)

	visible = false


func setup(tracker: MFTutorialTracker) -> void:
	_tracker = tracker


## Show the overlay for the current tutorial step.
func show_step(step: Dictionary) -> void:
	_is_active = true
	visible = true

	_text_label.text = str(step.get("text", ""))

	# If step has a target rect, create a spotlight hole (simplified)
	if step.has("target_rect"):
		_spotlight.visible = true
	else:
		_spotlight.visible = true


## Hide the overlay.
func hide_overlay() -> void:
	_is_active = false
	visible = false


## Handle tap to advance the tutorial.
func _unhandled_input(event: InputEvent) -> void:
	if not _is_active:
		return
	if event is InputEventMouseButton and event.pressed:
		if _tracker != null:
			if not _tracker.advance():
				hide_overlay()
			else:
				show_step(_tracker.get_current_step())
		get_viewport().set_input_as_handled()
