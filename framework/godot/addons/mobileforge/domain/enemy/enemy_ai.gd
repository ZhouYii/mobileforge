class_name MFEnemyAI extends RefCounted
## Enemy turn logic. Pure functions.


## Tick all enemy countdowns by 1. Returns enemies that are ready to attack (countdown == 0).
## Enemies with "bind" or "stun" status are excluded from the ready list.
static func tick_countdowns(enemies: Array) -> Array:  # Array[EnemyState] -> Array[EnemyState] (ready ones)
	var ready: Array = []
	for enemy in enemies:
		if not enemy.is_alive:
			continue
		# Bound enemies don't count down
		if enemy.has_status("bind"):
			continue
		enemy.countdown -= 1
		if enemy.countdown <= 0:
			# Stunned enemies consume the stun instead of attacking
			if enemy.has_status("stun"):
				enemy.remove_status("stun")
				reset_countdown(enemy)
				continue
			ready.append(enemy)
	return ready


## Decide what action an enemy takes based on its behavior field.
## Behaviors:
##   "normal" — always attacks
##   "heavy_attack" — 2x damage every 3rd attack, normal otherwise
##   "heal_self" — heals 20% max HP when below 30% HP, otherwise attacks
##   "buff_allies" — boosts other enemies' next attack once, then attacks normally
static func decide_action(enemy: RefCounted) -> RefCounted:  # EnemyState -> EnemyAction
	var b: String = enemy.behavior if "behavior" in enemy else "normal"

	match b:
		"heavy_attack":
			enemy.attack_count += 1
			if enemy.attack_count % 3 == 0:
				return MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk * 2.0), "all")
			return MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")

		"heal_self":
			var hp_ratio := float(enemy.hp) / float(enemy.max_hp) if enemy.max_hp > 0 else 1.0
			if hp_ratio < 0.3:
				var heal_amount := int(enemy.max_hp * 0.2)
				enemy.heal(heal_amount)
				var action = MFEnemyTypes.EnemyAction.new("heal", 0, "self")
				action.extra = {"heal_amount": heal_amount}
				return action
			return MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")

		"buff_allies":
			if not enemy.has_used_buff:
				enemy.has_used_buff = true
				var action = MFEnemyTypes.EnemyAction.new("buff", 0, "allies")
				action.extra = {"atk_boost": 1.5}
				return action
			return MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")

		"jammer_spawn":
			# Attack + spawn 3 jammer gems on the board
			var action = MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")
			action.extra = {"spawn_hazard": MFBoardTypes.Element.JAMMER, "spawn_count": 3}
			return action

		"poison_spawn":
			# Attack + spawn 2 poison gems on the board
			var action = MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk * 0.5), "all")
			action.extra = {"spawn_hazard": MFBoardTypes.Element.POISON, "spawn_count": 2}
			return action

		"bomb_spawn":
			# Spawn 1 bomb gem (no direct damage — bomb explodes when matched)
			var action = MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk * 0.3), "all")
			action.extra = {"spawn_hazard": MFBoardTypes.Element.BOMB, "spawn_count": 1}
			return action

		"lock_gems":
			# Attack + lock 4 random gems (can't be matched until status expires)
			var action = MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")
			action.extra = {"lock_count": 4, "lock_turns": 3}
			return action

		_:  # "normal" or unknown
			return MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")


## Apply buff_allies effect to other enemies in the wave
static func apply_buff_allies(enemies: Array, buffer: RefCounted) -> void:
	var boost: float = 1.5
	for enemy in enemies:
		if enemy != buffer and enemy.is_alive:
			enemy.atk *= boost
			enemy.add_status("atk_boosted", 1, {"original_atk": enemy.atk / boost})


## Reset an enemy's countdown after it attacks
static func reset_countdown(enemy: RefCounted) -> void:
	enemy.countdown = enemy.max_countdown


## Check if an enemy can attack this turn
static func can_attack(enemy: RefCounted) -> bool:
	if not enemy.is_alive:
		return false
	return enemy.countdown <= 0


## Tick status effects on all enemies. Removes expired statuses.
static func tick_enemy_statuses(enemies: Array) -> void:
	for enemy in enemies:
		if not enemy.is_alive:
			continue
		for i in range(enemy.status_effects.size() - 1, -1, -1):
			var status: Dictionary = enemy.status_effects[i]
			if status.has("turns") and status["turns"] > 0:
				status["turns"] -= 1
				if status["turns"] <= 0:
					enemy.status_effects.remove_at(i)
