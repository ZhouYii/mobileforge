class_name MFFadeOverlay extends ColorRect
## Full-screen color fade transition overlay.

signal midpoint_reached  ## Emitted at peak opacity during transition_async

var _target_alpha: float = 0.0
var _speed: float = 2.0
var _on_complete: Callable

func _init() -> void:
	color = Color.BLACK
	modulate.a = 0.0
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func fade_in(duration: float = 0.5, on_complete: Callable = Callable()) -> void:
	_target_alpha = 1.0
	_speed = 1.0 / maxf(duration, 0.01)
	_on_complete = on_complete
	mouse_filter = Control.MOUSE_FILTER_STOP

func fade_out(duration: float = 0.5, on_complete: Callable = Callable()) -> void:
	_target_alpha = 0.0
	_speed = 1.0 / maxf(duration, 0.01)
	_on_complete = on_complete

## Full transition: fade in -> emit midpoint_reached -> fade out.
## Await this for the complete transition. Use midpoint_reached to swap content at peak.
func transition_async(duration: float = 0.5) -> void:
	var half := duration * 0.5
	# Fade to black
	var fade_done := false
	fade_in(half, func(): fade_done = true)
	while not fade_done:
		await get_tree().process_frame
	midpoint_reached.emit()
	# Fade from black
	fade_done = false
	fade_out(half, func(): fade_done = true)
	while not fade_done:
		await get_tree().process_frame

func _process(delta: float) -> void:
	var prev := modulate.a
	modulate.a = move_toward(modulate.a, _target_alpha, _speed * delta)
	if prev != modulate.a and modulate.a == _target_alpha:
		if _target_alpha == 0.0:
			mouse_filter = Control.MOUSE_FILTER_IGNORE
		if _on_complete.is_valid():
			_on_complete.call()
			_on_complete = Callable()
