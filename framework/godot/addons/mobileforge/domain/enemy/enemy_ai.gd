class_name MFEnemyAI extends RefCounted
## Enemy turn logic. Pure functions.


## Tick all enemy countdowns by 1. Returns enemies that are ready to attack (countdown == 0).
static func tick_countdowns(enemies: Array) -> Array:  # Array[EnemyState] -> Array[EnemyState] (ready ones)
	var ready: Array = []
	for enemy in enemies:
		if not enemy.is_alive:
			continue
		enemy.countdown -= 1
		if enemy.countdown <= 0:
			ready.append(enemy)
	return ready


## Decide what action an enemy takes. Simple: always attacks.
## Override in game code for more complex AI patterns.
static func decide_action(enemy: RefCounted) -> RefCounted:  # EnemyState -> EnemyAction
	return MFEnemyTypes.EnemyAction.new("attack", int(enemy.atk), "all")


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
