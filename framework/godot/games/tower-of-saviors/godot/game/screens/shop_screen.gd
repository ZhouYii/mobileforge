extends MFBaseScreen
## Shop screen — regular items + event shop with limited-time currency items.

var _economy: MFEconomy
var _ui_router: Node
var _player_state: Node
var _monster_manager: MFMonsterManager
var _game_data: Node
var _event_shop: MFEventShop
var _status_label: Label
var _main: VBoxContainer
var _selected_tab: String = "general"


func setup(economy: MFEconomy, ui_router: Node, player_state: Node = null,
		monster_manager: MFMonsterManager = null, game_data: Node = null) -> void:
	_economy = economy
	_ui_router = ui_router
	_player_state = player_state
	_monster_manager = monster_manager
	_game_data = game_data
	if player_state != null:
		_event_shop = MFEventShop.new(player_state, economy)


func on_enter(_params: Dictionary = {}) -> void:
	_build_ui()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	_main = VBoxContainer.new()
	_main.set_anchors_preset(Control.PRESET_FULL_RECT)
	_main.offset_left = 12
	_main.offset_right = -12
	_main.offset_top = 12
	_main.add_theme_constant_override("separation", 8)
	add_child(_main)

	# Header
	var header = HBoxContainer.new()
	header.add_theme_constant_override("separation", 12)
	_main.add_child(header)

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
	var parts: Array = [
		"Gems: %d" % _economy.get_balance("gems"),
		"Coins: %d" % _economy.get_balance("coins"),
		"Stamina: %d" % _economy.get_balance("stamina"),
	]
	var event_bal := _economy.get_balance("event_tokens")
	if event_bal > 0:
		parts.append("Event Tokens: %d" % event_bal)
	currency_info.text = " | ".join(parts)
	currency_info.add_theme_font_size_override("font_size", 12)
	_main.add_child(currency_info)

	# Tab bar
	var tab_bar = HBoxContainer.new()
	tab_bar.alignment = BoxContainer.ALIGNMENT_CENTER
	tab_bar.add_theme_constant_override("separation", 6)
	_main.add_child(tab_bar)

	for tab in ["general", "event"]:
		var tab_btn = Button.new()
		tab_btn.text = tab.capitalize()
		tab_btn.custom_minimum_size = Vector2(100, 34)
		if tab == _selected_tab:
			tab_btn.modulate = Color(1.0, 0.9, 0.5)
		else:
			tab_btn.modulate = Color(0.6, 0.6, 0.6)
		var t = tab
		tab_btn.pressed.connect(func():
			_selected_tab = t
			_build_ui())
		tab_bar.add_child(tab_btn)

	# Content
	var scroll = ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_main.add_child(scroll)

	var content = VBoxContainer.new()
	content.add_theme_constant_override("separation", 6)
	scroll.add_child(content)

	if _selected_tab == "general":
		_build_general_shop(content)
	else:
		_build_event_shop(content)

	# Status
	_status_label = Label.new()
	_status_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_main.add_child(_status_label)


func _build_general_shop(parent: VBoxContainer) -> void:
	# Stamina refill
	_add_shop_button(parent, "Refill Stamina (+%d)" % _economy.get_max_stamina(), "1 Gem", func():
		if _economy.refill_stamina_with_gems():
			_status_label.text = "Stamina refilled!"
			_build_ui()
		else:
			_status_label.text = "Not enough gems!")

	# Coin pack
	_add_shop_button(parent, "Coin Pack (10,000 Coins)", "2 Gems", func():
		if _economy.spend("gems", 2):
			_economy.earn("coins", 10000)
			_status_label.text = "Purchased 10,000 coins!"
			_build_ui()
		else:
			_status_label.text = "Not enough gems!")

	# Debug: free gems
	_add_shop_button(parent, "Free 50 Gems (Debug)", "Free", func():
		_economy.earn("gems", 50)
		_build_ui())

	# Debug: free event tokens
	_add_shop_button(parent, "Free 100 Event Tokens (Debug)", "Free", func():
		_economy.earn("event_tokens", 100)
		_build_ui())


func _build_event_shop(parent: VBoxContainer) -> void:
	if _event_shop == null or _game_data == null:
		var lbl = Label.new()
		lbl.text = "No events active"
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		parent.add_child(lbl)
		return

	# Load event shop definitions
	var shop_defs = _game_data.get_all_definitions(&"event_shops")
	if shop_defs.is_empty():
		var lbl = Label.new()
		lbl.text = "No event shops available"
		lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		parent.add_child(lbl)
		return

	for shop_def_data in shop_defs:
		var shop_def = MFEventShop.EventShopDef.new(shop_def_data.raw())

		var shop_header = Label.new()
		shop_header.text = shop_def.name
		shop_header.add_theme_font_size_override("font_size", 18)
		shop_header.add_theme_color_override("font_color", Color(1.0, 0.7, 0.1))
		parent.add_child(shop_header)

		var bal_lbl = Label.new()
		bal_lbl.text = "%s: %d" % [shop_def.currency, _economy.get_balance(shop_def.currency)]
		bal_lbl.add_theme_font_size_override("font_size", 12)
		parent.add_child(bal_lbl)

		for item in shop_def.items:
			var bought := _event_shop.get_purchase_count(shop_def.id, item.id)
			var limit_text := ""
			if item.buy_limit > 0:
				limit_text = " [%d/%d]" % [bought, item.buy_limit]

			var item_text := "%s x%d" % [item.name, item.count]
			var cost_text := "%d %s%s" % [item.cost_amount, item.cost_currency, limit_text]

			var at_limit: bool = item.buy_limit > 0 and bought >= item.buy_limit

			var panel = PanelContainer.new()
			parent.add_child(panel)
			var hbox = HBoxContainer.new()
			hbox.add_theme_constant_override("separation", 8)
			panel.add_child(hbox)

			var info_lbl = Label.new()
			info_lbl.text = item_text
			info_lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			hbox.add_child(info_lbl)

			var cost_lbl = Label.new()
			cost_lbl.text = cost_text
			cost_lbl.add_theme_font_size_override("font_size", 12)
			hbox.add_child(cost_lbl)

			var buy_btn = Button.new()
			if at_limit:
				buy_btn.text = "Sold Out"
				buy_btn.disabled = true
			else:
				buy_btn.text = "Buy"
				buy_btn.custom_minimum_size = Vector2(60, 34)
			var sd = shop_def
			var si = item
			buy_btn.pressed.connect(func(): _on_buy_event_item(sd, si))
			hbox.add_child(buy_btn)


func _on_buy_event_item(shop_def: RefCounted, item: RefCounted) -> void:
	var result = _event_shop.purchase(shop_def, item.id)
	if not result.success:
		_status_label.text = result.error
		return

	# Grant the purchased item
	if item.type == "currency":
		_economy.earn(item.currency_reward, item.count)
		_status_label.text = "Purchased %d %s!" % [item.count, item.currency_reward]
	elif item.type == "monster" and _monster_manager != null and _player_state != null:
		var inst = _monster_manager.create_instance(item.item_id)
		var section = _player_state.get_section(&"monsters")
		if section != null:
			section.set_value(StringName(str(inst.instance_id)), inst.to_dict())
		var mdef = _monster_manager.get_def(item.item_id)
		_status_label.text = "Purchased %s!" % (mdef.name if mdef else "Monster #%d" % item.item_id)
	else:
		_status_label.text = "Purchased %s x%d!" % [item.name, item.count]

	_build_ui()


func _add_shop_button(parent: VBoxContainer, text: String, cost: String, callback: Callable) -> void:
	var panel = PanelContainer.new()
	parent.add_child(panel)
	var hbox = HBoxContainer.new()
	hbox.add_theme_constant_override("separation", 8)
	panel.add_child(hbox)
	var lbl = Label.new()
	lbl.text = text
	lbl.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(lbl)
	var cost_lbl = Label.new()
	cost_lbl.text = cost
	cost_lbl.add_theme_font_size_override("font_size", 12)
	hbox.add_child(cost_lbl)
	var btn = Button.new()
	btn.text = "Buy"
	btn.custom_minimum_size = Vector2(60, 34)
	btn.pressed.connect(callback)
	hbox.add_child(btn)
