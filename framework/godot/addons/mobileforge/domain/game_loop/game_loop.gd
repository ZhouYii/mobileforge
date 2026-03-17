class_name MFGameLoop extends RefCounted
## Interface for genre-specific game loops.
## Implementations must override all methods.
## Turn-based games use process_input(); real-time games use tick().


## Start the game loop with a configuration dictionary.
func start(config: Dictionary) -> void:
	push_error("MFGameLoop.start() not implemented")


## Process player input (turn-based games).
## Returns a PhaseResult describing what happened.
func process_input(input: Dictionary) -> MFGameLoopTypes.PhaseResult:
	push_error("MFGameLoop.process_input() not implemented")
	return MFGameLoopTypes.PhaseResult.new()


## Tick the loop forward by delta seconds (real-time games).
## Returns a PhaseResult describing what happened.
func tick(delta: float) -> MFGameLoopTypes.PhaseResult:
	push_error("MFGameLoop.tick() not implemented")
	return MFGameLoopTypes.PhaseResult.new()


## Get a snapshot of the current game state.
func get_state() -> MFGameLoopTypes.GameLoopState:
	push_error("MFGameLoop.get_state() not implemented")
	return MFGameLoopTypes.GameLoopState.new()


## Whether the game loop is currently active.
func is_active() -> bool:
	return false
