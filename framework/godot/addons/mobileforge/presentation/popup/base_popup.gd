class_name MFBasePopup extends Control
## Abstract base class for popups. Game code extends this.
## Provides lifecycle methods and dismiss signal.

signal dismissed(result: Variant)

var _popup_stack: Object  # Reference to PopupStack (set by PopupStack)
var popup_id: StringName

## Called when the popup is shown
func on_show(params: Dictionary = {}) -> void:
	pass

## Called to dismiss this popup with a result
func dismiss(result: Variant = null) -> void:
	dismissed.emit(result)

## Override to handle back button / escape
func on_back_pressed() -> bool:
	dismiss(null)
	return true
