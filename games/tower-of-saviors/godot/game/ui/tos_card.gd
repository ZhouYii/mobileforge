class_name TosCard extends PanelContainer
## Interactive monster card with ToS-specific additions.
## Wraps the framework MFCardView pattern with:
## - Click signal, selection highlight, dimmed/locked states
## - Plus-stat badge, favorite star, awakening dots
## - Acquisition animation (1.12x scale pop)

signal card_pressed(monster_id: int)

var _monster_id: int = -1
var _vbox: VBoxContainer
var _element_label: Label
var _name_label: Label
var _rarity_label: Label
var _level_label: Label
var _stat_label: Label
var _style: StyleBoxFlat

var _is_selected: bool = false
var _is_dimmed: bool = false
var _is_locked: bool = false

# State tracking for feedback
var _feedback: MFButtonFeedback


func _init() -> void:
	custom_minimum_size = Vector2(72, 96)
	mouse_filter = MOUSE_FILTER_STOP

	_style = StyleBoxFlat.new()
	_style.bg_color = Color(0.12, 0.12, 0.16, 0.9)
	_style.corner_radius_top_left = 6
	_style.corner_radius_top_right = 6
	_style.corner_radius_bottom_left = 6
	_style.corner_radius_bottom_right = 6
	_style.border_width_bottom = 3
	_style.border_color = Color(0.3, 0.3, 0.35)
	_style.content_margin_left = 4
	_style.content_margin_right = 4
	_style.content_margin_top = 4
	_style.content_margin_bottom = 4
	add_theme_stylebox_override("panel", _style)

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 1)
	add_child(_vbox)

	_element_label = Label.new()
	_element_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_element_label.add_theme_font_size_override("font_size", 10)
	_vbox.add_child(_element_label)

	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_name_label.add_theme_font_size_override("font_size", 11)
	_name_label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	_vbox.add_child(_name_label)

	_rarity_label = Label.new()
	_rarity_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_rarity_label.add_theme_font_size_override("font_size", 9)
	_vbox.add_child(_rarity_label)

	_level_label = Label.new()
	_level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_level_label.add_theme_font_size_override("font_size", 9)
	_vbox.add_child(_level_label)

	_stat_label = Label.new()
	_stat_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_stat_label.add_theme_font_size_override("font_size", 8)
	_stat_label.visible = false
	_vbox.add_child(_stat_label)


func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		card_pressed.emit(_monster_id)
		accept_event()


## Bind to a monster definition (for grid display, gacha, etc.)
func bind_def(def: RefCounted, monster_id: int = -1) -> void:
	_monster_id = monster_id if monster_id >= 0 else (def.id if def != null else -1)
	if def == null:
		_name_label.text = "???"
		return

	var element: int = def.element
	var rarity: int = def.rarity

	_name_label.text = def.name
	_element_label.text = TosTheme.element_icon(element) + " " + TosTheme.element_name(element)
	_element_label.add_theme_color_override("font_color", TosTheme.element_color(element))
	_rarity_label.text = TosTheme.rarity_stars(rarity)
	_rarity_label.add_theme_color_override("font_color", TosTheme.rarity_color(rarity))
	_level_label.text = ""

	_apply_element_style(element)
	_apply_rarity_border(rarity)


## Bind to a monster instance + def for full display with level.
func bind(instance: RefCounted, def: RefCounted) -> void:
	bind_def(def, instance.monster_id if instance != null else -1)
	if instance != null:
		_level_label.text = "Lv.%d" % instance.level


## Show plus stats (e.g. "+120" for plus eggs)
func set_plus_stats(plus: int) -> void:
	if plus > 0:
		_stat_label.text = "+%d" % plus
		_stat_label.add_theme_color_override("font_color", Color(1.0, 1.0, 0.3))
		_stat_label.visible = true
	else:
		_stat_label.visible = false


## Selection state — bright highlight border
func set_selected(selected: bool) -> void:
	_is_selected = selected
	if selected:
		_style.border_width_left = 2
		_style.border_width_right = 2
		_style.border_width_top = 2
		_style.border_color = Color(1.0, 1.0, 1.0, 0.9)
	else:
		_style.border_width_left = 0
		_style.border_width_right = 0
		_style.border_width_top = 0
		# Restore element-based border
		_style.border_width_bottom = 3


## Dimmed state (0.6 alpha per ToS DataCardIcon)
func set_dimmed(dimmed: bool) -> void:
	_is_dimmed = dimmed
	modulate.a = TosTheme.CARD_ALPHA_DISABLED if dimmed else 1.0


## Locked state (0.55 alpha + grey frame per ToS DataCardIcon)
func set_locked(locked: bool) -> void:
	_is_locked = locked
	if locked:
		modulate.a = TosTheme.CARD_ALPHA_LOCKED
		_style.border_color = TosTheme.CARD_FRAME_LOCKED
	else:
		modulate.a = 1.0


## Acquisition animation: scale to 1.12x over 0.3s with easeInOutBack
func play_acquire_animation() -> void:
	pivot_offset = size / 2.0
	var tween := create_tween()
	tween.tween_property(self, "scale", Vector2(TosTheme.CARD_POP_SCALE, TosTheme.CARD_POP_SCALE), TosTheme.ANIM_CARD_POP * 0.6) \
		.set_ease(Tween.EASE_OUT).set_trans(Tween.TRANS_BACK)
	tween.tween_property(self, "scale", Vector2.ONE, TosTheme.ANIM_CARD_POP * 0.4) \
		.set_ease(Tween.EASE_IN_OUT).set_trans(Tween.TRANS_QUAD)


## Set empty state (dashed appearance for empty team slots)
func set_empty(placeholder_text: String = "+") -> void:
	_monster_id = -1
	_name_label.text = placeholder_text
	_element_label.text = ""
	_rarity_label.text = ""
	_level_label.text = ""
	_stat_label.visible = false
	_style.bg_color = Color(0.08, 0.08, 0.1, 0.5)
	_style.border_color = Color(0.3, 0.3, 0.35, 0.5)
	_style.border_width_bottom = 2
	# Dashed border effect via alternating alpha
	modulate.a = 0.7


func _apply_element_style(element: int) -> void:
	var ec := TosTheme.element_color(element)
	_style.bg_color = Color(ec.r * 0.12, ec.g * 0.12, ec.b * 0.12, 0.92)
	_style.border_color = ec


func _apply_rarity_border(rarity: int) -> void:
	var rc := TosTheme.rarity_color(rarity)
	_style.border_color = rc
	if rarity >= 5:
		_style.border_width_top = 2
		_style.border_width_left = 1
		_style.border_width_right = 1
	elif rarity >= 4:
		_style.border_width_top = 1
