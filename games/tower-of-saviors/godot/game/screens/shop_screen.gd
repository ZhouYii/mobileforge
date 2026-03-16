extends MFBaseScreen
## Simple shop screen — stamina refill and gem packs.

var _economy: MFEconomy
var _ui_router: Node
var _status_label: Label

func setup(economy: MFEconomy, ui_router: Node) -> void:
	_economy = economy
	_ui_router = ui_router

func on_enter(_params: Dictionary = {}) -> void:
	_build_ui()

func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	var main = VBoxContainer.new()
	main.set_anchors_preset(Control.PRESET_FULL_RECT)
	main.offset_left = 20
	main.offset_right = -20
	main.offset_top = 20
	add_child(main)

	var header = HBoxContainer.new()
	main.add_child(header)

	var title = Label.new()
	title.text = "Shop"
	title.add_theme_font_size_override("font_size", 24)
	header.add_child(title)
	header.add_child(Control.new())
	var back_btn = Button.new()
	back_btn.text = "Back"
	back_btn.pressed.connect(func(): _ui_router.pop())
	header.add_child(back_btn)

	# Currency display
	var currency_info = Label.new()
	currency_info.text = "Gems: %d | Coins: %d | Stamina: %d" % [
		_economy.get_balance("gems"),
		_economy.get_balance("coins"),
		_economy.get_balance("stamina")
	]
	main.add_child(currency_info)

	var spacer = Control.new()
	spacer.custom_minimum_size.y = 20
	main.add_child(spacer)

	# Stamina refill
	var refill_btn = Button.new()
	refill_btn.text = "Refill Stamina (1 Gem → +50 Stamina)"
	refill_btn.custom_minimum_size.y = 50
	refill_btn.pressed.connect(_on_refill_stamina)
	main.add_child(refill_btn)

	# Free gems (for testing)
	var free_btn = Button.new()
	free_btn.text = "Free 50 Gems (Debug)"
	free_btn.custom_minimum_size.y = 50
	free_btn.pressed.connect(func():
		_economy.earn("gems", 50)
		_build_ui())
	main.add_child(free_btn)

	# Status
	_status_label = Label.new()
	_status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	main.add_child(_status_label)

func _on_refill_stamina() -> void:
	if _economy.spend("gems", 1):
		_economy.earn("stamina", 50)
		_status_label.text = "Stamina refilled! +50"
	else:
		_status_label.text = "Not enough gems!"
	_build_ui()
