extends Node
class_name MFHitstop
## Hit pause / hitstop: freeze the scene tree for N frames.
## Critical for action games to emphasize impacts.

var _freeze_frames: int = 0
var _original_timescale: float = 1.0
var _is_frozen: bool = false


## Freeze for a number of frames.
func freeze(frames: int = 4) -> void:
	if _is_frozen:
		return
	_freeze_frames = frames
	_original_timescale = Engine.time_scale
	Engine.time_scale = 0.0
	_is_frozen = true


## Freeze for a duration in seconds (converted to frames at 60fps).
func freeze_seconds(duration: float = 0.067) -> void:
	freeze(int(duration * 60.0))


func _process(_delta: float) -> void:
	if not _is_frozen:
		return
	# Even with time_scale=0, _process still runs (it uses unscaled delta)
	_freeze_frames -= 1
	if _freeze_frames <= 0:
		Engine.time_scale = _original_timescale
		_is_frozen = false
