class_name MFHapticManager extends RefCounted
## Haptic feedback manager with light/medium/heavy/custom presets.
## Delegates to platform-specific provider.

var _provider: Object  ## Platform-specific haptic provider
var _enabled: bool = true


func set_provider(provider: Object) -> void:
	_provider = provider


func set_enabled(enabled: bool) -> void:
	_enabled = enabled


func light() -> void:
	_play("light")


func medium() -> void:
	_play("medium")


func heavy() -> void:
	_play("heavy")


func custom(intensity: float, duration_ms: int) -> void:
	if not _enabled or _provider == null:
		return
	if _provider.has_method("play_custom"):
		_provider.play_custom(intensity, duration_ms)


func _play(preset: String) -> void:
	if not _enabled or _provider == null:
		return
	if _provider.has_method("play"):
		_provider.play(preset)
