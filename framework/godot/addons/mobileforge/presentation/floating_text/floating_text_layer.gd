extends Node2D
class_name MFFloatingTextLayer
## Floating combat/damage text system with object pooling.
## Supports color-coding by element, amount-proportional sizing, and animations.

var _pool: MFScenePool
var _label_scene: Callable  ## Factory for Label nodes


func _init() -> void:
	_label_scene = func():
		var label := Label.new()
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.z_index = 100
		return label
	_pool = MFScenePool.new(_label_scene, 32)


func setup(label_factory: Callable = Callable()) -> void:
	if label_factory.is_valid():
		_label_scene = label_factory
		_pool = MFScenePool.new(_label_scene, 32)


## Spawn a floating text at a position.
func spawn(text: String, pos: Vector2, config: Dictionary = {}) -> void:
	var label: Label = _pool.acquire()
	label.text = text
	label.position = pos
	add_child(label)

	# Color by type
	var color: Color = Color.WHITE
	match config.get("type", ""):
		"damage": color = Color.RED
		"heal": color = Color.GREEN
		"critical": color = Color.YELLOW
		"miss": color = Color.GRAY
		_:
			if config.has("color"):
				color = config.get("color")
	label.modulate = color

	# Scale by amount (optional)
	var amount: float = float(config.get("amount", 0))
	var base_scale := 1.0
	if amount > 0:
		base_scale = clampf(1.0 + log(amount) * 0.1, 1.0, 3.0)
	label.scale = Vector2(base_scale, base_scale)

	# Animate: float up and fade out
	var duration: float = config.get("duration", 0.8)
	var rise: float = config.get("rise", 60.0)
	var tween := create_tween()
	tween.set_parallel(true)
	tween.tween_property(label, "position:y", pos.y - rise, duration)
	tween.tween_property(label, "modulate:a", 0.0, duration * 0.6).set_delay(duration * 0.4)
	tween.set_parallel(false)
	tween.tween_callback(func():
		_pool.release(label)
	)


## Convenience: spawn damage number.
func spawn_damage(amount: int, pos: Vector2, is_critical: bool = false) -> void:
	spawn(str(amount), pos, {
		"type": "critical" if is_critical else "damage",
		"amount": amount,
	})


## Convenience: spawn heal number.
func spawn_heal(amount: int, pos: Vector2) -> void:
	spawn("+" + str(amount), pos, {"type": "heal", "amount": amount})
