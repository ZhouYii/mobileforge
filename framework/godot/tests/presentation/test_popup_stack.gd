extends MFTestBase
## Tests for PopupStack (presentation/popup_stack/popup_stack.gd)
## PopupStack manages a priority queue of popups, showing one at a time.
## We test the queue/priority/callback logic using mock popup factories.

const PopupStackScript = preload("res://addons/mobileforge/presentation/popup_stack/popup_stack.gd")

var _stack: Node


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_stack = PopupStackScript.new()


func after_each() -> void:
	if _stack != null:
		_stack.free()
		_stack = null


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

var _dismiss_results: Array = []

func _on_dismiss(result: Variant) -> void:
	_dismiss_results.append(result)


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_show_queues_popup() -> void:
	_stack.show_popup("alert", {"title": "Hello"}, 0)
	assert_eq(_stack.popup_count(), 1, "popup_count should be 1 after showing one popup")


func test_dismiss_removes_popup() -> void:
	_stack.show_popup("alert", {"title": "Hello"}, 0)
	_stack.dismiss()
	assert_eq(_stack.popup_count(), 0, "popup_count should be 0 after dismiss")


func test_priority_ordering() -> void:
	# Show a low-priority popup first, then a high-priority one.
	# Higher numeric priority should appear first (show_popup enqueues by priority).
	_stack.show_popup("low", {"title": "Low"}, 1)
	_stack.show_popup("high", {"title": "High"}, 5)
	# The currently showing popup should be the higher priority one.
	assert_eq(_stack.current_popup_id(), "high", "higher priority popup should show first")


func test_dismiss_shows_next() -> void:
	_stack.show_popup("first", {"title": "1st"}, 5)
	_stack.show_popup("second", {"title": "2nd"}, 1)
	assert_eq(_stack.popup_count(), 2, "should have 2 popups queued")
	# Dismiss the first (highest priority)
	_stack.dismiss()
	assert_eq(_stack.popup_count(), 1, "should have 1 popup remaining")
	assert_eq(_stack.current_popup_id(), "second", "second popup should now be showing")


func test_dismiss_all_clears() -> void:
	_stack.show_popup("a", {}, 1)
	_stack.show_popup("b", {}, 2)
	_stack.show_popup("c", {}, 3)
	assert_eq(_stack.popup_count(), 3, "should have 3 popups")
	_stack.dismiss_all()
	assert_eq(_stack.popup_count(), 0, "dismiss_all should clear all popups")


func test_is_showing() -> void:
	assert_false(_stack.is_showing(), "should not be showing initially")
	_stack.show_popup("notice", {}, 0)
	assert_true(_stack.is_showing(), "should be showing after show_popup")
	_stack.dismiss()
	assert_false(_stack.is_showing(), "should not be showing after dismiss")


func test_on_dismiss_callback() -> void:
	_dismiss_results.clear()
	_stack.show_popup("confirm", {"title": "OK?"}, 0, _on_dismiss)
	_stack.dismiss({"accepted": true})
	assert_eq(_dismiss_results.size(), 1, "dismiss callback should be called once")
	assert_true(_dismiss_results[0] is Dictionary, "result should be a dictionary")
	assert_eq(_dismiss_results[0]["accepted"], true, "callback should receive dismiss result")


func test_empty_dismiss_no_crash() -> void:
	# Dismissing when nothing is showing should not crash
	_stack.dismiss()
	assert_true(true, "dismiss on empty stack should not crash")
	assert_eq(_stack.popup_count(), 0, "popup_count should remain 0")
