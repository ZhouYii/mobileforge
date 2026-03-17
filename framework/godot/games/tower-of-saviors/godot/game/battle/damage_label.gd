extends Label
## Floating damage number that rises and fades.
## Element-colored text, 0.46s float duration (from ToS playerDamageTextJumpTime).
## Shadow outline for readability. Crits: gold + shake.

var _velocity: Vector2 = Vector2(0, -80)
var _lifetime: float = TosTheme.ANIM_DAMAGE
var _timer: float = 0.0


func setup(damage: int, color: Color = Color.WHITE, crit: bool = false) -> void:
	text = str(damage)
	horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

	if crit:
		add_theme_font_size_override("font_size", 28)
		add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))  # Gold for crits
		# Shake effect for crits
		if is_inside_tree():
			MFUIAnim.shake(self, 6.0, 0.3)
	else:
		add_theme_font_size_override("font_size", 20)
		add_theme_color_override("font_color", color)

	# Shadow outline for readability
	add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
	add_theme_constant_override("shadow_offset_x", 1)
	add_theme_constant_override("shadow_offset_y", 1)

	_timer = 0.0


## Setup with element-based coloring
func setup_element(damage: int, element: int, crit: bool = false) -> void:
	setup(damage, TosTheme.element_color(element), crit)


func _process(delta: float) -> void:
	_timer += delta
	position += _velocity * delta
	_velocity.y += 50 * delta  # Gravity
	modulate.a = 1.0 - (_timer / _lifetime)
	if _timer >= _lifetime:
		queue_free()
