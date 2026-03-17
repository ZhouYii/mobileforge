extends MFBaseScreen
## Main battle screen. Uses BoardView for interactive gem board,
## EnemyView for enemy display, ComboDisplay for cascade feedback,
## and DamageLabel for floating damage numbers.
## Styled with TosTheme: MFProgressBar HP, element-colored skill buttons,
## dark panels, and ToS-accurate animation timings.

const BoardView := preload("res://games/tower-of-saviors/godot/game/battle/board_view.gd")
const ComboDisplay := preload("res://games/tower-of-saviors/godot/game/battle/combo_display.gd")
const DamageLabel := preload("res://games/tower-of-saviors/godot/game/battle/damage_label.gd")
const EnemyView := preload("res://games/tower-of-saviors/godot/game/battle/enemy_view.gd")

var _game_data: Node
var _monster_manager: MFMonsterManager
var _skill_pipeline: MFSkillPipeline
var _economy: MFEconomy
var _event_bus: Node
var _ui_router: Node
var _player_state: Node
var _params: Dictionary

var _board: MFBoardLogic
var _combat: MFCombatResolver
var _dungeon_runner: MFDungeonRunner

# Team data built from team_ids on battle start
var _team: Array = []        # Array[MonsterDef]
var _team_stats: Array = []  # Array[MonsterStats]
var _team_instances: Array = []  # Array[MonsterInstance] (for skill cooldowns)
var _skill_cooldowns: Array = []  # Array[int] — current cooldown per team slot

# UI references
var _board_view: Control
var _combo_display: Control
var _enemy_hbox: HBoxContainer
var _enemy_views: Array = []  # Array[EnemyView]
var _hp_bar: MFProgressBar
var _hp_label: Label
var _wave_label: Label
var _turn_label: Label
var _shuffle_btn: Button
var _skill_btn_container: HBoxContainer
var _skill_btns: Array = []  # Array[Button]
var _skill_feedbacks: Array = []  # Array[MFButtonFeedback]
var _damage_layer: Control  # Overlay for floating damage labels
var _is_enemy_turn: bool = false
var _header_panel: PanelContainer


func setup(game_data: Node, monster_manager: MFMonsterManager, skill_pipeline: MFSkillPipeline,
		economy: MFEconomy, event_bus: Node, ui_router: Node, params: Dictionary,
		player_state: Node = null) -> void:
	_game_data = game_data
	_monster_manager = monster_manager
	_skill_pipeline = skill_pipeline
	_economy = economy
	_event_bus = event_bus
	_ui_router = ui_router
	_player_state = player_state
	_params = params


func on_enter(params: Dictionary = {}) -> void:
	# Spend stamina
	var stamina_cost := int(_params.get("stamina_cost", 10))
	_economy.spend_stamina(stamina_cost)

	# Build team from selected monster IDs
	_build_team()

	# Load dungeon def first (needed for board size)
	var stage_id := int(_params.get("stage_id", 1))
	var stage_def_data = _game_data.get_definition(&"stages", stage_id)
	var dungeon_def: RefCounted = null
	if stage_def_data != null:
		dungeon_def = MFDungeonTypes.DungeonDef.new(stage_def_data.raw())

	# Create board — use dungeon-specific size or default 5x6
	var board_rows := 5
	var board_cols := 6
	if dungeon_def != null:
		if dungeon_def.board_rows > 0:
			board_rows = dungeon_def.board_rows
		if dungeon_def.board_cols > 0:
			board_cols = dungeon_def.board_cols
	var config = MFBoardConfig.new(board_rows, board_cols)
	_board = MFBoardLogic.new(config)
	_board.init_board()

	# Create combat resolver with element chart
	var chart = MFElementChart.new()
	_combat = MFCombatResolver.new(chart)

	# Create dungeon runner
	_dungeon_runner = MFDungeonRunner.new(_board, _combat, _skill_pipeline, _event_bus)

	# Start dungeon
	if dungeon_def != null:
		var team_hp := _calculate_team_hp()
		_dungeon_runner.start_dungeon(dungeon_def, team_hp, team_hp)

	# Evaluate and apply team/leader skills
	_apply_team_skills()
	_apply_leader_skill()
	_apply_friend_leader_skill()

	# Initialize skill cooldowns for active skills
	_init_skill_cooldowns()

	_build_ui()


func _build_team() -> void:
	_team.clear()
	_team_stats.clear()
	_team_instances.clear()
	var team_ids: Array = _params.get("team_ids", [])
	# Look up player-owned monster instances for accurate levels/stats
	var owned_section = null
	if _player_state != null and _player_state.has_method("get_section"):
		owned_section = _player_state.get_section(&"monsters")
	for mid in team_ids:
		var def = _monster_manager.get_def(int(mid))
		# Try to find the actual player-owned instance with real level/stats
		var inst = _find_owned_instance(owned_section, int(mid))
		if inst == null:
			inst = _monster_manager.create_instance(int(mid))  # fallback: level 1
		_team.append(def)
		_team_instances.append(inst)
		if def != null:
			_team_stats.append(_monster_manager.get_stats(inst))
		else:
			_team_stats.append(MFMonsterTypes.MonsterStats.new(100, 50, 20))

	# Append friend helper as last team slot
	var helper_id := int(_params.get("helper_id", -1))
	if helper_id > 0:
		var helper_def = _monster_manager.get_def(helper_id)
		var helper_inst = _monster_manager.create_instance(helper_id)
		if helper_def != null:
			helper_inst.level = helper_def.max_level
			_team.append(helper_def)
			_team_instances.append(helper_inst)
			_team_stats.append(_monster_manager.get_stats(helper_inst))
		else:
			var fallback_stats = MFMonsterTypes.MonsterStats.new(100, 50, 20)
			_team.append(null)
			_team_instances.append(helper_inst)
			_team_stats.append(fallback_stats)


func _find_owned_instance(section: RefCounted, def_id: int) -> RefCounted:
	# Search player-owned monsters (MFStateSection) for one matching this def_id
	if section == null:
		return null
	for key in section.keys():
		var data = section.get_value(StringName(str(key)))
		if data is Dictionary and int(data.get("def_id", -1)) == def_id:
			return MFMonsterTypes.MonsterInstance.from_dict(data)
	return null


func _calculate_team_hp() -> int:
	var total_hp := 0
	for stats in _team_stats:
		if stats != null:
			total_hp += stats.hp
	return maxi(total_hp, 100)  # floor at 100 HP


func _apply_team_skills() -> void:
	## Load team skill definitions and check which activate for the current team.
	var team_ids: Array = _params.get("team_ids", [])
	if team_ids.is_empty():
		return

	# Build team data for skill context
	var team_data: Array = []
	for mid in team_ids:
		var data: Dictionary = _game_data.get_definition(&"monsters", int(mid)).raw() if _game_data.get_definition(&"monsters", int(mid)) != null else {}
		team_data.append(data)

	# Get all team skill definitions
	var team_skill_defs = _game_data.get_all_definitions(&"team_skills")
	if team_skill_defs == null or team_skill_defs.is_empty():
		return

	# Build a skill context with team info
	var ctx = MFSkillTypes.SkillContext.new()
	ctx.team = team_data

	for ts_data in team_skill_defs:
		var raw: Dictionary = ts_data.raw() if ts_data.has_method("raw") else ts_data
		var skill_def = _skill_pipeline.load_skill_def(raw)
		var result = _skill_pipeline.activate_skill(skill_def, ctx)

		# Apply stat multipliers from buffs_applied to dungeon runner context
		for buff in result.buffs_applied:
			var buff_type: String = buff.get("type", "")
			if buff_type == "element_atk_mult" or buff_type == "element_hp_mult" or buff_type == "element_rec_mult":
				if _dungeon_runner.state != null:
					if not _dungeon_runner.state.has("team_buffs"):
						_dungeon_runner.state.team_buffs = []
					_dungeon_runner.state.team_buffs.append(buff)


func _apply_leader_skill() -> void:
	## Evaluate the team leader's (slot 0) leader skill.
	if _team.is_empty() or _team[0] == null:
		return
	var leader_def = _team[0]
	var leader_skill_id: int = leader_def.leader_skill_id
	if leader_skill_id < 0:
		return

	var ls_data = _game_data.get_definition(&"leader_skills", leader_skill_id)
	if ls_data == null:
		return

	var ctx = MFSkillTypes.SkillContext.new()
	ctx.team = _team
	ctx.team_stats = _team_stats

	var skill_def = _skill_pipeline.load_skill_def(ls_data.raw())
	var result = _skill_pipeline.activate_skill(skill_def, ctx)

	# Apply leader skill buffs to dungeon runner state
	for buff in result.buffs_applied:
		var buff_type: String = buff.get("type", "")
		if buff_type in ["element_atk_mult", "element_hp_mult", "element_rec_mult"]:
			if _dungeon_runner.state != null:
				if not _dungeon_runner.state.has("team_buffs"):
					_dungeon_runner.state.team_buffs = []
				_dungeon_runner.state.team_buffs.append(buff)


func _apply_friend_leader_skill() -> void:
	## Evaluate the friend helper's (last slot) leader skill.
	if _team.size() < 2:
		return
	var friend_def = _team[_team.size() - 1]
	if friend_def == null:
		return
	var friend_leader_skill_id: int = friend_def.leader_skill_id
	if friend_leader_skill_id < 0:
		return

	var ls_data = _game_data.get_definition(&"leader_skills", friend_leader_skill_id)
	if ls_data == null:
		return

	var ctx = MFSkillTypes.SkillContext.new()
	ctx.team = _team
	ctx.team_stats = _team_stats

	var skill_def = _skill_pipeline.load_skill_def(ls_data.raw())
	var result = _skill_pipeline.activate_skill(skill_def, ctx)

	# Apply friend leader skill buffs to dungeon runner state
	for buff in result.buffs_applied:
		var buff_type: String = buff.get("type", "")
		if buff_type in ["element_atk_mult", "element_hp_mult", "element_rec_mult"]:
			if _dungeon_runner.state != null:
				if not _dungeon_runner.state.has("team_buffs"):
					_dungeon_runner.state.team_buffs = []
				_dungeon_runner.state.team_buffs.append(buff)


func _init_skill_cooldowns() -> void:
	_skill_cooldowns.clear()
	for i in range(_team.size()):
		var def = _team[i]
		if def != null and def.active_skill_id >= 0:
			var skill_data = _game_data.get_definition(&"skills", def.active_skill_id)
			if skill_data != null:
				var max_cd := int(skill_data.get_field("max_cd", 10))
				var min_cd := int(skill_data.get_field("min_cd", 5))
				var skill_level: int = _team_instances[i].skill_level if i < _team_instances.size() else 1
				_skill_cooldowns.append(MFSkillCooldown.calculate_cd(max_cd, min_cd, skill_level))
			else:
				_skill_cooldowns.append(0)
		else:
			_skill_cooldowns.append(0)


func _build_skill_buttons() -> void:
	_skill_btns.clear()
	_skill_feedbacks.clear()
	for child in _skill_btn_container.get_children():
		child.queue_free()

	for i in range(_team.size()):
		var def = _team[i]
		if def == null or def.active_skill_id < 0:
			continue
		var skill_data = _game_data.get_definition(&"skills", def.active_skill_id)
		if skill_data == null:
			continue

		var btn = Button.new()
		btn.custom_minimum_size = TosTheme.SKILL_BTN_SIZE
		btn.add_theme_font_size_override("font_size", TosTheme.SKILL_BTN_FONT_SIZE)
		btn.pressed.connect(_on_skill_pressed.bind(i))

		# Element-colored button style
		var element: int = def.element if def != null else 0
		var btn_style := TosTheme.make_button_style(element)
		btn.add_theme_stylebox_override("normal", btn_style)

		# Button feedback
		var feedback := MFButtonFeedback.new(btn)
		feedback.add_scale(0.92)
		_skill_feedbacks.append(feedback)

		_skill_btn_container.add_child(btn)
		_skill_btns.append({"button": btn, "slot": i, "skill_data": skill_data})

	_refresh_skill_buttons()


func _refresh_skill_buttons() -> void:
	var skills_blocked := false
	if _dungeon_runner.state != null and _dungeon_runner.state.dungeon_def != null:
		skills_blocked = _dungeon_runner.state.dungeon_def.has_floor_effect("no_active_skills")

	for entry in _skill_btns:
		var btn: Button = entry["button"]
		var slot: int = entry["slot"]
		var skill_data = entry["skill_data"]
		var cd: int = _skill_cooldowns[slot] if slot < _skill_cooldowns.size() else 99
		var skill_name: String = str(skill_data.get_field("name", "Skill"))
		if skills_blocked:
			btn.text = skill_name + "\n(SEALED)"
			btn.disabled = true
			btn.modulate = Color(0.4, 0.3, 0.3)
		elif MFSkillCooldown.is_ready(cd):
			btn.text = skill_name
			btn.disabled = _is_enemy_turn
			btn.modulate = Color(TosTheme.SKILL_ACTIVE_SCALE, TosTheme.SKILL_ACTIVE_SCALE, TosTheme.SKILL_ACTIVE_SCALE)
		else:
			btn.text = "%s\n(%d)" % [skill_name, cd]
			btn.disabled = true
			btn.modulate = Color(0.5, 0.5, 0.5)


func _on_skill_pressed(slot_index: int) -> void:
	if _is_enemy_turn or slot_index >= _skill_cooldowns.size():
		return
	# Floor effect: no active skills
	if _dungeon_runner.state != null and _dungeon_runner.state.dungeon_def != null:
		if _dungeon_runner.state.dungeon_def.has_floor_effect("no_active_skills"):
			return
	if not MFSkillCooldown.is_ready(_skill_cooldowns[slot_index]):
		return

	var def = _team[slot_index]
	if def == null or def.active_skill_id < 0:
		return

	var skill_data = _game_data.get_definition(&"skills", def.active_skill_id)
	if skill_data == null:
		return

	# Build skill context
	var ctx = MFSkillTypes.SkillContext.new()
	ctx.caster_index = slot_index
	ctx.team = _team
	ctx.team_stats = _team_stats
	ctx.board = _board
	ctx.combat = _combat
	ctx.enemies = _dungeon_runner.state.enemies if _dungeon_runner.state != null else []
	ctx.team_hp = _dungeon_runner.state.team_hp if _dungeon_runner.state != null else 0
	ctx.max_hp = _dungeon_runner.state.max_hp if _dungeon_runner.state != null else 0

	var skill_def = _skill_pipeline.load_skill_def(skill_data.raw())
	var result = _dungeon_runner.activate_skill(skill_def, ctx)

	# Reset cooldown to max
	var max_cd := int(skill_data.get_field("max_cd", 10))
	var min_cd := int(skill_data.get_field("min_cd", 5))
	var skill_level: int = _team_instances[slot_index].skill_level if slot_index < _team_instances.size() else 1
	_skill_cooldowns[slot_index] = MFSkillCooldown.calculate_cd(max_cd, min_cd, skill_level)

	# Show effects
	if result.healing > 0:
		_dungeon_runner.state.team_hp = mini(_dungeon_runner.state.team_hp + result.healing, _dungeon_runner.state.max_hp)
		_spawn_heal_label(result.healing)
	for enemy_idx in result.damage_dealt:
		var dmg: int = result.damage_dealt[enemy_idx]
		if dmg > 0 and enemy_idx < _dungeon_runner.state.enemies.size():
			_dungeon_runner.state.enemies[enemy_idx].take_damage(dmg)
			_spawn_damage_label(dmg, enemy_idx, Color.CYAN)

	_update_enemy_views()
	_refresh_hp()
	_refresh_skill_buttons()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.add_theme_constant_override("separation", 6)
	add_child(vbox)

	# --- Top header panel (dark, 0.5s slide-in) ---
	_header_panel = PanelContainer.new()
	var header_style := TosTheme.make_panel(Color(0.3, 0.3, 0.4))
	_header_panel.add_theme_stylebox_override("panel", header_style)
	vbox.add_child(_header_panel)

	var top_row = HBoxContainer.new()
	top_row.alignment = BoxContainer.ALIGNMENT_CENTER
	top_row.add_theme_constant_override("separation", 16)
	_header_panel.add_child(top_row)

	_wave_label = Label.new()
	_wave_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_wave_label.add_theme_font_size_override("font_size", 16)
	_wave_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))
	_wave_label.add_theme_constant_override("shadow_offset_x", 1)
	_wave_label.add_theme_constant_override("shadow_offset_y", 1)
	top_row.add_child(_wave_label)

	_turn_label = Label.new()
	_turn_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_turn_label.add_theme_font_size_override("font_size", 14)
	top_row.add_child(_turn_label)

	# Header slide-in animation (0.5s from ToS top bar timing)
	MFUIAnim.slide_in_from(_header_panel, Vector2(0, -40), TosTheme.ANIM_PANEL)

	# --- Enemy zone (min 140px, ~30% layout) ---
	_enemy_hbox = HBoxContainer.new()
	_enemy_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_enemy_hbox.custom_minimum_size.y = 140
	_enemy_hbox.add_theme_constant_override("separation", 8)
	vbox.add_child(_enemy_hbox)

	# --- Combo display (overlaid on enemy area) ---
	_combo_display = ComboDisplay.new()
	_combo_display.custom_minimum_size = Vector2(200, 50)
	_combo_display.set_anchors_preset(Control.PRESET_CENTER_TOP)
	add_child(_combo_display)
	_combo_display.position.y = 60

	# --- Team HP bar (full width, 24px, 4-layer MFProgressBar) ---
	var hp_container = HBoxContainer.new()
	hp_container.alignment = BoxContainer.ALIGNMENT_CENTER
	hp_container.add_theme_constant_override("separation", 8)
	vbox.add_child(hp_container)

	var hp_title = Label.new()
	hp_title.text = "HP"
	hp_title.add_theme_font_size_override("font_size", 14)
	hp_title.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))
	hp_title.add_theme_constant_override("shadow_offset_x", 1)
	hp_title.add_theme_constant_override("shadow_offset_y", 1)
	hp_container.add_child(hp_title)

	_hp_bar = MFProgressBar.new()
	_hp_bar.animation_duration = TosTheme.ANIM_HP_BAR
	_hp_bar.setup(280, TosTheme.HP_BAR_HEIGHT)
	_hp_bar.set_colors(Color(0.2, 0.8, 0.2))
	hp_container.add_child(_hp_bar)

	_hp_label = Label.new()
	_hp_label.add_theme_font_size_override("font_size", 12)
	_hp_label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.5))
	_hp_label.add_theme_constant_override("shadow_offset_x", 1)
	_hp_label.add_theme_constant_override("shadow_offset_y", 1)
	hp_container.add_child(_hp_label)

	# --- Board view (interactive gem grid) ---
	var board_center = CenterContainer.new()
	board_center.size_flags_vertical = Control.SIZE_EXPAND_FILL
	vbox.add_child(board_center)

	_board_view = BoardView.new()
	_board_view.setup(_board)
	_board_view.turn_completed.connect(_on_turn_completed)
	_board_view.cascade_animating.connect(_on_cascade_animating)
	board_center.add_child(_board_view)

	# --- Active skill buttons (element-colored, 80x44) ---
	_skill_btn_container = HBoxContainer.new()
	_skill_btn_container.alignment = BoxContainer.ALIGNMENT_CENTER
	_skill_btn_container.add_theme_constant_override("separation", 4)
	vbox.add_child(_skill_btn_container)
	_build_skill_buttons()

	# --- Shuffle button ---
	_shuffle_btn = Button.new()
	_shuffle_btn.text = "Shuffle"
	_shuffle_btn.custom_minimum_size = Vector2(0, 40)
	_shuffle_btn.pressed.connect(_on_shuffle_pressed)
	var shuffle_style := TosTheme.make_panel(Color(0.4, 0.4, 0.5))
	_shuffle_btn.add_theme_stylebox_override("normal", shuffle_style)
	var shuffle_feedback := MFButtonFeedback.new(_shuffle_btn)
	shuffle_feedback.add_scale(0.95)
	vbox.add_child(_shuffle_btn)

	# --- Damage label overlay (sits on top of everything) ---
	_damage_layer = Control.new()
	_damage_layer.set_anchors_preset(Control.PRESET_FULL_RECT)
	_damage_layer.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_damage_layer.z_index = 20
	add_child(_damage_layer)

	# Populate with current state
	_refresh_enemies()
	_refresh_hp()


func _refresh_enemies() -> void:
	# Clear existing enemy views
	for child in _enemy_hbox.get_children():
		child.queue_free()
	_enemy_views.clear()

	if _dungeon_runner.state == null:
		return

	_wave_label.text = "Wave %d" % (_dungeon_runner.state.current_wave_index + 1)

	for enemy in _dungeon_runner.state.enemies:
		var ev = EnemyView.new()
		_enemy_hbox.add_child(ev)
		ev.update_from_enemy(enemy)
		ev.play_enter()
		_enemy_views.append(ev)


func _refresh_hp() -> void:
	if _dungeon_runner.state == null:
		return

	var ratio := float(_dungeon_runner.state.team_hp) / float(_dungeon_runner.state.max_hp) if _dungeon_runner.state.max_hp > 0 else 0.0
	_hp_bar.set_value(ratio)
	_hp_label.text = "%d / %d" % [_dungeon_runner.state.team_hp, _dungeon_runner.state.max_hp]

	# Overlaid HP text on the bar itself
	_hp_bar.set_text("%d / %d" % [_dungeon_runner.state.team_hp, _dungeon_runner.state.max_hp])
	_hp_bar.set_text_visible(true)

	# Color HP bar based on ratio
	if ratio > 0.5:
		_hp_bar.set_colors(Color(0.2, 0.8, 0.2))
	elif ratio > 0.2:
		_hp_bar.set_colors(Color(0.9, 0.8, 0.1))
	else:
		_hp_bar.set_colors(Color(0.9, 0.15, 0.15))

	# Update turn counter
	var turn_limit: int = _dungeon_runner.state.dungeon_def.turn_limit if _dungeon_runner.state.dungeon_def != null else 0
	if turn_limit > 0:
		var remaining: int = turn_limit - _dungeon_runner.state.turn_number
		_turn_label.text = "Turn %d/%d" % [_dungeon_runner.state.turn_number, turn_limit]
		if remaining <= 3:
			_turn_label.add_theme_color_override("font_color", Color.RED)
		else:
			_turn_label.remove_theme_color_override("font_color")
	else:
		_turn_label.text = "Turn %d" % _dungeon_runner.state.turn_number


func _update_enemy_views() -> void:
	if _dungeon_runner.state == null:
		return
	for i in range(mini(_enemy_views.size(), _dungeon_runner.state.enemies.size())):
		_enemy_views[i].update_from_enemy(_dungeon_runner.state.enemies[i])


func _spawn_damage_label(damage: int, enemy_index: int, color: Color = Color.WHITE) -> void:
	if enemy_index < 0 or enemy_index >= _enemy_views.size():
		return
	var enemy_view: Control = _enemy_views[enemy_index]
	var label = DamageLabel.new()
	_damage_layer.add_child(label)
	# Position above the enemy view
	label.position = enemy_view.global_position - _damage_layer.global_position + Vector2(
		enemy_view.size.x / 2 - 30,
		-10
	)
	label.setup(damage, color)


func _spawn_heal_label(amount: int) -> void:
	if amount <= 0:
		return
	var label = DamageLabel.new()
	_damage_layer.add_child(label)
	# Position near HP bar
	label.position = _hp_bar.global_position - _damage_layer.global_position + Vector2(
		_hp_bar.size.x / 2 - 20,
		-20
	)
	label.setup(amount, Color.GREEN)


func _spawn_enemy_damage_label(damage: int) -> void:
	var label = DamageLabel.new()
	_damage_layer.add_child(label)
	# Position near HP bar
	label.position = _hp_bar.global_position - _damage_layer.global_position + Vector2(
		_hp_bar.size.x / 2 - 30,
		-20
	)
	label.setup(damage, Color.RED)


## --- Signal Handlers ---

func _on_cascade_animating(animating: bool) -> void:
	_shuffle_btn.disabled = animating
	if animating:
		# Count total combos for the display — will be updated per-step in turn_completed
		pass


func _on_turn_completed(cascade_steps: Array) -> void:
	# Show combo count
	var total_combos := 0
	for step in cascade_steps:
		total_combos += step.matches.size()
	if total_combos > 0:
		_combo_display.show_combo(total_combos)

	# Disable board input during resolution
	_board_view.set_input_enabled(false)
	_shuffle_btn.disabled = true

	# Tick skill cooldowns each turn
	for i in range(_skill_cooldowns.size()):
		_skill_cooldowns[i] = MFSkillCooldown.tick(_skill_cooldowns[i])

	# Execute player turn with actual team data
	var result = _dungeon_runner.execute_player_turn(cascade_steps, _team, _team_stats)

	# Spawn damage labels for each enemy that took damage
	for enemy_idx in result.damage_per_enemy:
		var dmg: int = result.damage_per_enemy[enemy_idx]
		if dmg > 0:
			# Color by the dominant element matched
			_spawn_damage_label(dmg, enemy_idx)

	# Show healing
	if result.healing > 0:
		_spawn_heal_label(result.healing)

	# Update enemy views with new HP
	_update_enemy_views()
	_refresh_hp()
	_refresh_skill_buttons()

	# Check if battle ended after player turn
	if not _dungeon_runner.state.is_active:
		_on_battle_end(result)
		return

	# Handle wave clear
	if result.wave_cleared:
		_on_wave_cleared()
		return

	# Execute enemy turn after a brief delay
	_execute_enemy_turn_delayed()


func _execute_enemy_turn_delayed() -> void:
	_is_enemy_turn = true
	var timer = get_tree().create_timer(0.8)
	timer.timeout.connect(_execute_enemy_turn)


func _execute_enemy_turn() -> void:
	var attacks = _dungeon_runner.execute_enemy_turn()

	# Show damage from each attacking enemy
	for attack in attacks:
		var dmg: int = attack["damage"]
		if dmg > 0:
			_spawn_enemy_damage_label(dmg)

	# Update views
	_update_enemy_views()
	_refresh_hp()

	_is_enemy_turn = false

	# Check if player lost
	if not _dungeon_runner.state.is_active:
		# Lost
		var result = MFDungeonTypes.TurnResult.new()
		result.battle_ended = true
		result.battle_won = false
		_on_battle_end(result)
		return

	# Re-enable board input
	_board_view.set_input_enabled(true)
	_shuffle_btn.disabled = false
	_refresh_skill_buttons()


func _on_wave_cleared() -> void:
	# Brief pause before showing new wave
	var timer = get_tree().create_timer(1.0)
	timer.timeout.connect(func():
		_refresh_enemies()
		_refresh_hp()
		_board_view.set_input_enabled(true)
		_shuffle_btn.disabled = false)


func _on_battle_end(result: RefCounted) -> void:
	# Disable everything
	_board_view.set_input_enabled(false)
	_shuffle_btn.disabled = true

	# Navigate to result screen after a brief delay
	var stage_id := int(_params.get("stage_id", 0))
	var timer = get_tree().create_timer(1.5)
	timer.timeout.connect(func():
		if result.battle_won:
			_ui_router.navigate(&"result", {"won": true, "rewards": result.rewards, "stage_id": stage_id, "exp_gained": result.exp_gained, "team_ids": _params.get("team_ids", [])})
		elif result.turn_limit_exceeded:
			_ui_router.navigate(&"result", {"won": false, "reason": "turn_limit"})
		else:
			_ui_router.navigate(&"result", {"won": false}))


func _on_shuffle_pressed() -> void:
	if _board_view.is_animating() or _is_enemy_turn:
		return
	# Reshuffle the board
	_board.init_board()
	_board_view.setup(_board)
