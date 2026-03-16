extends MFBaseScreen
## Gacha pull screen. Shows available pools, handles pulls.

var _game_data: Node
var _economy: MFEconomy
var _monster_manager: MFMonsterManager
var _player_state: Node
var _event_bus: Node
var _ui_router: Node
var _pity_tracker: MFPityTracker

var _result_container: VBoxContainer
var _currency_label: Label

func setup(game_data: Node, economy: MFEconomy, monster_manager: MFMonsterManager,
           player_state: Node, event_bus: Node, ui_router: Node) -> void:
	_game_data = game_data
	_economy = economy
	_monster_manager = monster_manager
	_player_state = player_state
	_event_bus = event_bus
	_ui_router = ui_router
	_pity_tracker = MFPityTracker.new()

func on_enter(_params: Dictionary = {}) -> void:
	_build_ui()

func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	var main = VBoxContainer.new()
	main.set_anchors_preset(Control.PRESET_FULL_RECT)
	main.offset_left = 10
	main.offset_right = -10
	main.offset_top = 10
	add_child(main)

	# Header
	var header = HBoxContainer.new()
	main.add_child(header)

	var title = Label.new()
	title.text = "Gacha"
	title.add_theme_font_size_override("font_size", 24)
	header.add_child(title)

	header.add_child(Control.new())  # Spacer

	_currency_label = Label.new()
	_currency_label.text = "Gems: %d" % _economy.get_balance("gems")
	header.add_child(_currency_label)

	var back_btn = Button.new()
	back_btn.text = "Back"
	back_btn.pressed.connect(func(): _ui_router.pop())
	header.add_child(back_btn)

	# Pool list
	var pools = _game_data.get_all_definitions(&"gacha_pools")
	for pool_def in pools:
		var pool_data = pool_def.raw()
		var pool = MFGachaTypes.GachaPool.new(pool_data)
		_add_pool_ui(main, pool)

	# Results area
	_result_container = VBoxContainer.new()
	main.add_child(_result_container)

func _add_pool_ui(parent: VBoxContainer, pool: RefCounted) -> void:
	var panel = PanelContainer.new()
	parent.add_child(panel)

	var hbox = HBoxContainer.new()
	panel.add_child(hbox)

	var info = VBoxContainer.new()
	hbox.add_child(info)

	var name_label = Label.new()
	name_label.text = pool.name
	info.add_child(name_label)

	var cost_label = Label.new()
	cost_label.text = "Cost: %d %s per pull" % [pool.cost_amount, pool.cost_currency]
	cost_label.add_theme_font_size_override("font_size", 12)
	info.add_child(cost_label)

	# Rate display
	var rates = MFGachaRoller.get_displayed_rates(pool)
	var rate_text := ""
	for rarity in rates:
		rate_text += "★%d: %.1f%%  " % [rarity, rates[rarity]]
	var rate_label = Label.new()
	rate_label.text = rate_text
	rate_label.add_theme_font_size_override("font_size", 11)
	info.add_child(rate_label)

	var btns = VBoxContainer.new()
	hbox.add_child(btns)

	var pull1_btn = Button.new()
	pull1_btn.text = "Pull x1"
	pull1_btn.custom_minimum_size.x = 100
	var pool_ref = pool
	pull1_btn.pressed.connect(func(): _do_pull(pool_ref, 1))
	btns.add_child(pull1_btn)

	var pull10_btn = Button.new()
	pull10_btn.text = "Pull x10"
	pull10_btn.custom_minimum_size.x = 100
	pull10_btn.pressed.connect(func(): _do_pull(pool_ref, 10))
	btns.add_child(pull10_btn)

func _do_pull(pool: RefCounted, count: int) -> void:
	var total_cost := pool.cost_amount * count
	if not _economy.can_afford(pool.cost_currency, total_cost):
		_show_result("Not enough %s! Need %d" % [pool.cost_currency, total_cost])
		return

	_economy.spend(pool.cost_currency, total_cost)

	var rng = RandomNumberGenerator.new()
	rng.randomize()
	var pity := _pity_tracker.get_pity(pool.id)
	var results = MFGachaRoller.roll_multi(pool, count, pity, rng)

	# Update pity
	for result in results:
		if result.is_pity or _is_top_rarity(pool, result.rarity):
			_pity_tracker.reset(pool.id)
		else:
			_pity_tracker.increment(pool.id)

	# Add monsters to collection
	for result in results:
		var instance = _monster_manager.create_instance(result.monster_id)
		# TODO: add to player inventory

	# Display results
	_show_pull_results(results)
	_currency_label.text = "Gems: %d" % _economy.get_balance("gems")

func _show_pull_results(results: Array) -> void:
	# Clear previous results
	for child in _result_container.get_children():
		child.queue_free()

	var header = Label.new()
	header.text = "--- Pull Results ---"
	header.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result_container.add_child(header)

	for result in results:
		var def = _monster_manager.get_def(result.monster_id)
		var text := "★%d " % result.rarity
		text += def.name if def != null else "Monster #%d" % result.monster_id
		if result.is_pity:
			text += " (PITY!)"
		if result.is_featured:
			text += " ★FEATURED★"

		var label = Label.new()
		label.text = text
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		_result_container.add_child(label)

func _show_result(text: String) -> void:
	for child in _result_container.get_children():
		child.queue_free()
	var label = Label.new()
	label.text = text
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_result_container.add_child(label)

func _is_top_rarity(pool: RefCounted, rarity: int) -> bool:
	var max_r := 0
	for entry in pool.entries:
		if entry.rarity > max_r:
			max_r = entry.rarity
	return rarity == max_r
