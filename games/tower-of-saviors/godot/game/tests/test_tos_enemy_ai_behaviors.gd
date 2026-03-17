extends MFTestBase
## Tests for ToS enemy AI behaviors (normal, heavy_attack, heal_self, buff_allies)

const EnemyAIScript = preload("res://addons/mobileforge/domain/enemy/enemy_ai.gd")


func test_normal_behavior_always_attacks() -> void:
	var enemy = _make_enemy({"atk": 100, "behavior": "normal"})
	
	var action = MFEnemyAI.decide_action(enemy)
	
	assert_eq(action.type, "attack", "normal behavior should always attack")
	assert_eq(action.damage, 100, "normal attack should use base ATK")


func test_heavy_attack_2x_every_3rd() -> void:
	var enemy = _make_enemy({"atk": 100, "behavior": "heavy_attack"})
	
	enemy.attack_count = 0
	var action1 = MFEnemyAI.decide_action(enemy)
	assert_eq(action1.damage, 100, "1st attack: normal damage")
	
	enemy.attack_count = 1
	var action2 = MFEnemyAI.decide_action(enemy)
	assert_eq(action2.damage, 100, "2nd attack: normal damage")
	
	enemy.attack_count = 2
	var action3 = MFEnemyAI.decide_action(enemy)
	assert_eq(action3.damage, 200, "3rd attack: 2x damage")
	
	enemy.attack_count = 3
	var action4 = MFEnemyAI.decide_action(enemy)
	assert_eq(action4.damage, 100, "4th attack: normal damage (cycle restarts)")
	
	enemy.attack_count = 5
	var action6 = MFEnemyAI.decide_action(enemy)
	assert_eq(action6.damage, 200, "6th attack: 2x damage")


func test_heal_self_triggers_below_30_percent() -> void:
	var enemy = _make_enemy({"hp": 1000, "max_hp": 1000, "atk": 100, "behavior": "heal_self"})
	
	enemy.hp = 350
	var action = MFEnemyAI.decide_action(enemy)
	assert_eq(action.type, "attack", "at 35% HP should attack")
	
	enemy.hp = 250
	action = MFEnemyAI.decide_action(enemy)
	assert_eq(action.type, "heal", "at 25% HP should heal")
	assert_eq(action.extra.heal_amount, 200, "heal amount should be 20% of max_hp")


func test_heal_self_heals_correct_amount() -> void:
	var enemy = _make_enemy({"hp": 200, "max_hp": 1000, "atk": 100, "behavior": "heal_self"})
	
	MFEnemyAI.decide_action(enemy)
	
	assert_eq(enemy.hp, 400, "should heal for 200 (20% of 1000)")


func test_buff_allies_triggers_once() -> void:
	var enemy = _make_enemy({"atk": 100, "behavior": "buff_allies"})
	
	assert_false(enemy.has_used_buff, "should start with has_used_buff = false")
	
	var action1 = MFEnemyAI.decide_action(enemy)
	assert_eq(action1.type, "buff", "first action should be buff")
	assert_true(enemy.has_used_buff, "should mark buff as used")
	
	var action2 = MFEnemyAI.decide_action(enemy)
	assert_eq(action2.type, "attack", "subsequent actions should be attacks")
	
	var action3 = MFEnemyAI.decide_action(enemy)
	assert_eq(action3.type, "attack", "all subsequent actions should be attacks")


func test_buff_allies_boosts_other_enemies() -> void:
	var buffer = _make_enemy({"atk": 100, "behavior": "buff_allies"})
	var ally1 = _make_enemy({"atk": 100, "behavior": "normal"})
	var ally2 = _make_enemy({"atk": 150, "behavior": "normal"})
	var enemies = [buffer, ally1, ally2]
	
	MFEnemyAI.apply_buff_allies(enemies, buffer)
	
	assert_eq(buffer.atk, 100, "buffer's ATK should not change")
	assert_eq(ally1.atk, 150, "ally1 ATK should be boosted by 1.5x")
	assert_eq(ally2.atk, 225, "ally2 ATK should be boosted by 1.5x")


func test_buff_allies_ignores_dead_allies() -> void:
	var buffer = _make_enemy({"atk": 100, "behavior": "buff_allies"})
	var alive_ally = _make_enemy({"atk": 100, "behavior": "normal"})
	var dead_ally = _make_enemy({"atk": 100, "behavior": "normal"})
	dead_ally.hp = 0
	dead_ally.is_alive = false
	var enemies = [buffer, alive_ally, dead_ally]
	
	MFEnemyAI.apply_buff_allies(enemies, buffer)
	
	assert_eq(alive_ally.atk, 150, "alive ally should be boosted")
	assert_eq(dead_ally.atk, 100, "dead ally should not be boosted")


func test_jammer_spawn_attacks_and_spawns() -> void:
	var enemy = _make_enemy({"atk": 100, "behavior": "jammer_spawn"})
	
	var action = MFEnemyAI.decide_action(enemy)
	
	assert_eq(action.type, "attack", "jammer_spawn should attack")
	assert_eq(action.damage, 100, "should deal normal damage")
	assert_not_null(action.extra, "should have extra data")
	assert_eq(action.extra.spawn_hazard, 7, "should spawn jammer element")
	assert_eq(action.extra.spawn_count, 3, "should spawn 3 jammers")


func test_poison_spawn_half_damage() -> void:
	var enemy = _make_enemy({"atk": 100, "behavior": "poison_spawn"})
	
	var action = MFEnemyAI.decide_action(enemy)
	
	assert_eq(action.damage, 50, "poison_spawn should deal 0.5x ATK")
	assert_eq(action.extra.spawn_hazard, 8, "should spawn poison element")
	assert_eq(action.extra.spawn_count, 2, "should spawn 2 poisons")


func test_lock_gems_attacks_and_locks() -> void:
	var enemy = _make_enemy({"atk": 100, "behavior": "lock_gems"})
	
	var action = MFEnemyAI.decide_action(enemy)
	
	assert_eq(action.type, "attack", "lock_gems should attack")
	assert_eq(action.damage, 100, "should deal normal damage")
	assert_eq(action.extra.lock_count, 4, "should lock 4 gems")
	assert_eq(action.extra.lock_turns, 3, "should lock for 3 turns")


func _make_enemy(overrides: Dictionary = {}) -> RefCounted:
	var enemy = RefCounted.new()
	enemy.set("hp", overrides.get("hp", 1000))
	enemy.set("max_hp", overrides.get("max_hp", overrides.get("hp", 1000)))
	enemy.set("atk", overrides.get("atk", 100))
	enemy.set("is_alive", overrides.get("is_alive", true))
	enemy.set("countdown", overrides.get("countdown", 1))
	enemy.set("max_countdown", overrides.get("max_countdown", 2))
	enemy.set("behavior", overrides.get("behavior", "normal"))
	enemy.set("attack_count", 0)
	enemy.set("has_used_buff", false)
	enemy.set("status_effects", [])
	
	enemy.set("has_status", func(_type: String) -> bool: return false)
	enemy.set("add_status", func(_type: String, _turns: int = -1, _extra: Dictionary = {}) -> void: pass)
	enemy.set("remove_status", func(_type: String) -> void: pass)
	enemy.set("heal", func(amount: int) -> void:
		enemy.hp = mini(enemy.hp + amount, enemy.max_hp)
	)
	
	return enemy
