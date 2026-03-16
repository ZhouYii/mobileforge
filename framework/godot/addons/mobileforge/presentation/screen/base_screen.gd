class_name MFBaseScreen extends Control
## Abstract base class for screens. Game code extends this.
## Lifecycle: on_enter -> (on_pause -> on_resume)* -> on_exit

## Called when this screen becomes active (pushed or navigated to)
func on_enter(params: Dictionary = {}) -> void:
	pass

## Called when another screen is pushed on top (this screen is still in stack)
func on_pause() -> void:
	pass

## Called when the screen above is popped and this screen becomes active again
func on_resume() -> void:
	pass

## Called when this screen is being removed from the stack
func on_exit() -> void:
	pass
