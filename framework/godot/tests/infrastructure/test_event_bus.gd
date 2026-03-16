extends MFTestBase
## Tests for EventBus (infrastructure/event_bus/event_bus.gd)

const EventBusScript = preload("res://addons/mobileforge/infrastructure/event_bus/event_bus.gd")

var _bus: Node


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_bus = EventBusScript.new()


func after_each() -> void:
	if _bus != null:
		_bus.free()
		_bus = null


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

var _last_payload: Dictionary = {}
var _call_count := 0

func _on_event(payload: Dictionary) -> void:
	_last_payload = payload
	_call_count += 1

var _second_call_count := 0

func _on_event_second(payload: Dictionary) -> void:
	_second_call_count += 1


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_subscribe_and_emit() -> void:
	_call_count = 0
	_last_payload = {}
	_bus.subscribe(&"test_event", _on_event)
	_bus.emit_event(&"test_event", {"key": "value"})
	assert_eq(_call_count, 1, "callback should be called once")
	assert_has(_last_payload, "key", "payload should contain key")
	assert_eq(_last_payload.get("key"), "value", "payload value should match")


func test_multiple_subscribers() -> void:
	_call_count = 0
	_second_call_count = 0
	_bus.subscribe(&"multi", _on_event)
	_bus.subscribe(&"multi", _on_event_second)
	_bus.emit_event(&"multi", {})
	assert_eq(_call_count, 1, "first subscriber called")
	assert_eq(_second_call_count, 1, "second subscriber called")


func test_unsubscribe() -> void:
	_call_count = 0
	_bus.subscribe(&"unsub_event", _on_event)
	_bus.unsubscribe(&"unsub_event", _on_event)
	_bus.emit_event(&"unsub_event", {"should": "not arrive"})
	assert_eq(_call_count, 0, "callback should not be called after unsubscribe")


func test_emit_no_subscribers() -> void:
	# Should not crash or error
	_bus.emit_event(&"nobody_listening", {"data": 42})
	assert_true(true, "emit with no subscribers should not crash")


func test_subscriber_count() -> void:
	assert_eq(_bus.subscriber_count(&"counted"), 0, "initial count is 0")
	_bus.subscribe(&"counted", _on_event)
	assert_eq(_bus.subscriber_count(&"counted"), 1, "count after one subscribe")
	_bus.subscribe(&"counted", _on_event_second)
	assert_eq(_bus.subscriber_count(&"counted"), 2, "count after two subscribes")
	_bus.unsubscribe(&"counted", _on_event)
	assert_eq(_bus.subscriber_count(&"counted"), 1, "count after one unsubscribe")
	_bus.unsubscribe(&"counted", _on_event_second)
	assert_eq(_bus.subscriber_count(&"counted"), 0, "count drops to 0 and entry is cleaned up")


func test_clear_all() -> void:
	_bus.subscribe(&"a", _on_event)
	_bus.subscribe(&"b", _on_event_second)
	_bus.clear_all()
	assert_eq(_bus.subscriber_count(&"a"), 0, "count for 'a' after clear")
	assert_eq(_bus.subscriber_count(&"b"), 0, "count for 'b' after clear")


func test_duplicate_subscribe_ignored() -> void:
	_call_count = 0
	_bus.subscribe(&"dup", _on_event)
	_bus.subscribe(&"dup", _on_event)  # duplicate
	assert_eq(_bus.subscriber_count(&"dup"), 1, "duplicate subscribe should not add twice")
	_bus.emit_event(&"dup", {})
	assert_eq(_call_count, 1, "callback called only once despite duplicate subscribe")


func test_different_events_isolated() -> void:
	_call_count = 0
	_second_call_count = 0
	_bus.subscribe(&"event_a", _on_event)
	_bus.subscribe(&"event_b", _on_event_second)
	_bus.emit_event(&"event_a", {})
	assert_eq(_call_count, 1, "event_a callback called")
	assert_eq(_second_call_count, 0, "event_b callback NOT called when event_a emitted")


func test_unsubscribe_during_emit() -> void:
	# Callback that unsubscribes itself should not crash because emit iterates a copy.
	_call_count = 0
	var self_unsub_cb := Callable(self, "_on_self_unsubscribe")
	_bus.subscribe(&"self_unsub", self_unsub_cb)
	_bus.subscribe(&"self_unsub", _on_event)
	_bus.emit_event(&"self_unsub", {})
	# Both callbacks should have been called (iteration over copy)
	assert_eq(_call_count, 1, "second subscriber still called after first unsubscribed itself")
	assert_eq(_bus.subscriber_count(&"self_unsub"), 1, "self-unsubscriber removed")

func _on_self_unsubscribe(payload: Dictionary) -> void:
	_bus.unsubscribe(&"self_unsub", Callable(self, "_on_self_unsubscribe"))


func test_emit_empty_payload() -> void:
	_call_count = 0
	_last_payload = {"leftover": true}
	_bus.subscribe(&"empty_payload", _on_event)
	_bus.emit_event(&"empty_payload")
	assert_eq(_call_count, 1, "callback called with default empty payload")
	assert_eq(_last_payload.size(), 0, "payload should be empty dict")


func test_unsubscribe_nonexistent_event() -> void:
	# Unsubscribing from an event that was never subscribed to should not crash.
	_bus.unsubscribe(&"never_existed", _on_event)
	assert_true(true, "unsubscribe from nonexistent event should not crash")


func test_unsubscribe_nonexistent_callback() -> void:
	_bus.subscribe(&"exists", _on_event)
	_bus.unsubscribe(&"exists", _on_event_second)  # was never subscribed
	assert_eq(_bus.subscriber_count(&"exists"), 1, "original subscriber still present")
