## Material Property Tweener
## Animates shader parameters (color, float, vector) on ShaderMaterial nodes.
## Consolidates the material tween patterns from Coin Master (GoTween),
## Merge Dragons (ShaderPropAnimator), and Monument Valley (MaterialParameterBlender).
##
## Usage:
##   MaterialTweener.tween_color(sprite, "tint_color", Color.RED, 0.5)
##   MaterialTweener.tween_float(sprite, "glow_power", 1.0, 0.3)
##   MaterialTweener.ping_pong_float(sprite, "glow_power", 0.3, 1.0, 1.0)
class_name MaterialTweener
extends RefCounted


## Tween a shader color parameter from current value to target.
static func tween_color(
	node: CanvasItem,
	param: StringName,
	end_value: Color,
	duration: float,
	ease_type: Tween.EaseType = Tween.EASE_IN_OUT,
	trans_type: Tween.TransitionType = Tween.TRANS_CUBIC
) -> Tween:
	var mat := node.material as ShaderMaterial
	if not mat:
		push_error("MaterialTweener: node has no ShaderMaterial")
		return null
	var start_value: Color = mat.get_shader_parameter(param)
	var tween := node.create_tween()
	tween.tween_method(
		func(val: Color) -> void: mat.set_shader_parameter(param, val),
		start_value, end_value, duration
	).set_ease(ease_type).set_trans(trans_type)
	return tween


## Tween a shader float parameter from current value to target.
static func tween_float(
	node: CanvasItem,
	param: StringName,
	end_value: float,
	duration: float,
	ease_type: Tween.EaseType = Tween.EASE_IN_OUT,
	trans_type: Tween.TransitionType = Tween.TRANS_CUBIC
) -> Tween:
	var mat := node.material as ShaderMaterial
	if not mat:
		push_error("MaterialTweener: node has no ShaderMaterial")
		return null
	var start_value: float = mat.get_shader_parameter(param)
	var tween := node.create_tween()
	tween.tween_method(
		func(val: float) -> void: mat.set_shader_parameter(param, val),
		start_value, end_value, duration
	).set_ease(ease_type).set_trans(trans_type)
	return tween


## Tween a shader Vector4 parameter from current value to target.
static func tween_vector4(
	node: CanvasItem,
	param: StringName,
	end_value: Vector4,
	duration: float,
	ease_type: Tween.EaseType = Tween.EASE_IN_OUT,
	trans_type: Tween.TransitionType = Tween.TRANS_CUBIC
) -> Tween:
	var mat := node.material as ShaderMaterial
	if not mat:
		push_error("MaterialTweener: node has no ShaderMaterial")
		return null
	var start_value: Vector4 = mat.get_shader_parameter(param)
	var tween := node.create_tween()
	tween.tween_method(
		func(val: Vector4) -> void: mat.set_shader_parameter(param, val),
		start_value, end_value, duration
	).set_ease(ease_type).set_trans(trans_type)
	return tween


## Ping-pong a float shader parameter between two values indefinitely.
## Inspired by Merge Dragons' ShaderPropAnimator with random offset/speed.
static func ping_pong_float(
	node: CanvasItem,
	param: StringName,
	min_value: float,
	max_value: float,
	cycle_duration: float,
	randomize_start: bool = true
) -> Tween:
	var mat := node.material as ShaderMaterial
	if not mat:
		push_error("MaterialTweener: node has no ShaderMaterial")
		return null
	if randomize_start:
		var t := randf()
		mat.set_shader_parameter(param, lerpf(min_value, max_value, t))
	var half := cycle_duration * 0.5
	var tween := node.create_tween().set_loops()
	tween.tween_method(
		func(val: float) -> void: mat.set_shader_parameter(param, val),
		min_value, max_value, half
	).set_trans(Tween.TRANS_SINE)
	tween.tween_method(
		func(val: float) -> void: mat.set_shader_parameter(param, val),
		max_value, min_value, half
	).set_trans(Tween.TRANS_SINE)
	return tween


## Blend all lighting colors between two material presets.
## Inspired by Monument Valley's MaterialParameterBlender.
static func blend_lighting(
	target_mat: ShaderMaterial,
	mat_a: ShaderMaterial,
	mat_b: ShaderMaterial,
	t: float,
	light_params: Array[StringName] = [
		&"light_colour_0", &"light_colour_1",
		&"light_colour_2", &"light_colour_3",
		&"shadow_colour", &"ambient_colour"
	]
) -> void:
	for param in light_params:
		var a_val: Color = mat_a.get_shader_parameter(param)
		var b_val: Color = mat_b.get_shader_parameter(param)
		target_mat.set_shader_parameter(param, a_val.lerp(b_val, t))
