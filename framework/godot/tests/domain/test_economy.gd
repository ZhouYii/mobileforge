extends MFTestBase
## Tests for Economy (domain/economy/economy.gd)
## Creates mock PlayerState and EventBus per test for isolation.

const EventBusScript = preload("res://addons/mobileforge/infrastructure/event_bus/event_bus.gd")
const PlayerStateScript = preload("res://addons/mobileforge/infrastructure/player_state/player_state.gd")
const EconomyScript = preload("res://addons/mobileforge/domain/economy/economy.gd")
const EconomyTypesScript = preload("res://addons/mobileforge/domain/economy/economy_types.gd")
const StaminaTimerScript = preload("res://addons/mobileforge/domain/economy/stamina_timer.gd")

var _bus: Node
var _ps: Node
var _economy: MFEconomy

# Event tracking
var _currency_events: Array[Dictionary] = []


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_bus = EventBusScript.new()
	_ps = PlayerStateScript.new()
	_ps._event_bus = _bus
	# Register a currencies section with 0 initial balances
	_ps.register_section(&"currencies", {"coins": 0, "gems": 0, "stamina": 50})
	_economy = MFEconomy.new(_ps, _bus)
	_currency_events.clear()


func after_each() -> void:
	if _ps != null:
		_ps.free()
		_ps = null
	if _bus != null:
		_bus.free()
		_bus = null


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

func _on_currency_changed(payload: Dictionary) -> void:
	_currency_events.append(payload)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_earn_increases_balance() -> void:
	_economy.earn("coins", 100)
	assert_eq(_economy.get_balance("coins"), 100,
		"balance should be 100 after earning 100 coins")


func test_spend_decreases_balance() -> void:
	_economy.earn("coins", 100)
	var ok = _economy.spend("coins", 30)
	assert_true(ok, "spend should succeed when balance is sufficient")
	assert_eq(_economy.get_balance("coins"), 70,
		"balance should be 70 after earning 100 and spending 30")


func test_can_afford_true() -> void:
	_economy.earn("coins", 100)
	assert_true(_economy.can_afford("coins", 50),
		"should be able to afford 50 when balance is 100")


func test_can_afford_false() -> void:
	_economy.earn("coins", 100)
	assert_false(_economy.can_afford("coins", 200),
		"should not be able to afford 200 when balance is 100")


func test_spend_fails_when_insufficient() -> void:
	_economy.earn("coins", 100)
	var ok = _economy.spend("coins", 200)
	assert_false(ok, "spend should return false when balance is insufficient")
	assert_eq(_economy.get_balance("coins"), 100,
		"balance should remain 100 after failed spend")


func test_stamina_timer_refill() -> void:
	# Configure stamina: refill every 300 seconds
	var config = MFEconomyTypes.StaminaConfig.new(100, 300.0, "gems", 1)
	var timer = MFStaminaTimer.new(config)

	# First call initializes the last_update_time
	var t0 := 1000.0
	var result0 = timer.calculate_refill(50, t0)
	assert_eq(result0["stamina"], 50, "initial call should return current stamina")

	# Advance 600 seconds -> 2 refill ticks at 300s each
	var t1 := t0 + 600.0
	var result1 = timer.calculate_refill(50, t1)
	assert_eq(result1["stamina"], 52,
		"after 600s with 300s/point, should gain 2 points: 50 + 2 = 52")


func test_currency_changed_event() -> void:
	_bus.subscribe(EventNames.CURRENCY_CHANGED, _on_currency_changed)
	_economy.earn("coins", 100)
	_economy.spend("coins", 30)

	assert_eq(_currency_events.size(), 2,
		"should have 2 currency_changed events (earn + spend)")

	# Verify the spend event payload
	var spend_evt: Dictionary = _currency_events[1]
	assert_eq(spend_evt["currency_type"], "coins", "event currency_type should be 'coins'")
	assert_eq(spend_evt["old_value"], 100, "old_value should be 100 before spend")
	assert_eq(spend_evt["new_value"], 70, "new_value should be 70 after spend of 30")
