extends MFBaseScreen
## Battle result screen — shows win/loss and rewards.
## Victory: gold text + pop_in; Defeat: red text + shake.
## Rewards stagger-animated, TosCard for monster drops.
## Continue button with delayed entrance.

var _params: Dictionary
var _economy: MFEconomy
var _monster_manager: MFMonsterManager
var _player_state: Node
var _game_data: Node
var _ui_router: Node
var _rewards_granted: bool = false
var _btn_feedback: MFButtonFeedback


func setup(params: Dictionary, economy: MFEconomy = null, monster_manager: MFMonsterManager = null,
		player_state: Node = null, game_data: Node = null, ui_router: Node = null) -> void:
	_params = params
	_economy = economy
	_monster_manager = monster_manager
	_player_state = player_state
	_game_data = game_data
	_ui_router = ui_router


func on_enter(params: Dictionary = {}) -> void:
	var won: bool = _params.get("won", false)
	if won and not _rewards_granted:
		_roll_and_grant_rewards()
	_build_ui()


func _roll_and_grant_rewards() -> void:
	_rewards_granted = true
	var final_rewards: Array = []

	# Roll loot table if available
	var stage_id: int = int(_params.get("stage_id", 0))
	if stage_id > 0 and _game_data != null:
		var loot_tables = _game_data.get_all_definitions(&"loot_tables")
		for lt_def in loot_tables:
			if int(lt_def.get_field("stage_id", 0)) == stage_id:
				var table = MFLootTypes.LootTableDef.new(lt_def.raw())
				var rng = RandomNumberGenerator.new()
				rng.randomize()
				var drops = MFLootTable.roll_drops(table, rng, 2)
				for drop in drops:
					final_rewards.append({"type": drop.type, "item_id": drop.item_id, "count": drop.count})
				break

	# If no loot table matched, fall back to static rewards
	if final_rewards.is_empty():
		final_rewards = _params.get("rewards", [])

	# Grant each reward
	for reward in final_rewards:
		var rtype: String = str(reward.get("type", ""))
		var count: int = int(reward.get("count", 0))
		if rtype == "currency":
			var currency: String = reward.get("currency", "coins")
			if _economy != null:
				_economy.earn(currency, count)
		elif rtype == "monster":
			var monster_id: int = int(reward.get("item_id", 0))
			if monster_id > 0 and _monster_manager != null and _player_state != null:
				var inst = _monster_manager.create_instance(monster_id)
				var section = _player_state.get_section(&"monsters")
				if section != null:
					section.set_value(StringName(str(inst.instance_id)), inst.to_dict())

	_params["granted_rewards"] = final_rewards

	# Distribute EXP to team members
	var exp_gained: int = int(_params.get("exp_gained", 0))
	var team_ids: Array = _params.get("team_ids", [])
	var level_ups: Array = []  # Array of {name, old_level, new_level}
	if exp_gained > 0 and not team_ids.is_empty() and _monster_manager != null and _player_state != null:
		var section = _player_state.get_section(&"monsters")
		if section != null:
			var per_member_exp: int = exp_gained / maxi(team_ids.size(), 1)
			var owned_data: Dictionary = section.to_dict()
			for mid in team_ids:
				# Find the owned instance
				for key in owned_data:
					var data = owned_data[key]
					if data is Dictionary and int(data.get("def_id", -1)) == int(mid):
						var inst = MFMonsterTypes.MonsterInstance.from_dict(data)
						var old_level: int = inst.level
						var levels = _monster_manager.add_exp(inst, per_member_exp)
						if levels > 0:
							var def = _monster_manager.get_def(int(mid))
							var mname: String = def.name if def != null else "Monster"
							level_ups.append({"name": mname, "old_level": old_level, "new_level": inst.level})
						# Save updated instance
						section.set_value(StringName(str(key)), inst.to_dict())
						break
	_params["level_ups"] = level_ups
	_params["exp_per_member"] = int(_params.get("exp_gained", 0)) / maxi(team_ids.size(), 1) if not team_ids.is_empty() else 0


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	# Dark background
	var bg = ColorRect.new()
	bg.set_anchors_preset(Control.PRESET_FULL_RECT)
	bg.color = Color(0.05, 0.05, 0.08)
	bg.mouse_filter = MOUSE_FILTER_IGNORE
	add_child(bg)

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_CENTER)
	vbox.alignment = BoxContainer.ALIGNMENT_CENTER
	vbox.add_theme_constant_override("separation", 12)
	add_child(vbox)

	var won: bool = _params.get("won", false)

	# Result title
	var result_label = Label.new()
	var reason: String = _params.get("reason", "")
	if won:
		result_label.text = "VICTORY!"
		result_label.add_theme_color_override("font_color", Color(1.0, 0.85, 0.0))
		result_label.add_theme_color_override("font_shadow_color", Color(0.5, 0.3, 0.0, 0.7))
	elif reason == "turn_limit":
		result_label.text = "TIME'S UP!"
		result_label.add_theme_color_override("font_color", Color(1.0, 0.5, 0.2))
		result_label.add_theme_color_override("font_shadow_color", Color(0.4, 0.15, 0.0, 0.7))
	else:
		result_label.text = "DEFEATED..."
		result_label.add_theme_color_override("font_color", Color(1.0, 0.25, 0.25))
		result_label.add_theme_color_override("font_shadow_color", Color(0.4, 0.0, 0.0, 0.7))
	result_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	result_label.add_theme_font_size_override("font_size", 32)
	result_label.add_theme_constant_override("shadow_offset_x", 2)
	result_label.add_theme_constant_override("shadow_offset_y", 2)
	vbox.add_child(result_label)

	# Animated result title
	if won:
		MFUIAnim.pop_in(result_label, 0.4)
	else:
		MFUIAnim.shake(result_label, 10.0, 0.5)

	# Rewards section
	if won:
		var rewards = _params.get("granted_rewards", _params.get("rewards", []))
		if rewards.is_empty():
			var no_loot = Label.new()
			no_loot.text = "No rewards"
			no_loot.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			no_loot.add_theme_color_override("font_color", Color(0.5, 0.5, 0.6))
			vbox.add_child(no_loot)
		else:
			var rewards_title = Label.new()
			rewards_title.text = "Rewards"
			rewards_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			rewards_title.add_theme_font_size_override("font_size", 18)
			rewards_title.add_theme_color_override("font_color", Color(0.8, 0.8, 0.9))
			vbox.add_child(rewards_title)

			var reward_controls: Array = []
			for reward in rewards:
				var rtype: String = str(reward.get("type", ""))
				var count: int = int(reward.get("count", 0))

				if rtype == "monster":
					# Use TosCard for monster rewards
					var mid: int = int(reward.get("item_id", 0))
					var mdef = _monster_manager.get_def(mid) if _monster_manager != null else null
					if mdef != null:
						var card := TosCard.new()
						card.bind_def(mdef, mid)
						vbox.add_child(card)
						reward_controls.append(card)
						continue

				# Currency or other rewards as styled labels
				var reward_panel = PanelContainer.new()
				reward_panel.add_theme_stylebox_override("panel", TosTheme.make_panel(Color(0.3, 0.5, 0.3)))
				vbox.add_child(reward_panel)

				var reward_label = Label.new()
				if rtype == "currency":
					var currency: String = reward.get("currency", "coins")
					reward_label.text = "  %s x%d" % [currency, count]
				else:
					var mid: int = int(reward.get("item_id", 0))
					var mdef = _monster_manager.get_def(mid) if _monster_manager != null else null
					var mname: String = mdef.name if mdef != null else "%s #%d" % [rtype, mid]
					reward_label.text = "  %s x%d" % [mname, count]
				reward_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
				reward_label.add_theme_font_size_override("font_size", 14)
				reward_panel.add_child(reward_label)
				reward_controls.append(reward_panel)

			# Stagger animate rewards
			if not reward_controls.is_empty():
				MFUIAnim.stagger_fade_in(reward_controls, 0.25, 0.1)

	# EXP section (victory only)
	if won:
		var exp_gained: int = int(_params.get("exp_gained", 0))
		var exp_per: int = int(_params.get("exp_per_member", 0))
		if exp_gained > 0:
			var exp_panel = PanelContainer.new()
			exp_panel.add_theme_stylebox_override("panel", TosTheme.make_panel(Color(0.5, 0.4, 0.1)))
			vbox.add_child(exp_panel)
			var exp_label = Label.new()
			exp_label.text = "EXP: +%d per member" % exp_per
			exp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			exp_label.add_theme_font_size_override("font_size", 16)
			exp_label.add_theme_color_override("font_color", Color(1.0, 0.9, 0.3))
			exp_panel.add_child(exp_label)
			MFUIAnim.fade_in(exp_panel, 0.3, 0.5)

		# Level-up notifications
		var level_ups: Array = _params.get("level_ups", [])
		for lu in level_ups:
			var lu_panel = PanelContainer.new()
			lu_panel.add_theme_stylebox_override("panel", TosTheme.make_panel(Color(1.0, 0.85, 0.0)))
			vbox.add_child(lu_panel)
			var lu_label = Label.new()
			lu_label.text = "%s LEVEL UP! Lv.%d \u2192 Lv.%d" % [lu.get("name", ""), lu.get("old_level", 0), lu.get("new_level", 0)]
			lu_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			lu_label.add_theme_font_size_override("font_size", 14)
			lu_label.add_theme_color_override("font_color", Color(1.0, 1.0, 0.3))
			lu_panel.add_child(lu_label)
			MFUIAnim.pop_in(lu_panel, 0.4)

	var spacer = Control.new()
	spacer.custom_minimum_size.y = 20
	vbox.add_child(spacer)

	# Continue button with delayed entrance
	var continue_btn = Button.new()
	continue_btn.text = "Continue"
	continue_btn.custom_minimum_size = Vector2(220, 50)
	continue_btn.add_theme_font_size_override("font_size", 18)
	var btn_color := Color(0.3, 0.5, 0.8) if won else Color(0.5, 0.3, 0.3)
	var btn_style := StyleBoxFlat.new()
	btn_style.bg_color = Color(btn_color.r * 0.25, btn_color.g * 0.25, btn_color.b * 0.25, 0.9)
	btn_style.corner_radius_top_left = 8
	btn_style.corner_radius_top_right = 8
	btn_style.corner_radius_bottom_left = 8
	btn_style.corner_radius_bottom_right = 8
	btn_style.border_width_bottom = 3
	btn_style.border_color = btn_color
	btn_style.content_margin_left = 16
	btn_style.content_margin_right = 16
	btn_style.content_margin_top = 10
	btn_style.content_margin_bottom = 10
	continue_btn.add_theme_stylebox_override("normal", btn_style)
	continue_btn.pressed.connect(func():
		if _ui_router != null:
			_ui_router.navigate(&"title")
		else:
			var router = get_node_or_null("/root/TosGame")
			if router and router._ui_router:
				router._ui_router.navigate(&"title"))

	_btn_feedback = MFButtonFeedback.new(continue_btn)
	_btn_feedback.add_scale(0.94)

	vbox.add_child(continue_btn)

	# Delayed entrance for continue button (after rewards animate in)
	continue_btn.modulate.a = 0.0
	MFUIAnim.fade_in(continue_btn, 0.3, 0.8)
