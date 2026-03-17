class_name MFAudioVariation extends RefCounted
## Audio variation system. Extends AudioManager with random pitch/clip selection per SFX group.

var _groups: Dictionary = {}  # group_name -> {clips: Array[StringName], pitch_range: Vector2}


## Register an SFX group with multiple clip variants and pitch randomization.
func register_group(group_name: StringName, clip_ids: Array, pitch_min: float = 0.9, pitch_max: float = 1.1) -> void:
	_groups[group_name] = {
		"clips": clip_ids,
		"pitch_range": Vector2(pitch_min, pitch_max),
	}


## Pick a random clip and pitch from a group. Returns {clip_id, pitch}.
func pick(group_name: StringName) -> Dictionary:
	if not _groups.has(group_name):
		return {"clip_id": group_name, "pitch": 1.0}  # Fallback: use group_name as clip_id
	var group: Dictionary = _groups[group_name]
	var clips: Array = group.clips
	var clip_id: StringName = clips[randi() % clips.size()]
	var pr: Vector2 = group.pitch_range
	var pitch := randf_range(pr.x, pr.y)
	return {"clip_id": clip_id, "pitch": pitch}


## Play a varied SFX through the audio manager.
func play(group_name: StringName, audio_manager: Object) -> void:
	var pick_result := pick(group_name)
	if audio_manager != null and audio_manager.has_method("play_sfx"):
		# Find the SFX player and set pitch before playing
		audio_manager.play_sfx(pick_result.clip_id)
