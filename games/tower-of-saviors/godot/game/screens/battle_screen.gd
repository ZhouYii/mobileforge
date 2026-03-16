extends MFBaseScreen
## Main battle screen. Uses BoardView for interactive gem board,
## EnemyView for enemy display, ComboDisplay for cascade feedback,
## and DamageLabel for floating damage numbers.

const BoardView := preload("res://game/battle/board_view.gd")
const ComboDisplay := preload("res://game/battle/combo_display.gd")
const DamageLabel := preload("res://game/battle/damage_label.gd")
const EnemyView := preload("res://game/battle/enemy_view.gd")

var _game_data: Node
var _monster_manager: MFMonsterManager
var _skill_pipeline: MFSkillPipeline
var _economy: MFEconomy
var _event_bus: Node
var _ui_router: Node
var _params: Dictionary

var _board: MFBoardLogic
var _combat: MFCombatResolver
var _dungeon_runner: MFDungeonRunner

# UI references
var _board_view: Control
var _combo_display: Control
var _enemy_hbox: HBoxContainer
var _enemy_views: Array = []  # Array[EnemyView]
var _hp_bar: ProgressBar
var _hp_label: Label
var _wave_label: Label
var _shuffle_btn: Button
var _damage_layer: Control  # Overlay for floating damage labels
var _is_enemy_turn: bool = false


func setup(game_data: Node, monster_manager: MFMonsterManager, skill_pipeline: MFSkillPipeline,
		economy: MFEconomy, event_bus: Node, ui_router: Node, params: Dictionary) -> void:
	_game_data = game_data
	_monster_manager = monster_manager
	_skill_pipeline = skill_pipeline
	_economy = economy
	_event_bus = event_bus
	_ui_router = ui_router
	_params = params


func on_enter(params: Dictionary = {}) -> void:
	# Spend stamina
	var stamina_cost := int(_params.get("stamina_cost", 10))
	_economy.spend_stamina(stamina_cost)

	# Create board
	var config = MFBoardConfig.new(5, 6)
	_board = MFBoardLogic.new(config)
	_board.init_board()

	# Create combat resolver with element chart
	var chart = MFElementChart.new()
	_combat = MFCombatResolver.new(chart)

	# Create dungeon runner
	_dungeon_runner = MFDungeonRunner.new(_board, _combat, _skill_pipeline, _event_bus)

	# Load dungeon
	var stage_id := int(_params.get("stage_id", 1))
	var stage_def_data = _game_data.get_definition(&"stages", stage_id)
	if stage_def_data != null:
		var dungeon_def = MFDungeonTypes.DungeonDef.new(stage_def_data.raw())
		# Calculate team HP (simplified: use fixed value for now)
		_dungeon_runner.start(dungeon_def, 10000, 10000)

	_build_ui()


func _build_ui() -> void:
	for child in get_children():
		child.queue_free()

	var vbox = VBoxContainer.new()
	vbox.set_anchors_preset(Control.PRESET_FULL_RECT)
	vbox.add_theme_constant_override("separation", 8)
	add_child(vbox)

	# --- Wave label ---
	_wave_label = Label.new()
	_wave_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_wave_label.add_theme_font_size_override("font_size", 16)
	vbox.add_child(_wave_label)

	# --- Enemy panel (HBox of EnemyViews) ---
	_enemy_hbox = HBoxContainer.new()
	_enemy_hbox.alignment = BoxContainer.ALIGNMENT_CENTER
	_enemy_hbox.custom_minimum_size.y = 110
	_enemy_hbox.add_theme_constant_override("separation", 12)
	vbox.add_child(_enemy_hbox)

	# --- Combo display (overlaid on enemy area) ---
	_combo_display = ComboDisplay.new()
	_combo_display.custom_minimum_size = Vector2(200, 40)
	_combo_display.set_anchors_preset(Control.PRESET_CENTER_TOP)
	add_child(_combo_display)
	_combo_display.position.y = 60

	# --- Team HP bar ---
	var hp_container = HBoxContainer.new()
	hp_container.alignment = BoxContainer.ALIGNMENT_CENTER
	hp_container.add_theme_constant_override("separation", 8)
	vbox.add_child(hp_container)

	var hp_title = Label.new()
	hp_title.text = "HP"
	hp_title.add_theme_font_size_override("font_size", 14)
	hp_container.add_child(hp_title)

	_hp_bar = ProgressBar.new()
	_hp_bar.custom_minimum_size = Vector2(200, 16)
	_hp_bar.show_percentage = false
	hp_container.add_child(_hp_bar)

	_hp_label = Label.new()
	_hp_label.add_theme_font_size_override("font_size", 12)
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

	# --- Shuffle button ---
	_shuffle_btn = Button.new()
	_shuffle_btn.text = "Shuffle"
	_shuffle_btn.custom_minimum_size = Vector2(0, 44)
	_shuffle_btn.pressed.connect(_on_shuffle_pressed)
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
		_enemy_views.append(ev)


func _refresh_hp() -> void:
	if _dungeon_runner.state == null:
		return
	_hp_bar.max_value = _dungeon_runner.state.max_hp
	_hp_bar.value = _dungeon_runner.state.team_hp
	_hp_label.text = "%d / %d" % [_dungeon_runner.state.team_hp, _dungeon_runner.state.max_hp]

	# Color HP bar
	var ratio := float(_dungeon_runner.state.team_hp) / float(_dungeon_runner.state.max_hp) if _dungeon_runner.state.max_hp > 0 else 0.0
	if ratio > 0.5:
		_hp_bar.modulate = Color.GREEN
	elif ratio > 0.2:
		_hp_bar.modulate = Color.YELLOW
	else:
		_hp_bar.modulate = Color.RED


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

	# Execute player turn (simplified: empty team for now)
	var team: Array = []
	var team_stats: Array = []
	var result = _dungeon_runner.execute_player_turn(cascade_steps, team, team_stats)

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
	var timer = get_tree().create_timer(1.5)
	timer.timeout.connect(func():
		if result.battle_won:
			_ui_router.navigate(&"result", {"won": true, "rewards": result.rewards})
		else:
			_ui_router.navigate(&"result", {"won": false}))


func _on_shuffle_pressed() -> void:
	if _board_view.is_animating() or _is_enemy_turn:
		return
	# Reshuffle the board
	_board.init_board()
	_board_view.setup(_board)
