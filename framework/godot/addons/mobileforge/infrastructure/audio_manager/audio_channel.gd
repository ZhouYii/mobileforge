class_name MFAudioChannel extends RefCounted
## Represents an audio channel (SFX or BGM).

var name: StringName
var volume: float = 1.0
var is_muted: bool = false

func _init(p_name: StringName, p_volume: float = 1.0) -> void:
    name = p_name
    volume = p_volume

func get_effective_volume() -> float:
    return 0.0 if is_muted else volume
