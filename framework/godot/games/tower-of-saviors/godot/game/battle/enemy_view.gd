extends PanelContainer
## Displays a single enemy: name, HP bar, countdown.
## Element-colored background, MFProgressBar for HP, countdown badge.
## Enter animation: 1.0s (from ToS Come()).

var _vbox: VBoxContainer
var _name_label: Label
var _hp_bar: MFProgressBar
var _hp_label: Label
var _countdown_label: Label
var _element: int = 0
var _bg_style: StyleBoxFlat
var _entered: bool = false


func _init() -> void:
	custom_minimum_size = Vector2(130, 140)

	_bg_style = TosTheme.make_panel()
	add_theme_stylebox_override("panel", _bg_style)

	_vbox = VBoxContainer.new()
	_vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_vbox.add_theme_constant_override("separation", 4)
	add_child(_vbox)

	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_name_label.add_theme_font_size_override("font_size", 13)
	_name_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.6))
	_name_label.add_theme_constant_override("shadow_offset_x", 1)
	_name_label.add_theme_constant_override("shadow_offset_y", 1)
	_name_label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	_vbox.add_child(_name_label)

	# Element-tinted MFProgressBar for HP
	_hp_bar = MFProgressBar.new()
	_hp_bar.animation_duration = TosTheme.ANIM_HP_BAR
	_hp_bar.setup(110, 14)
	_vbox.add_child(_hp_bar)

	_hp_label = Label.new()
	_hp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hp_label.add_theme_font_size_override("font_size", 11)
	_hp_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))
	_hp_label.add_theme_constant_override("shadow_offset_x", 1)
	_hp_label.add_theme_constant_override("shadow_offset_y", 1)
	_vbox.add_child(_hp_label)

	# Countdown badge
	_countdown_label = Label.new()
	_countdown_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_countdown_label.add_theme_font_size_override("font_size", 16)
	_countdown_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.6))
	_countdown_label.add_theme_constant_override("shadow_offset_x", 1)
	_countdown_label.add_theme_constant_override("shadow_offset_y", 1)
	_vbox.add_child(_countdown_label)


func update_from_enemy(enemy: RefCounted) -> void:
	_name_label.text = enemy.name

	# Update element styling
	_element = enemy.element if "element" in enemy else 0
	var ec := TosTheme.element_color(_element)
	_bg_style.bg_color = Color(ec.r * 0.12, ec.g * 0.12, ec.b * 0.12, 0.92)
	_bg_style.border_color = ec
	_hp_bar.set_colors(ec)

	# HP bar
	var hp_ratio := float(enemy.hp) / float(enemy.max_hp) if enemy.max_hp > 0 else 0.0
	_hp_bar.set_value(hp_ratio, _entered)
	_hp_label.text = "%d / %d" % [enemy.hp, enemy.max_hp]

	# Countdown display with pulse when CD=1
	_countdown_label.text = "CD: %d" % enemy.countdown
	if enemy.countdown <= 1:
		_countdown_label.add_theme_color_override("font_color", Color(1.0, 0.3, 0.3))
		if _entered and is_inside_tree():
			MFUIAnim.pulse(_countdown_label, 1.2, 0.6)
	else:
		_countdown_label.remove_theme_color_override("font_color")

	# Dead state: greyscale + red X
	if not enemy.is_alive:
		modulate = Color(0.4, 0.4, 0.4, 0.5)
		_name_label.text = enemy.name + " \u2716"  # ✖
	else:
		modulate = Color.WHITE

	_entered = true


## Play enter animation (1.0s from ToS Come())
func play_enter() -> void:
	MFUIAnim.slide_in_from(self, Vector2(0, -60), TosTheme.ANIM_ENEMY_ENTER)
