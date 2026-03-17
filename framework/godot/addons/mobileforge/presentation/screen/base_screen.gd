class_name MFBaseScreen extends Control
## Abstract base class for screens. Game code extends this.
## Lifecycle: will_enter -> on_enter -> on_enter_finished -> (on_pause -> on_resume)* -> will_exit -> on_exit -> on_exit_finished
##
## Screen chrome configuration: set flags in subclass _init() to control
## which UI chrome the screen requests (header bar, back button, footer).

# Screen chrome configuration (set in subclass _init)
var show_header: bool = true
var show_back_button: bool = true
var show_footer: bool = true
var transition_duration: float = 0.25


func _ready() -> void:
	# Fill parent viewport by default
	set_anchors_preset(PRESET_FULL_RECT)


## Called when this screen becomes active (pushed or navigated to)
func on_enter(params: Dictionary = {}) -> void:
	pass

## Called before the enter transition starts. Use for pre-transition setup.
func will_enter(params: Dictionary = {}) -> void:
	pass

## Called after the enter transition animation completes. Safe to interact.
func on_enter_finished() -> void:
	pass

## Called when another screen is pushed on top (this screen is still in stack)
func on_pause() -> void:
	pass

## Called when the screen above is popped and this screen becomes active again
func on_resume() -> void:
	pass

## Called before the exit transition starts.
func will_exit() -> void:
	pass

## Called when this screen is being removed from the stack
func on_exit() -> void:
	pass

## Called after the exit transition animation completes.
func on_exit_finished() -> void:
	pass

## Built-in slide-in transition (matching ToS ContainerObject pattern).
## Override in subclass for custom enter transitions.
func _play_enter_transition() -> Tween:
	return MFUIAnim.slide_in_from(self, Vector2(size.x if size.x > 0 else 640.0, 0), transition_duration)

## Built-in slide-out transition.
## Override in subclass for custom exit transitions.
func _play_exit_transition() -> Tween:
	return MFUIAnim.slide_out_to(self, Vector2(-(size.x if size.x > 0 else 640.0), 0), transition_duration)
