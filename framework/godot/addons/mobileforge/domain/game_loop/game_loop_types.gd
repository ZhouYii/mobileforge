class_name MFGameLoopTypes extends RefCounted
## Types shared across all game loop implementations.


## The result of processing a phase (input or tick).
class PhaseResult extends RefCounted:
	var phase_name: String
	var completed: bool  ## True if this phase is done
	var next_phase: String  ## Hint for the next phase (empty = let loop decide)
	var data: Dictionary  ## Arbitrary phase-specific output

	func _init(p_phase: String = "", p_completed: bool = false, p_data: Dictionary = {}) -> void:
		phase_name = p_phase
		completed = p_completed
		next_phase = ""
		data = p_data


## Snapshot of a game loop's current state.
class GameLoopState extends RefCounted:
	var phase: String  ## Current phase name
	var turn_number: int
	var is_active: bool
	var custom: Dictionary  ## Genre-specific state

	func _init() -> void:
		phase = ""
		turn_number = 0
		is_active = false
		custom = {}
