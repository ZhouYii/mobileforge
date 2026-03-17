extends PanelContainer
## Monster/item card display component.
## Binds to a MonsterInstance or MonsterDef to show name, element, rarity, level.
## Styled with element-colored backgrounds and rarity star indicators.

var _vbox: VBoxContainer
var _name_label: Label
var _level_label: Label
var _element_label: Label
var _rarity_label: Label
var _bg_panel: StyleBoxFlat


func _init() -> void:
	# Create styled background
	_bg_panel = StyleBoxFlat.new()
	_bg_panel.bg_color = Color(0.15, 0.15, 0.2)
	_bg_panel.corner_radius_top_left = 6
	_bg_panel.corner_radius_top_right = 6
	_bg_panel.corner_radius_bottom_left = 6
	_bg_panel.corner_radius_bottom_right = 6
	_bg_panel.border_width_bottom = 3
	_bg_panel.border_color = Color(0.3, 0.3, 0.3)
	add_theme_stylebox_override("panel", _bg_panel)

	_vbox = VBoxContainer.new()
	_vbox.add_theme_constant_override("separation", 2)
	add_child(_vbox)

	# Element indicator (colored bar at top)
	_element_label = Label.new()
	_element_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_element_label.add_theme_font_size_override("font_size", 10)
	_vbox.add_child(_element_label)

	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_name_label.add_theme_font_size_override("font_size", 11)
	_vbox.add_child(_name_label)

	_rarity_label = Label.new()
	_rarity_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_rarity_label.add_theme_font_size_override("font_size", 10)
	_vbox.add_child(_rarity_label)

	_level_label = Label.new()
	_level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_level_label.add_theme_font_size_override("font_size", 9)
	_vbox.add_child(_level_label)


## Bind to a monster instance + def for full display.
func bind(instance: RefCounted, def: RefCounted) -> void:
	var element := def.element if def != null else 0
	var rarity := def.rarity if def != null else 1

	_name_label.text = def.name if def != null else "???"
	_level_label.text = "Lv.%d" % instance.level
	_element_label.text = _element_icon(element) + " " + _element_name(element)
	_rarity_label.text = _rarity_stars(rarity)

	_apply_element_style(element)
	_apply_rarity_style(rarity)


## Bind to just a definition (for gacha display, dex, etc.).
func bind_def(def: RefCounted) -> void:
	_name_label.text = def.name
	_level_label.text = ""
	_element_label.text = _element_icon(def.element) + " " + _element_name(def.element)
	_rarity_label.text = _rarity_stars(def.rarity)

	_apply_element_style(def.element)
	_apply_rarity_style(def.rarity)


func _apply_element_style(element: int) -> void:
	var color := _element_color(element)
	# Tinted background
	_bg_panel.bg_color = Color(color.r * 0.15, color.g * 0.15, color.b * 0.15, 0.9)
	# Colored bottom border
	_bg_panel.border_color = color
	# Element text color
	_element_label.add_theme_color_override("font_color", color)


func _apply_rarity_style(rarity: int) -> void:
	var star_color := _rarity_color(rarity)
	_rarity_label.add_theme_color_override("font_color", star_color)

	# Higher rarity gets a glow-like top border
	if rarity >= 5:
		_bg_panel.border_width_top = 2
		_bg_panel.border_width_left = 1
		_bg_panel.border_width_right = 1
		var glow_color := star_color
		glow_color.a = 0.6
		_bg_panel.border_color = glow_color
	elif rarity >= 4:
		_bg_panel.border_width_top = 1


static func _element_name(element: int) -> String:
	match element:
		1: return "Water"
		2: return "Fire"
		3: return "Earth"
		4: return "Light"
		5: return "Dark"
		6: return "Heart"
		_: return "None"


static func _element_icon(element: int) -> String:
	match element:
		1: return "\u2248"  # ≈ water waves
		2: return "\u2668"  # ♨ fire/hot springs
		3: return "\u2618"  # ☘ shamrock/earth
		4: return "\u2600"  # ☀ sun/light
		5: return "\u263d"  # ☽ moon/dark
		6: return "\u2665"  # ♥ heart
		_: return "\u25cf"  # ● circle


static func _element_color(element: int) -> Color:
	match element:
		1: return Color(0.3, 0.6, 1.0)    # Water - blue
		2: return Color(1.0, 0.35, 0.2)   # Fire - red-orange
		3: return Color(0.3, 0.8, 0.25)   # Earth - green
		4: return Color(1.0, 0.95, 0.4)   # Light - warm yellow
		5: return Color(0.7, 0.35, 0.95)  # Dark - purple
		6: return Color(1.0, 0.4, 0.55)   # Heart - pink
		_: return Color(0.5, 0.5, 0.5)    # Neutral - grey


static func _rarity_color(rarity: int) -> Color:
	match rarity:
		6: return Color(1.0, 0.85, 0.0)   # 6★ - gold
		5: return Color(1.0, 0.7, 0.1)    # 5★ - gold-orange
		4: return Color(0.7, 0.5, 1.0)    # 4★ - purple
		3: return Color(0.4, 0.7, 1.0)    # 3★ - blue
		2: return Color(0.6, 0.8, 0.6)    # 2★ - green-grey
		_: return Color(0.7, 0.7, 0.7)    # 1★ - grey


static func _rarity_stars(rarity: int) -> String:
	return "\u2605".repeat(rarity)
