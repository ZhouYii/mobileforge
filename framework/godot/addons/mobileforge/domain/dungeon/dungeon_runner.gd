class_name MFDungeonRunner extends RefCounted
## Orchestrates a dungeon run: start, turn loop, wave progression, end.
## This is the ONLY domain module that sequences calls to other domain modules.

var state: RefCounted  # DungeonState
var _board: RefCounted  # MFBoardLogic
var _combat: RefCounted  # MFCombatResolver
var _skill_pipeline: RefCounted  # MFSkillPipeline
var _event_bus: Object  # EventBus node (nullable for testing)


func _init(board: RefCounted, combat: RefCounted, skill_pipeline: RefCounted, event_bus: Object = null) -> void:
	_board = board
	_combat = combat
	_skill_pipeline = skill_pipeline
	_event_bus = event_bus


## Start a new dungeon run
func start(dungeon_def: RefCounted, team_hp: int, max_hp: int) -> void:
	state = MFDungeonTypes.DungeonState.new()
	state.dungeon_def = dungeon_def
	state.team_hp = team_hp
	state.max_hp = max_hp
	state.is_active = true
	_load_wave(0)
	_emit("dungeon_started", {"dungeon_id": dungeon_def.id})


## Execute the player's turn after gems have been matched.
## cascade_steps: Array[CascadeStep] from BoardLogic.resolve_cascade()
func execute_player_turn(cascade_steps: Array, team: Array, team_stats: Array) -> RefCounted:  # -> TurnResult
	var result = MFDungeonTypes.TurnResult.new()
	state.turn_number += 1

	# 1. Aggregate matches
	var elements_matched: Dictionary = {}
	var total_combos := 0
	for step in cascade_steps:
		for match_result in step.matches:
			total_combos += 1
			var elem: int = match_result.element
			if not elements_matched.has(elem):
				elements_matched[elem] = 0
			elements_matched[elem] += match_result.gem_count

	state.total_combos += total_combos

	# 2. Process skill pipeline turn start
	var skill_ctx := _make_skill_context(team, team_stats, elements_matched, total_combos)
	_skill_pipeline.process_turn_start(skill_ctx)

	# 3. For each combo, for each team monster, resolve damage
	var combo_index := 0
	for step in cascade_steps:
		for match_result in step.matches:
			for i in range(team.size()):
				if team_stats[i] == null:
					continue
				# Heart gems heal, not damage
				if match_result.element == MFBoardTypes.Element.HEART:
					continue

				for enemy_idx in range(state.enemies.size()):
					var enemy = state.enemies[enemy_idx]
					if not enemy.is_alive:
						continue

					var ctx = MFCombatTypes.DamageContext.new()
					ctx.attacker_element = match_result.element
					ctx.defender_element = enemy.element
					ctx.attacker_atk = team_stats[i].atk
					ctx.defender_defense = enemy.defense
					ctx.gems_matched = match_result.gem_count
					ctx.combo_count = total_combos
					ctx.combo_index = combo_index

					var dmg_result = _combat.resolve_player_attack(ctx)
					var actual_damage := enemy.take_damage(dmg_result.final_damage)

					if not result.damage_per_enemy.has(enemy_idx):
						result.damage_per_enemy[enemy_idx] = 0
					result.damage_per_enemy[enemy_idx] += actual_damage

					if not enemy.is_alive and enemy_idx not in result.enemies_killed:
						result.enemies_killed.append(enemy_idx)

					break  # Each monster attacks one enemy (first alive)

			combo_index += 1

	# 4. Apply heart healing
	if elements_matched.has(MFBoardTypes.Element.HEART):
		var heart_count: int = elements_matched[MFBoardTypes.Element.HEART]
		var total_rec := 0
		for stats in team_stats:
			if stats != null:
				total_rec += stats.rec
		var combo_mult := MFComboCalculator.calculate(total_combos)
		result.healing = int(total_rec * heart_count * combo_mult * 0.1)
		state.team_hp = mini(state.team_hp + result.healing, state.max_hp)

	# 5. Skill pipeline turn end (tick durations)
	_skill_pipeline.process_turn_end(skill_ctx)

	# 6. Check wave clear
	var all_dead := true
	for enemy in state.enemies:
		if enemy.is_alive:
			all_dead = false
			break

	if all_dead:
		result.wave_cleared = true
		if state.current_wave_index + 1 < state.dungeon_def.waves.size():
			_load_wave(state.current_wave_index + 1)
		else:
			result.battle_ended = true
			result.battle_won = true
			result.rewards = state.dungeon_def.rewards
			state.is_active = false
			_emit("battle_won", {"rewards": result.rewards})

	_emit("player_turn_resolved", {"turn_result": result})
	return result


## Execute the enemy turn after the player's turn.
func execute_enemy_turn() -> Array:  # -> Array[{enemy_id, damage}]
	var attacks: Array = []
	var ready := MFEnemyAI.tick_countdowns(state.enemies)

	for enemy in ready:
		if not enemy.is_alive:
			continue
		var action = MFEnemyAI.decide_action(enemy)
		var damage := _combat.resolve_enemy_attack(enemy.atk)
		state.team_hp = maxi(state.team_hp - damage, 0)
		attacks.append({"enemy_id": enemy.id, "damage": damage})
		MFEnemyAI.reset_countdown(enemy)

	MFEnemyAI.tick_enemy_statuses(state.enemies)

	# Check loss
	if state.team_hp <= 0:
		state.is_active = false
		_emit("battle_lost", {})

	_emit("enemy_turn_resolved", {"attacks": attacks})
	return attacks


## Activate a skill for a team monster
func activate_skill(skill_def: RefCounted, context: RefCounted) -> RefCounted:  # -> SkillResult
	return _skill_pipeline.activate_skill(skill_def, context)


## Get current dungeon state
func get_state() -> RefCounted:
	return state


func _load_wave(wave_index: int) -> void:
	state.current_wave_index = wave_index
	state.enemies.clear()
	var wave_def = state.dungeon_def.waves[wave_index]
	var enemy_id := 0
	for enemy_data in wave_def.enemies:
		var enemy = MFEnemyTypes.EnemyState.new(enemy_data)
		enemy.id = enemy_id
		state.enemies.append(enemy)
		enemy_id += 1
	_emit("wave_started", {"wave_index": wave_index})


func _make_skill_context(team: Array, team_stats: Array, elements_matched: Dictionary, combo_count: int) -> RefCounted:
	var ctx = MFSkillTypes.SkillContext.new()
	ctx.team = team
	ctx.team_stats = team_stats
	ctx.board = _board
	ctx.combat = _combat
	ctx.enemies = state.enemies
	ctx.team_hp = state.team_hp
	ctx.max_hp = state.max_hp
	ctx.combo_count = combo_count
	ctx.elements_matched = elements_matched
	ctx.turn_number = state.turn_number
	return ctx


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
