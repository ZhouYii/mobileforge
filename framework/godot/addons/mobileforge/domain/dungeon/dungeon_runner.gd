class_name MFDungeonRunner extends MFGameLoop
## Orchestrates a dungeon run: start, turn loop, wave progression, end.
## This is the ONLY domain module that sequences calls to other domain modules.
## Implements MFGameLoop for the match-3 RPG genre.

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
func start_dungeon(dungeon_def: RefCounted, team_hp: int, max_hp: int) -> void:
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

				# Floor effect: element restriction — skip non-matching elements
				var restricted_elem := _get_element_restriction()
				if restricted_elem > 0 and match_result.element != restricted_elem:
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

					# Apply enemy characteristics before resolving damage
					var chars: Dictionary = enemy.def_data.get("characteristics", {})
					var has_void_shield := _has_team_buff("void_element_shield")
					var has_void_absorb := _has_team_buff("void_damage_absorb")

					# Combo shield: skip if combo count too low
					if chars.has("combo_shield"):
						if not MFEnemyCharacteristics.combo_shield(total_combos, int(chars["combo_shield"])):
							continue

					# Element shield: skip if attacking element is immune (unless voided)
					if chars.has("element_shield") and not has_void_shield:
						var immune: Array[int] = []
						for e in chars["element_shield"]:
							immune.append(int(e))
						if not MFEnemyCharacteristics.element_shield(match_result.element, immune):
							continue

					var dmg_result = _combat.resolve_player_attack(ctx)
					var final_dmg: float = dmg_result.final_damage

					# Damage absorb: enemy heals instead of taking damage (unless voided)
					if chars.has("damage_absorb") and not has_void_absorb:
						var absorb_elems: Array[int] = []
						for e in chars["damage_absorb"]:
							absorb_elems.append(int(e))
						if MFEnemyCharacteristics.damage_absorb(match_result.element, absorb_elems):
							enemy.heal(int(final_dmg))
							continue

					# Damage reduction
					if chars.has("damage_reduction"):
						final_dmg = MFEnemyCharacteristics.apply_damage_reduction(final_dmg, float(chars["damage_reduction"]))

					# Damage cap
					if chars.has("damage_cap"):
						final_dmg = MFEnemyCharacteristics.apply_damage_cap(final_dmg, float(chars["damage_cap"]))

					var actual_damage: int = enemy.take_damage(int(final_dmg))

					if not result.damage_per_enemy.has(enemy_idx):
						result.damage_per_enemy[enemy_idx] = 0
					result.damage_per_enemy[enemy_idx] += actual_damage

					if not enemy.is_alive and enemy_idx not in result.enemies_killed:
						result.enemies_killed.append(enemy_idx)

					break  # Each monster attacks one enemy (first alive)

			combo_index += 1

	# 4. Apply heart healing (unless no_heart_heal floor effect)
	if elements_matched.has(MFBoardTypes.Element.HEART) and not _has_floor_effect("no_heart_heal"):
		var heart_count: int = elements_matched[MFBoardTypes.Element.HEART]
		var total_rec := 0
		for stats in team_stats:
			if stats != null:
				total_rec += stats.rec
		var combo_mult := MFComboCalculator.calculate(total_combos)
		result.healing = int(total_rec * heart_count * combo_mult * 0.1)
		state.team_hp = mini(state.team_hp + result.healing, state.max_hp)

	# 4b. Apply poison_floor effect (team takes 5% max HP damage per turn)
	if _has_floor_effect("poison_floor"):
		var floor_dmg := int(state.max_hp * 0.05)
		state.team_hp = maxi(state.team_hp - floor_dmg, 0)

	# 4c. Apply hazard gem effects (jammer/bomb/poison damage to player)
	for step in cascade_steps:
		if "hazard_effects" in step:
			for effect in step.hazard_effects:
				var effect_type: String = effect.get("type", "")
				var dmg: int = int(effect.get("damage", 0))
				if dmg > 0 and (effect_type == "jammer_damage" or effect_type == "poison_damage"):
					state.team_hp = maxi(state.team_hp - dmg, 0)

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
			result.exp_gained = _calculate_dungeon_exp()
			state.is_active = false
			_emit("battle_won", {"rewards": result.rewards, "exp": result.exp_gained})

	# 7. Check turn limit
	if not result.battle_ended and state.dungeon_def.turn_limit > 0:
		if state.turn_number >= state.dungeon_def.turn_limit:
			result.battle_ended = true
			result.battle_won = false
			result.turn_limit_exceeded = true
			state.is_active = false
			_emit("turn_limit_exceeded", {"turn_number": state.turn_number})

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

		match action.type:
			"heal":
				# Enemy healed itself (already applied in decide_action)
				var heal_amount: int = action.extra.get("heal_amount", 0)
				attacks.append({"enemy_id": enemy.id, "damage": 0, "type": "heal", "heal_amount": heal_amount})
			"buff":
				# Enemy buffs allies
				MFEnemyAI.apply_buff_allies(state.enemies, enemy)
				attacks.append({"enemy_id": enemy.id, "damage": 0, "type": "buff"})
			_:
				# Attack — use action.damage which accounts for heavy_attack multiplier
				var damage: int = _combat.resolve_enemy_attack(action.damage)
				state.team_hp = maxi(state.team_hp - damage, 0)
				attacks.append({"enemy_id": enemy.id, "damage": damage, "type": "attack"})

				# Counter attack: reflect damage back at the attacking enemy
				var reflect_pct := _get_counter_attack_percent()
				if reflect_pct > 0.0 and damage > 0:
					var reflected := int(damage * reflect_pct)
					enemy.take_damage(reflected)
					attacks.append({"enemy_id": enemy.id, "damage": reflected, "type": "counter"})

		# Apply board hazard effects from enemy action
		if action.extra.has("spawn_hazard") and _board != null:
			var hazard_elem: int = action.extra["spawn_hazard"]
			var hazard_count: int = action.extra.get("spawn_count", 1)
			var placed: Array = _board.place_hazard_gems(hazard_elem, hazard_count)
			if not placed.is_empty():
				attacks.append({"enemy_id": enemy.id, "damage": 0, "type": "hazard_spawn",
					"hazard_element": hazard_elem, "positions": placed})

		if action.extra.has("lock_count") and _board != null:
			var lock_count: int = action.extra["lock_count"]
			var lock_turns: int = action.extra.get("lock_turns", 3)
			# Pick random gem positions to lock
			var candidates: Array[int] = []
			for i in range(_board.config.total_cells()):
				var gem = _board.get_gem(i)
				if gem != null and not gem.has_status(MFBoardTypes.GemStatus.LOCKED):
					candidates.append(i)
			candidates.shuffle()
			var to_lock: Array[int] = []
			for i in range(mini(lock_count, candidates.size())):
				to_lock.append(candidates[i])
			_board.apply_status_at(to_lock, MFBoardTypes.GemStatus.LOCKED, lock_turns)
			if not to_lock.is_empty():
				attacks.append({"enemy_id": enemy.id, "damage": 0, "type": "lock_gems",
					"positions": to_lock, "turns": lock_turns})

		MFEnemyAI.reset_countdown(enemy)

	MFEnemyAI.tick_enemy_statuses(state.enemies)

	# Floor effect: enemy HP regen (5% max HP per turn)
	if _has_floor_effect("enemy_hp_regen"):
		for enemy in state.enemies:
			if enemy.is_alive:
				enemy.heal(int(enemy.max_hp * 0.05))

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
func get_dungeon_state() -> RefCounted:
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


func _has_floor_effect(effect: String) -> bool:
	if state == null or state.dungeon_def == null:
		return false
	return state.dungeon_def.has_floor_effect(effect)


func _get_element_restriction() -> int:
	## Returns the restricted element ID, or 0 if no restriction.
	if state == null or state.dungeon_def == null:
		return 0
	for fe in state.dungeon_def.floor_effects:
		if fe.begins_with("element_restrict_"):
			return int(fe.substr("element_restrict_".length()))
	return 0


func _get_counter_attack_percent() -> float:
	if state == null or not "team_buffs" in state:
		return 0.0
	for buff in state.team_buffs:
		if buff.get("type", "") == "counter_attack":
			return float(buff.get("reflect_percent", 0.0))
	return 0.0


func _has_team_buff(buff_type: String) -> bool:
	if state == null:
		return false
	if not "team_buffs" in state:
		return false
	for buff in state.team_buffs:
		if buff.get("type", "") == buff_type:
			return true
	return false


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)


# ── MFGameLoop interface ──

## IGameLoop.start — config must contain "dungeon_def", "team_hp", "max_hp".
func start(config: Dictionary) -> void:
	var ddef = config.get("dungeon_def")
	var hp := int(config.get("team_hp", 0))
	var mhp := int(config.get("max_hp", hp))
	if ddef is RefCounted:
		start_dungeon(ddef, hp, mhp)


## IGameLoop.process_input — input must contain "cascade_steps", "team", "team_stats".
func process_input(input: Dictionary) -> MFGameLoopTypes.PhaseResult:
	var cascade_steps: Array = input.get("cascade_steps", [])
	var team: Array = input.get("team", [])
	var team_stats: Array = input.get("team_stats", [])
	var turn_result = execute_player_turn(cascade_steps, team, team_stats)
	var result := MFGameLoopTypes.PhaseResult.new("player_turn", true)
	result.data = {"turn_result": turn_result}
	if turn_result.battle_ended:
		result.next_phase = "ended"
	else:
		result.next_phase = "enemy_turn"
	return result


## IGameLoop.tick — match-3 RPG is turn-based, tick is a no-op.
func tick(_delta: float) -> MFGameLoopTypes.PhaseResult:
	return MFGameLoopTypes.PhaseResult.new("idle", false)


func get_state() -> MFGameLoopTypes.GameLoopState:
	var s := MFGameLoopTypes.GameLoopState.new()
	s.phase = "active" if (state != null and state.is_active) else "inactive"
	s.turn_number = state.turn_number if state != null else 0
	s.is_active = state != null and state.is_active
	s.custom = {"team_hp": state.team_hp, "max_hp": state.max_hp} if state != null else {}
	return s


func is_active() -> bool:
	return state != null and state.is_active


## Calculate total EXP from all enemies across all waves in the dungeon.
## Formula: sum of (enemy_hp / 10) for all enemies, multiplied by difficulty.
func _calculate_dungeon_exp() -> int:
	if state == null or state.dungeon_def == null:
		return 0
	var total_exp := 0
	for wave_def in state.dungeon_def.waves:
		for enemy_data in wave_def.enemies:
			var hp: int = int(enemy_data.get("hp", 0))
			var atk: int = int(enemy_data.get("atk", 0))
			total_exp += hp / 10 + atk / 5
	# Difficulty multiplier
	var diff_mult := 1.0
	match state.dungeon_def.difficulty:
		"expert": diff_mult = 1.5
		"mythical": diff_mult = 2.5
		"annihilation": diff_mult = 4.0
	return int(total_exp * diff_mult)
