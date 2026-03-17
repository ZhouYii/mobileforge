class_name MFButtonFeedback extends RefCounted
## Composable button feedback decorators.
## Attach modular press effects (scale, color, offset, sound) to any BaseButton.

var _button: BaseButton
var _decorators: Array = []  # Array of Callable(button, pressed: bool)
var _audio_manager: Object  # MFAudioManager (optional)


func _init(button: BaseButton, audio_manager: Object = null) -> void:
	_button = button
	_audio_manager = audio_manager
	_button.button_down.connect(_on_press)
	_button.button_up.connect(_on_release)


## Add a scale-bounce decorator.
func add_scale(press_scale: float = 0.9, duration: float = 0.08) -> MFButtonFeedback:
	var original_scale := _button.scale
	_decorators.append(func(btn: BaseButton, pressed: bool):
		var tween := btn.create_tween()
		if pressed:
			tween.tween_property(btn, "scale", original_scale * press_scale, duration)
		else:
			tween.tween_property(btn, "scale", original_scale, duration)
	)
	return self


## Add a color-tint decorator.
func add_color(press_color: Color = Color(0.8, 0.8, 0.8, 1.0), duration: float = 0.05) -> MFButtonFeedback:
	var original_color := _button.modulate
	_decorators.append(func(btn: BaseButton, pressed: bool):
		var tween := btn.create_tween()
		if pressed:
			tween.tween_property(btn, "modulate", press_color, duration)
		else:
			tween.tween_property(btn, "modulate", original_color, duration)
	)
	return self


## Add a position-offset decorator (e.g. button press depth).
func add_offset(press_offset: Vector2 = Vector2(0, 2), duration: float = 0.05) -> MFButtonFeedback:
	var original_pos := _button.position
	_decorators.append(func(btn: BaseButton, pressed: bool):
		var tween := btn.create_tween()
		if pressed:
			tween.tween_property(btn, "position", original_pos + press_offset, duration)
		else:
			tween.tween_property(btn, "position", original_pos, duration)
	)
	return self


## Add a sound-effect decorator.
func add_sound(sfx_id: StringName = &"btn_click") -> MFButtonFeedback:
	_decorators.append(func(_btn: BaseButton, pressed: bool):
		if pressed and _audio_manager != null:
			_audio_manager.play_sfx(sfx_id)
	)
	return self


func _on_press() -> void:
	for d in _decorators:
		d.call(_button, true)


func _on_release() -> void:
	for d in _decorators:
		d.call(_button, false)


func detach() -> void:
	if is_instance_valid(_button):
		_button.button_down.disconnect(_on_press)
		_button.button_up.disconnect(_on_release)
