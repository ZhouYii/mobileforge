extends MFTestBase
## Tests for EnemyAI (domain/enemy/enemy_ai.gd)

const EnemyTypesScript = preload("res://addons/mobileforge/domain/enemy/enemy_types.gd")
const EnemyAIScript = preload("res://addons/mobileforge/domain/enemy/enemy_ai.gd")

var _enemies: Array


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _make_enemy(id: int, hp: int, atk: float, countdown: int) -> RefCounted:
	return MFEnemyTypes.EnemyState.new({
		"id": id,
		"name": "Enemy_%d" % id,
		"element": 1,
		"hp": hp,
		"atk": atk,
		"countdown": countdown,
	})


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	# 3 enemies with countdowns [2, 1, 3]
	_enemies = [
		_make_enemy(0, 1000, 100.0, 2),
		_make_enemy(1, 800, 150.0, 1),
		_make_enemy(2, 1200, 80.0, 3),
	]


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_tick_countdowns_returns_ready() -> void:
	# After tick: [1, 0, 2]. Only enemy_1 (countdown was 1 -> 0) should be ready.
	var ready = MFEnemyAI.tick_countdowns(_enemies)
	assert_eq(ready.size(), 1, "only 1 enemy should be ready")
	assert_eq(ready[0].id, 1, "enemy_1 should be the ready one")
	# Verify countdowns were decremented
	assert_eq(_enemies[0].countdown, 1, "enemy_0 countdown should be 1")
	assert_eq(_enemies[1].countdown, 0, "enemy_1 countdown should be 0")
	assert_eq(_enemies[2].countdown, 2, "enemy_2 countdown should be 2")


func test_tick_countdowns_skips_dead() -> void:
	# Kill enemy_1 (hp=0), then tick. Dead enemy should not be ticked.
	_enemies[1].hp = 0
	var ready = MFEnemyAI.tick_countdowns(_enemies)
	# enemy_0: 2->1, enemy_1: dead (skipped), enemy_2: 3->2
	assert_eq(ready.size(), 0, "no enemies should be ready")
	assert_eq(_enemies[0].countdown, 1, "enemy_0 should be ticked")
	assert_eq(_enemies[1].countdown, 1, "dead enemy_1 should NOT be ticked")
	assert_eq(_enemies[2].countdown, 2, "enemy_2 should be ticked")


func test_decide_action_returns_attack() -> void:
	var action = MFEnemyAI.decide_action(_enemies[0])
	assert_not_null(action, "action should not be null")
	assert_eq(action.type, "attack", "default AI should return attack")
	assert_eq(action.damage, int(_enemies[0].atk), "damage should match enemy atk")


func test_reset_countdown() -> void:
	# Tick enemy_1 down to 0, then reset
	_enemies[1].countdown = 0
	MFEnemyAI.reset_countdown(_enemies[1])
	assert_eq(_enemies[1].countdown, _enemies[1].max_countdown,
		"countdown should reset to max_countdown")


func test_can_attack_ready() -> void:
	_enemies[0].countdown = 0
	assert_true(MFEnemyAI.can_attack(_enemies[0]),
		"enemy with countdown=0 should be able to attack")


func test_can_attack_not_ready() -> void:
	_enemies[0].countdown = 2
	assert_false(MFEnemyAI.can_attack(_enemies[0]),
		"enemy with countdown>0 should not be able to attack")


func test_tick_enemy_statuses() -> void:
	# Add a status with turns=1 to enemy_0
	_enemies[0].add_status("poison", 1)
	assert_true(_enemies[0].has_status("poison"),
		"enemy should have poison before tick")

	MFEnemyAI.tick_enemy_statuses(_enemies)
	# After tick, turns=1 decrements to 0 and status is removed
	assert_false(_enemies[0].has_status("poison"),
		"poison with turns=1 should expire after tick")

	# Add a longer status and verify it persists
	_enemies[1].add_status("defense_down", 3)
	MFEnemyAI.tick_enemy_statuses(_enemies)
	assert_true(_enemies[1].has_status("defense_down"),
		"status with turns=3 should persist after 1 tick (now turns=2)")
