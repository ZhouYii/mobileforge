extends MFTestBase
## Tests for UIRouter navigation logic.
## UIRouter manages a screen stack with push/pop/replace/navigate operations.
## We test without a scene tree by focusing on the navigation state.

const UIRouterScript = preload("res://addons/mobileforge/presentation/ui_router/ui_router.gd")

var _router: Node


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_router = UIRouterScript.new()
	# Register some test screens with simple factory callables
	_router.register_screen("home", func(): return Control.new())
	_router.register_screen("shop", func(): return Control.new())
	_router.register_screen("inventory", func(): return Control.new())
	_router.register_screen("settings", func(): return Control.new())


func after_each() -> void:
	if _router != null:
		_router.free()
		_router = null


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------

func test_register_screen() -> void:
	assert_true(_router.has_screen("home"), "home screen should be registered")
	assert_true(_router.has_screen("shop"), "shop screen should be registered")
	assert_false(_router.has_screen("nonexistent"), "nonexistent screen should not be registered")


func test_navigate_sets_current() -> void:
	_router.navigate("home")
	assert_eq(_router.current_screen_id(), "home", "current screen should be home")
	_router.navigate("shop")
	assert_eq(_router.current_screen_id(), "shop", "current screen should be shop after navigate")


func test_push_increases_depth() -> void:
	_router.navigate("home")
	var initial_depth := _router.stack_depth()
	_router.push("shop")
	assert_eq(_router.stack_depth(), initial_depth + 1, "push should increase stack depth by 1")


func test_pop_decreases_depth() -> void:
	_router.navigate("home")
	_router.push("shop")
	_router.push("inventory")
	var depth_before := _router.stack_depth()
	_router.pop()
	assert_eq(_router.stack_depth(), depth_before - 1, "pop should decrease stack depth by 1")
	assert_eq(_router.current_screen_id(), "shop", "current screen should be shop after popping inventory")


func test_pop_single_screen_no_op() -> void:
	_router.navigate("home")
	assert_eq(_router.stack_depth(), 1, "stack depth should be 1")
	_router.pop()
	# Pop with only one screen should be a no-op
	assert_eq(_router.stack_depth(), 1, "stack depth should still be 1 after pop on single screen")
	assert_eq(_router.current_screen_id(), "home", "current screen should still be home")


func test_replace_keeps_depth() -> void:
	_router.navigate("home")
	_router.push("shop")
	var depth_before := _router.stack_depth()
	_router.replace("inventory")
	assert_eq(_router.stack_depth(), depth_before, "replace should not change stack depth")
	assert_eq(_router.current_screen_id(), "inventory", "current screen should be inventory after replace")


func test_navigate_clears_stack() -> void:
	_router.navigate("home")
	_router.push("shop")
	_router.push("inventory")
	assert_eq(_router.stack_depth(), 3, "stack depth should be 3 after 3 screens")
	# Navigate resets the stack to a single screen
	_router.navigate("settings")
	assert_eq(_router.stack_depth(), 1, "navigate should reset stack depth to 1")
	assert_eq(_router.current_screen_id(), "settings", "current screen should be settings")
