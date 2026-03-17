class_name MFPhaseTypes extends RefCounted
## Type definitions for phase sequencer.


## Create a phase definition dictionary.
## Advance modes (first match wins):
##   - auto_advance = true: advances immediately after on_enter completes
##   - duration > 0: advances after duration seconds
##   - barrier != null: advances when barrier.all_resolved fires
##   - none of above: manual advance() required
static func create_phase(
	id: StringName,
	on_enter: Callable = Callable(),
	on_update: Callable = Callable(),
	on_exit: Callable = Callable(),
	auto_advance: bool = false,
	barrier: MFBarrier = null,
	duration: float = 0.0,
) -> Dictionary:
	return {
		"id": id,
		"on_enter": on_enter,
		"on_update": on_update,
		"on_exit": on_exit,
		"auto_advance": auto_advance,
		"barrier": barrier,
		"duration": duration,
	}
