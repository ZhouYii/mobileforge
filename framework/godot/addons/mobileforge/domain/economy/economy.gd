class_name MFEconomy extends RefCounted
## Currency and stamina management. Uses PlayerState for persistence.

var _player_state: Object  # PlayerState reference
var _event_bus: Object
var _stamina_timer: MFStaminaTimer
var _stamina_config: RefCounted


func _init(player_state: Object = null, event_bus: Object = null) -> void:
	_player_state = player_state
	_event_bus = event_bus


func setup_stamina(config: RefCounted) -> void:
	_stamina_config = config
	_stamina_timer = MFStaminaTimer.new(config)


func can_afford(currency: String, amount: int) -> bool:
	if _player_state == null:
		return false
	var current := int(_player_state.get_value(&"currencies", StringName(currency), 0))
	return current >= amount


func spend(currency: String, amount: int) -> bool:
	if not can_afford(currency, amount):
		return false
	var current := int(_player_state.get_value(&"currencies", StringName(currency), 0))
	_player_state.set_value(&"currencies", StringName(currency), current - amount)
	_emit_currency_changed(currency, current, current - amount)
	return true


func earn(currency: String, amount: int) -> void:
	if _player_state == null:
		return
	var current := int(_player_state.get_value(&"currencies", StringName(currency), 0))
	var new_val := current + amount
	_player_state.set_value(&"currencies", StringName(currency), new_val)
	_emit_currency_changed(currency, current, new_val)


func get_balance(currency: String) -> int:
	if _player_state == null:
		return 0
	return int(_player_state.get_value(&"currencies", StringName(currency), 0))


func check_stamina(cost: int) -> bool:
	return get_balance("stamina") >= cost


func spend_stamina(cost: int) -> bool:
	return spend("stamina", cost)


## Refill stamina by spending gems. Allows exceeding max (overflow).
## Returns true if refill was successful.
func refill_stamina_with_gems() -> bool:
	if _stamina_config == null:
		return false
	if not can_afford(_stamina_config.refill_cost_currency, _stamina_config.refill_cost_amount):
		return false
	spend(_stamina_config.refill_cost_currency, _stamina_config.refill_cost_amount)
	# Grant full max_stamina worth, even if it overflows
	earn("stamina", _stamina_config.max_stamina)
	return true


## Check if stamina is currently over the natural max.
func is_stamina_overflowed() -> bool:
	if _stamina_config == null:
		return false
	return get_balance("stamina") > _stamina_config.max_stamina


## Get the natural max stamina cap.
func get_max_stamina() -> int:
	if _stamina_config == null:
		return 0
	return _stamina_config.max_stamina


func _emit_currency_changed(currency: String, old_val: int, new_val: int) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(EventNames.CURRENCY_CHANGED, {
			"currency_type": currency,
			"old_value": old_val,
			"new_value": new_val,
		})
