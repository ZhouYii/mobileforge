extends PanelContainer
## Monster/item card display component.
## Binds to a MonsterInstance or MonsterDef to show icon, element, rarity, level.

var _name_label: Label
var _level_label: Label
var _element_label: Label
var _rarity_label: Label

func _init() -> void:
	var vbox = VBoxContainer.new()
	add_child(vbox)

	_name_label = Label.new()
	_name_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_name_label)

	_element_label = Label.new()
	_element_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_element_label)

	_rarity_label = Label.new()
	_rarity_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_rarity_label)

	_level_label = Label.new()
	_level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	vbox.add_child(_level_label)

## Bind to a monster instance + def for full display
func bind(instance: RefCounted, def: RefCounted) -> void:
	_name_label.text = def.name if def != null else "???"
	_level_label.text = "Lv.%d" % instance.level
	_element_label.text = _element_name(def.element if def != null else 0)
	_rarity_label.text = "\u2605".repeat(def.rarity if def != null else 1)

## Bind to just a definition (for gacha display, dex, etc.)
func bind_def(def: RefCounted) -> void:
	_name_label.text = def.name
	_level_label.text = ""
	_element_label.text = _element_name(def.element)
	_rarity_label.text = "\u2605".repeat(def.rarity)

func _element_name(element: int) -> String:
	match element:
		1: return "Water"
		2: return "Fire"
		3: return "Grass"
		4: return "Light"
		5: return "Dark"
		6: return "Heart"
		_: return "None"
