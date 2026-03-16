extends Label
## Floating damage number that rises and fades.

var _velocity: Vector2 = Vector2(0, -80)
var _lifetime: float = 1.0
var _timer: float = 0.0


func setup(damage: int, color: Color = Color.WHITE, crit: bool = false) -> void:
	text = str(damage)
	add_theme_font_size_override("font_size", 20 if not crit else 28)
	add_theme_color_override("font_color", color)
	horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_timer = 0.0


func _process(delta: float) -> void:
	_timer += delta
	position += _velocity * delta
	_velocity.y += 50 * delta  # Gravity
	modulate.a = 1.0 - (_timer / _lifetime)
	if _timer >= _lifetime:
		queue_free()
