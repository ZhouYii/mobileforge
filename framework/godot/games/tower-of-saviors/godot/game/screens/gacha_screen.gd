extends MFBaseScreen
## Gacha pull screen. Shows available pools, handles pulls.
## Supports daily free pull and multi-pull discounts.

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

	# Monster Exchange section
	_build_exchange_section(main)

	# Results area
	_result_container = VBoxContainer.new()
	main.add_child(_result_container)

func _add_pool_ui(parent: VBoxContainer, pool: RefCounted) -> void:
	# Skip one-time pools that have been used
	var one_time_used: bool = pool.one_time and _is_one_time_used(pool.id)

	var panel = PanelContainer.new()
	parent.add_child(panel)

	var hbox = HBoxContainer.new()
	panel.add_child(hbox)

	var info = VBoxContainer.new()
	info.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hbox.add_child(info)

	var name_label = Label.new()
	name_label.text = pool.name
	info.add_child(name_label)

	# One-time / beginner gacha label
	if pool.one_time and not one_time_used:
		var beginner_lbl = Label.new()
		var desc_parts: Array = ["One-time only"]
		if pool.guaranteed_top_rarity:
			desc_parts.append("Guaranteed top rarity!")
		beginner_lbl.text = "  ".join(desc_parts)
		beginner_lbl.add_theme_font_size_override("font_size", 11)
		beginner_lbl.add_theme_color_override("font_color", Color(1.0, 0.7, 0.1))
		info.add_child(beginner_lbl)

	# Step-up banner: show current step info
	if pool.is_step_up():
		var current_step := _get_step_index(pool.id)
		var step_def: Dictionary = pool.get_step(current_step)
		var completed: bool = current_step >= pool.total_steps()

		var step_lbl = Label.new()
		if completed:
			step_lbl.text = "Step-Up COMPLETE (%d/%d)" % [pool.total_steps(), pool.total_steps()]
			step_lbl.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		else:
			var cost_mult := float(step_def.get("cost_mult", 1.0))
			var pull_count := int(step_def.get("pull_count", 10))
			var step_cost := int(pool.cost_amount * pull_count * cost_mult)
			var guaranteed := int(step_def.get("guaranteed_rarity", 0))
			step_lbl.text = "Step %d/%d — %d %s for %d pulls" % [
				current_step + 1, pool.total_steps(), step_cost, pool.cost_currency, pull_count]
			if guaranteed > 0:
				step_lbl.text += " (★%d+ guaranteed!)" % guaranteed
			step_lbl.add_theme_color_override("font_color", Color(1.0, 0.7, 0.1))
		step_lbl.add_theme_font_size_override("font_size", 12)
		info.add_child(step_lbl)
	else:
		var cost_label = Label.new()
		cost_label.text = "Cost: %d %s per pull" % [pool.cost_amount, pool.cost_currency]
		cost_label.add_theme_font_size_override("font_size", 12)
		info.add_child(cost_label)

		if pool.multi_pull_discount > 0:
			var disc_label = Label.new()
			var pay_count: int = 10 - pool.multi_pull_discount
			disc_label.text = "10-pull: pay for %d (save %d %s)" % [
				pay_count, pool.cost_amount * pool.multi_pull_discount, pool.cost_currency]
			disc_label.add_theme_font_size_override("font_size", 11)
			disc_label.add_theme_color_override("font_color", Color(0.3, 0.9, 0.3))
			info.add_child(disc_label)

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

	var pool_ref = pool

	# Step-up pull button
	if pool.is_step_up():
		var current_step := _get_step_index(pool.id)
		if current_step < pool.total_steps():
			var step_btn = Button.new()
			var step_def: Dictionary = pool.get_step(current_step)
			var cost_mult := float(step_def.get("cost_mult", 1.0))
			var pull_count := int(step_def.get("pull_count", 10))
			var step_cost := int(pool.cost_amount * pull_count * cost_mult)
			if cost_mult <= 0.0:
				step_btn.text = "FREE Step!"
				step_btn.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
			else:
				step_btn.text = "Step %d (%d)" % [current_step + 1, step_cost]
			step_btn.custom_minimum_size.x = 100
			step_btn.pressed.connect(func(): _do_step_pull(pool_ref))
			btns.add_child(step_btn)
		else:
			var done_lbl = Label.new()
			done_lbl.text = "All steps done"
			done_lbl.add_theme_font_size_override("font_size", 11)
			btns.add_child(done_lbl)
	elif one_time_used:
		# Already used one-time pool
		var done_lbl = Label.new()
		done_lbl.text = "Completed"
		done_lbl.add_theme_font_size_override("font_size", 12)
		done_lbl.add_theme_color_override("font_color", Color(0.5, 0.5, 0.5))
		btns.add_child(done_lbl)
	elif pool.one_time:
		# One-time pool (beginner gacha)
		var pull_btn = Button.new()
		pull_btn.text = "Pull x10"
		pull_btn.custom_minimum_size.x = 100
		pull_btn.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
		pull_btn.pressed.connect(func(): _do_one_time_pull(pool_ref))
		btns.add_child(pull_btn)
	else:
		# Normal pool buttons
		if pool.daily_free:
			var free_available := _is_daily_free_available(pool.id)
			var free_btn = Button.new()
			if free_available:
				free_btn.text = "FREE x1"
				free_btn.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
			else:
				free_btn.text = "Free (done)"
				free_btn.disabled = true
			free_btn.custom_minimum_size.x = 100
			free_btn.pressed.connect(func(): _do_free_pull(pool_ref))
			btns.add_child(free_btn)

		var pull1_btn = Button.new()
		pull1_btn.text = "Pull x1"
		pull1_btn.custom_minimum_size.x = 100
		pull1_btn.pressed.connect(func(): _do_pull(pool_ref, 1))
		btns.add_child(pull1_btn)

		var pull10_btn = Button.new()
		if pool.multi_pull_discount > 0:
			var pay_count: int = 10 - pool.multi_pull_discount
			pull10_btn.text = "Pull x10 (%d)" % (pool.cost_amount * pay_count)
		else:
			pull10_btn.text = "Pull x10"
		pull10_btn.custom_minimum_size.x = 100
		pull10_btn.pressed.connect(func(): _do_pull(pool_ref, 10))
		btns.add_child(pull10_btn)


func _build_exchange_section(parent: VBoxContainer) -> void:
	var exchange_defs = _game_data.get_all_definitions(&"monster_exchange")
	if exchange_defs.is_empty():
		return

	var sep = HSeparator.new()
	parent.add_child(sep)

	var header = Label.new()
	header.text = "Monster Exchange"
	header.add_theme_font_size_override("font_size", 18)
	header.add_theme_color_override("font_color", Color(0.8, 0.5, 1.0))
	parent.add_child(header)

	var hint = Label.new()
	hint.text = "Trade duplicate monsters for guaranteed targets"
	hint.add_theme_font_size_override("font_size", 11)
	parent.add_child(hint)

	var exchange := MFMonsterExchange.new(_player_state)

	for ex_data in exchange_defs:
		var offer = MFMonsterExchange.ExchangeOffer.new(ex_data.raw())
		var done: int = exchange.get_exchange_count(offer.id)
		var at_limit: bool = offer.exchange_limit > 0 and done >= offer.exchange_limit

		var target_def = _monster_manager.get_def(offer.target_monster_id)
		var target_name: String = target_def.name if target_def != null else "Monster #%d" % offer.target_monster_id

		var panel = PanelContainer.new()
		parent.add_child(panel)
		var hbox = HBoxContainer.new()
		hbox.add_theme_constant_override("separation", 8)
		panel.add_child(hbox)

		var info_vbox = VBoxContainer.new()
		info_vbox.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		hbox.add_child(info_vbox)

		var name_lbl = Label.new()
		name_lbl.text = "Get: %s" % target_name
		name_lbl.add_theme_font_size_override("font_size", 14)
		info_vbox.add_child(name_lbl)

		var req_parts: Array = ["%d monsters" % offer.required_count, "%d★+ rarity" % offer.required_min_rarity]
		if offer.exchange_limit > 0:
			req_parts.append("Limit: %d/%d" % [done, offer.exchange_limit])
		var req_lbl = Label.new()
		req_lbl.text = "Requires: " + ", ".join(req_parts)
		req_lbl.add_theme_font_size_override("font_size", 11)
		info_vbox.add_child(req_lbl)

		var ex_btn = Button.new()
		if at_limit:
			ex_btn.text = "Done"
			ex_btn.disabled = true
		else:
			ex_btn.text = "Exchange"
			ex_btn.custom_minimum_size = Vector2(80, 36)
		var o = offer
		ex_btn.pressed.connect(func(): _do_exchange(o))
		hbox.add_child(ex_btn)


func _do_exchange(offer: RefCounted) -> void:
	# Auto-select eligible monsters from inventory
	var monster_section = _player_state.get_section(&"monsters")
	if monster_section == null:
		_show_result("No monsters available")
		return

	var data: Dictionary = monster_section.to_dict()
	var candidates: Array = []
	for key in data:
		var entry = data[key]
		if entry is Dictionary:
			var inst = MFMonsterTypes.MonsterInstance.from_dict(entry)
			var def = _monster_manager.get_def(inst.def_id)
			if def == null:
				continue
			if inst.is_favorite:
				continue
			if def.rarity < offer.required_min_rarity:
				continue
			if offer.required_element > 0 and def.element != offer.required_element:
				continue
			# Don't sacrifice the target monster itself
			if inst.def_id == offer.target_monster_id:
				continue
			candidates.append(inst)

	if candidates.size() < offer.required_count:
		_show_result("Not enough eligible monsters (%d/%d)" % [candidates.size(), offer.required_count])
		return

	# Take first N candidates
	var offered: Array = candidates.slice(0, offer.required_count)

	var exchange := MFMonsterExchange.new(_player_state)
	var result = exchange.execute(offer, offered, _monster_manager)

	if not result.success:
		_show_result(result.error)
		return

	# Remove sacrificed monsters
	for inst in offered:
		monster_section.erase(StringName(str(inst.instance_id)))

	# Add received monster
	var new_inst = _monster_manager.create_instance(result.received_monster_id)
	monster_section.set_value(StringName(str(new_inst.instance_id)), new_inst.to_dict())

	var received_def = _monster_manager.get_def(result.received_monster_id)
	var name: String = received_def.name if received_def != null else "Monster #%d" % result.received_monster_id
	_show_result("Exchanged %d monsters for %s!" % [offer.required_count, name])
	_build_ui()


func _do_pull(pool: RefCounted, count: int) -> void:
	# Calculate cost with multi-pull discount
	var total_cost: int
	if count == 10 and pool.multi_pull_discount > 0:
		total_cost = pool.cost_amount * (10 - pool.multi_pull_discount)
	else:
		total_cost = pool.cost_amount * count

	if not _economy.can_afford(pool.cost_currency, total_cost):
		_show_result("Not enough %s! Need %d" % [pool.cost_currency, total_cost])
		return

	_economy.spend(pool.cost_currency, total_cost)
	_execute_pull(pool, count)


func _do_step_pull(pool: RefCounted) -> void:
	var current_step := _get_step_index(pool.id)
	if current_step >= pool.total_steps():
		_show_result("All steps completed!")
		return

	var step_def: Dictionary = pool.get_step(current_step)
	var cost_mult := float(step_def.get("cost_mult", 1.0))
	var pull_count := int(step_def.get("pull_count", 10))
	var step_cost := int(pool.cost_amount * pull_count * cost_mult)

	if step_cost > 0 and not _economy.can_afford(pool.cost_currency, step_cost):
		_show_result("Not enough %s! Need %d" % [pool.cost_currency, step_cost])
		return

	if step_cost > 0:
		_economy.spend(pool.cost_currency, step_cost)

	# Use step-aware roller
	var rng = RandomNumberGenerator.new()
	rng.randomize()
	var pity := _pity_tracker.get_pity(pool.id)
	var results = MFGachaRoller.roll_step(pool, step_def, pity, rng)

	# Update pity
	for result in results:
		if result.is_pity or _is_top_rarity(pool, result.rarity):
			_pity_tracker.reset(pool.id)
		else:
			_pity_tracker.increment(pool.id)

	# Save monsters
	var monster_section = _player_state.get_section(&"monsters")
	for result in results:
		var instance = _monster_manager.create_instance(result.monster_id)
		if monster_section != null:
			monster_section.set_value(StringName(str(instance.instance_id)), instance.to_dict())

	# Advance step
	_set_step_index(pool.id, current_step + 1)

	_show_pull_results(results)
	_currency_label.text = "Gems: %d" % _economy.get_balance("gems")
	# Rebuild to show next step
	_build_ui()


func _do_one_time_pull(pool: RefCounted) -> void:
	if _is_one_time_used(pool.id):
		_show_result("This gacha has already been used!")
		return

	var total_cost: int = pool.cost_amount * 10
	if not _economy.can_afford(pool.cost_currency, total_cost):
		_show_result("Not enough %s! Need %d" % [pool.cost_currency, total_cost])
		return

	_economy.spend(pool.cost_currency, total_cost)

	var rng = RandomNumberGenerator.new()
	rng.randomize()
	var pity := _pity_tracker.get_pity(pool.id)
	var results = MFGachaRoller.roll_multi(pool, 10, pity, rng)

	# If guaranteed_top_rarity, ensure at least one top-rarity result
	if pool.guaranteed_top_rarity:
		var has_top := false
		for r in results:
			if _is_top_rarity(pool, r.rarity):
				has_top = true
				break
		if not has_top and not results.is_empty():
			results[-1] = MFGachaRoller._roll_top_rarity(pool, rng, false)

	# Update pity
	for result in results:
		if result.is_pity or _is_top_rarity(pool, result.rarity):
			_pity_tracker.reset(pool.id)
		else:
			_pity_tracker.increment(pool.id)

	# Save monsters
	var monster_section = _player_state.get_section(&"monsters")
	for result in results:
		var instance = _monster_manager.create_instance(result.monster_id)
		if monster_section != null:
			monster_section.set_value(StringName(str(instance.instance_id)), instance.to_dict())

	# Mark as used
	_set_one_time_used(pool.id)

	_show_pull_results(results)
	_currency_label.text = "Gems: %d" % _economy.get_balance("gems")
	_build_ui()


func _do_free_pull(pool: RefCounted) -> void:
	if not _is_daily_free_available(pool.id):
		_show_result("Daily free pull already used today!")
		return

	# Mark free pull as used today
	_set_daily_free_used(pool.id)
	_execute_pull(pool, 1)
	# Rebuild UI to update button state
	_build_ui()


func _execute_pull(pool: RefCounted, count: int) -> void:
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

	# Add monsters to collection and persist to player state
	var monster_section = _player_state.get_section(&"monsters")
	for result in results:
		var instance = _monster_manager.create_instance(result.monster_id)
		if monster_section != null:
			monster_section.set_value(StringName(str(instance.instance_id)), instance.to_dict())

	# Display results
	_show_pull_results(results)
	_currency_label.text = "Gems: %d" % _economy.get_balance("gems")


func _is_daily_free_available(pool_id: int) -> bool:
	var gacha_section = _player_state.get_section(&"gacha")
	if gacha_section == null:
		return true
	var key := StringName("free_pull_%d" % pool_id)
	var last_date = gacha_section.get_value(key, "")
	if last_date == "":
		return true
	var today := _get_today_string()
	return str(last_date) != today


func _set_daily_free_used(pool_id: int) -> void:
	# Ensure gacha section exists
	if not _player_state.has_section(&"gacha"):
		_player_state.register_section(&"gacha", {})
	var gacha_section = _player_state.get_section(&"gacha")
	var key := StringName("free_pull_%d" % pool_id)
	gacha_section.set_value(key, _get_today_string())


func _get_today_string() -> String:
	var dt := Time.get_date_dict_from_system()
	return "%04d-%02d-%02d" % [dt["year"], dt["month"], dt["day"]]


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


func _get_step_index(pool_id: int) -> int:
	if not _player_state.has_section(&"gacha"):
		return 0
	var section = _player_state.get_section(&"gacha")
	return int(section.get_value(StringName("step_%d" % pool_id), 0))


func _set_step_index(pool_id: int, step: int) -> void:
	if not _player_state.has_section(&"gacha"):
		_player_state.register_section(&"gacha", {})
	var section = _player_state.get_section(&"gacha")
	section.set_value(StringName("step_%d" % pool_id), step)


func _is_one_time_used(pool_id: int) -> bool:
	if not _player_state.has_section(&"gacha"):
		return false
	var section = _player_state.get_section(&"gacha")
	return bool(section.get_value(StringName("one_time_%d" % pool_id), false))


func _set_one_time_used(pool_id: int) -> void:
	if not _player_state.has_section(&"gacha"):
		_player_state.register_section(&"gacha", {})
	var section = _player_state.get_section(&"gacha")
	section.set_value(StringName("one_time_%d" % pool_id), true)
