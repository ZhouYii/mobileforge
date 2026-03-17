class_name MFUIAnim extends RefCounted
## Reusable UI animation helpers built on Godot's Tween system.
## All methods return the Tween so callers can chain or await.
##
## Usage:
##   MFUIAnim.fade_in(my_control, 0.3)
##   MFUIAnim.slide_in_from(my_panel, Vector2(-200, 0), 0.4)
##   await MFUIAnim.pop_in(my_button, 0.2).finished


## Fade a control from current alpha to 1.0.
static func fade_in(control: Control, duration: float = 0.3, delay: float = 0.0) -> Tween:
	control.modulate.a = 0.0
	var tween := control.create_tween()
	if delay > 0.0:
		tween.tween_interval(delay)
	tween.tween_property(control, "modulate:a", 1.0, duration) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	return tween


## Fade a control from current alpha to 0.0, then optionally hide or free it.
static func fade_out(control: Control, duration: float = 0.3, free_after: bool = false) -> Tween:
	var tween := control.create_tween()
	tween.tween_property(control, "modulate:a", 0.0, duration) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	if free_after:
		tween.tween_callback(control.queue_free)
	else:
		tween.tween_callback(func(): control.visible = false)
	return tween


## Slide a control in from an offset (relative to its current position).
static func slide_in_from(control: Control, offset: Vector2, duration: float = 0.4) -> Tween:
	var target_pos := control.position
	control.position = target_pos + offset
	control.modulate.a = 0.0
	var tween := control.create_tween()
	tween.set_parallel(true)
	tween.tween_property(control, "position", target_pos, duration) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_CUBIC)
	tween.tween_property(control, "modulate:a", 1.0, duration * 0.6) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	return tween


## Slide a control out to an offset, then optionally free it.
static func slide_out_to(control: Control, offset: Vector2, duration: float = 0.4, free_after: bool = false) -> Tween:
	var target_pos := control.position + offset
	var tween := control.create_tween()
	tween.set_parallel(true)
	tween.tween_property(control, "position", target_pos, duration) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_CUBIC)
	tween.tween_property(control, "modulate:a", 0.0, duration) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	tween.set_parallel(false)
	if free_after:
		tween.tween_callback(control.queue_free)
	return tween


## Scale-bounce entrance (pop in from 0 to overshoot to 1.0).
static func pop_in(control: Control, duration: float = 0.3) -> Tween:
	control.pivot_offset = control.size / 2.0
	control.scale = Vector2.ZERO
	control.modulate.a = 1.0
	var tween := control.create_tween()
	tween.tween_property(control, "scale", Vector2(1.1, 1.1), duration * 0.7) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	tween.tween_property(control, "scale", Vector2.ONE, duration * 0.3) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_QUAD)
	return tween


## Scale down and fade out (pop out).
static func pop_out(control: Control, duration: float = 0.2, free_after: bool = false) -> Tween:
	control.pivot_offset = control.size / 2.0
	var tween := control.create_tween()
	tween.set_parallel(true)
	tween.tween_property(control, "scale", Vector2(0.8, 0.8), duration) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	tween.tween_property(control, "modulate:a", 0.0, duration) \
		.set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	tween.set_parallel(false)
	if free_after:
		tween.tween_callback(control.queue_free)
	return tween


## Shake a control (for error feedback, damage, etc.).
static func shake(control: Control, intensity: float = 8.0, duration: float = 0.4) -> Tween:
	var original_pos := control.position
	var tween := control.create_tween()
	var steps := int(duration / 0.05)
	for i in range(steps):
		var offset := Vector2(
			randf_range(-intensity, intensity),
			randf_range(-intensity, intensity)
		) * (1.0 - float(i) / float(steps))  # Decay over time
		tween.tween_property(control, "position", original_pos + offset, 0.05)
	tween.tween_property(control, "position", original_pos, 0.05)
	return tween


## Pulse scale (heartbeat effect for attention).
static func pulse(control: Control, scale_amount: float = 1.15, duration: float = 0.6) -> Tween:
	control.pivot_offset = control.size / 2.0
	var tween := control.create_tween()
	tween.tween_property(control, "scale", Vector2(scale_amount, scale_amount), duration * 0.4) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_SINE)
	tween.tween_property(control, "scale", Vector2.ONE, duration * 0.6) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	return tween


## Stagger-animate an array of controls (e.g. grid items appearing one by one).
## Returns the last tween (await it for "all done").
static func stagger_fade_in(controls: Array, per_item_duration: float = 0.2, stagger_delay: float = 0.05) -> Tween:
	var last_tween: Tween = null
	for i in range(controls.size()):
		var control: Control = controls[i]
		control.modulate.a = 0.0
		last_tween = fade_in(control, per_item_duration, stagger_delay * i)
	return last_tween


## Number counter animation (e.g. score counting up).
## Updates a Label's text from start_value to end_value over duration.
static func count_to(label: Label, start_value: int, end_value: int, duration: float = 0.5, prefix: String = "", suffix: String = "") -> Tween:
	var tween := label.create_tween()
	var value_holder := {"v": float(start_value)}
	tween.tween_method(
		func(v: float):
			label.text = "%s%d%s" % [prefix, int(v), suffix],
		float(start_value), float(end_value), duration
	).set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	return tween


## Quick scale overshoot and return (juice for button press, damage, etc.).
static func scale_punch(control: Control, intensity: float = 1.3, duration: float = 0.2) -> Tween:
	control.pivot_offset = control.size / 2.0
	var tween := control.create_tween()
	tween.tween_property(control, "scale", Vector2(intensity, intensity), duration * 0.4) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	tween.tween_property(control, "scale", Vector2.ONE, duration * 0.6) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_ELASTIC)
	return tween


## Drop from above and bounce on landing.
static func bounce_in(control: Control, drop_distance: float = 50.0, duration: float = 0.4) -> Tween:
	var target_pos := control.position
	control.position = target_pos - Vector2(0, drop_distance)
	control.modulate.a = 0.0
	var tween := control.create_tween()
	tween.set_parallel(true)
	tween.tween_property(control, "position", target_pos, duration) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BOUNCE)
	tween.tween_property(control, "modulate:a", 1.0, duration * 0.3) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_QUAD)
	return tween


## Rotation wobble (like a bell being struck).
static func wobble(control: Control, degrees: float = 5.0, duration: float = 0.4) -> Tween:
	control.pivot_offset = control.size / 2.0
	var tween := control.create_tween()
	var steps := 4
	var step_dur := duration / (steps + 1)
	for i in range(steps):
		var angle := degrees * (1.0 - float(i) / float(steps)) * (1 if i % 2 == 0 else -1)
		tween.tween_property(control, "rotation_degrees", angle, step_dur) \
			.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	tween.tween_property(control, "rotation_degrees", 0.0, step_dur) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_SINE)
	return tween


## Apply an animation function to each control with a stagger delay.
## anim_func: Callable(control: Control) -> Tween
## Returns the last tween (await it for "all done").
static func stagger(controls: Array, anim_func: Callable, stagger_delay: float = 0.05) -> Tween:
	var last_tween: Tween = null
	for i in range(controls.size()):
		var control: Control = controls[i]
		# Delay by creating a timer tween first
		if i > 0 and stagger_delay > 0:
			var delay_tween := control.create_tween()
			delay_tween.tween_interval(stagger_delay * i)
			delay_tween.tween_callback(func(): anim_func.call(control))
			last_tween = delay_tween
		else:
			last_tween = anim_func.call(control)
	return last_tween


## Stagger animations by distance from origin point (closer = earlier).
## anim_func: Callable(control: Control) -> Tween
## speed: pixels per second for stagger timing
static func stagger_by_distance(controls: Array, origin: Vector2, anim_func: Callable, speed: float = 500.0) -> Tween:
	# Sort controls by distance from origin
	var sorted: Array = controls.duplicate()
	sorted.sort_custom(func(a: Control, b: Control):
		return a.global_position.distance_to(origin) < b.global_position.distance_to(origin)
	)
	var last_tween: Tween = null
	for control: Control in sorted:
		var dist := control.global_position.distance_to(origin)
		var delay := dist / maxf(speed, 1.0)
		if delay > 0:
			var delay_tween := control.create_tween()
			delay_tween.tween_interval(delay)
			delay_tween.tween_callback(func(): anim_func.call(control))
			last_tween = delay_tween
		else:
			last_tween = anim_func.call(control)
	return last_tween


## Animate a control along a bezier arc to target position.
static func fly_to(control: Control, target_pos: Vector2, duration: float = 0.5, arc_height: float = 50.0) -> Tween:
	var start_pos := control.position
	var mid_x := (start_pos.x + target_pos.x) * 0.5
	var mid_y := minf(start_pos.y, target_pos.y) - arc_height
	var mid_pos := Vector2(mid_x, mid_y)
	var tween := control.create_tween()
	tween.tween_method(
		func(t: float):
			# Quadratic bezier: P = (1-t)^2*P0 + 2*(1-t)*t*P1 + t^2*P2
			var u := 1.0 - t
			control.position = u * u * start_pos + 2.0 * u * t * mid_pos + t * t * target_pos,
		0.0, 1.0, duration
	).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
	return tween
