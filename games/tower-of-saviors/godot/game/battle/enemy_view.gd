extends VBoxContainer
## Displays a single enemy: name, HP bar, countdown.

var _name_label: Label
var _hp_bar: ProgressBar
var _hp_label: Label
var _countdown_label: Label


func _init() -> void:
	alignment = BoxContainer.ALIGNMENT_CENTER
	custom_minimum_size = Vector2(120, 100)

	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	add_child(_name_label)

	_hp_bar = ProgressBar.new()
	_hp_bar.custom_minimum_size = Vector2(100, 12)
	_hp_bar.show_percentage = false
	add_child(_hp_bar)

	_hp_label = Label.new()
	_hp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_hp_label.add_theme_font_size_override("font_size", 12)
	add_child(_hp_label)

	_countdown_label = Label.new()
	_countdown_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_countdown_label.add_theme_font_size_override("font_size", 14)
	add_child(_countdown_label)


func update_from_enemy(enemy: RefCounted) -> void:
	_name_label.text = enemy.name
	_hp_bar.max_value = enemy.max_hp
	_hp_bar.value = enemy.hp
	_hp_label.text = "%d / %d" % [enemy.hp, enemy.max_hp]
	_countdown_label.text = "CD: %d" % enemy.countdown

	# Color HP bar based on remaining HP
	var hp_ratio := float(enemy.hp) / float(enemy.max_hp) if enemy.max_hp > 0 else 0.0
	if hp_ratio > 0.5:
		_hp_bar.modulate = Color.GREEN
	elif hp_ratio > 0.2:
		_hp_bar.modulate = Color.YELLOW
	else:
		_hp_bar.modulate = Color.RED

	if not enemy.is_alive:
		modulate.a = 0.3
	else:
		modulate.a = 1.0
