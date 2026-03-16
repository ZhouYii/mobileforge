extends HBoxContainer
## Live-binding currency display with animated count-up/down.

var _label: Label
var _currency_type: StringName
var _current_display: int = 0
var _target_value: int = 0
var _event_bus: Object
var _animate_speed: float = 5.0  # Units per second multiplier

func setup(currency_type: StringName, event_bus: Object) -> void:
	_currency_type = currency_type
	_event_bus = event_bus

	_label = Label.new()
	_label.text = "0"
	add_child(_label)

	if _event_bus != null and _event_bus.has_method("subscribe"):
		_event_bus.subscribe(EventNames.CURRENCY_CHANGED, _on_currency_changed)

func set_value(value: int) -> void:
	_target_value = value

func _on_currency_changed(payload: Dictionary) -> void:
	if payload.get("currency_type") == _currency_type:
		_target_value = int(payload.get("new_value", _target_value))

func _process(delta: float) -> void:
	if _current_display != _target_value:
		var diff := _target_value - _current_display
		var step := maxi(int(abs(diff) * _animate_speed * delta), 1)
		if diff > 0:
			_current_display = mini(_current_display + step, _target_value)
		else:
			_current_display = maxi(_current_display - step, _target_value)
		_label.text = str(_current_display)

func _exit_tree() -> void:
	if _event_bus != null and _event_bus.has_method("unsubscribe"):
		_event_bus.unsubscribe(EventNames.CURRENCY_CHANGED, _on_currency_changed)
