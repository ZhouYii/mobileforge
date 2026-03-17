## Action Sequence -- Designer-Driven Animation Flow
## A sequential action runner inspired by Monument Valley's TriggerableAction system.
## Actions can be blocking (awaitable) or fire-and-forget.
## Supports fast-forward for checkpoint replay.
##
## Usage:
##   var seq = ActionSequence.new()
##   seq.add_action(move_platform)     # Returns Signal -> blocking
##   seq.add_action(play_sound)        # Returns null -> fire-and-forget
##   seq.add_wait(0.5)                 # Wait 0.5 seconds
##   seq.add_action(open_door)
##   await seq.execute(self)
class_name ActionSequence
extends RefCounted

## Whether to skip all waits and play animations instantly.
## Set to true when fast-forwarding to a checkpoint.
static var fast_forward: bool = false

var _actions: Array[Dictionary] = []  # [{type, callable, duration}]


## Add a callable action. If it returns a Signal, the sequence waits for it.
func add_action(action: Callable) -> ActionSequence:
	_actions.append({"type": "action", "callable": action})
	return self


## Add a wait step (seconds).
func add_wait(duration: float) -> ActionSequence:
	_actions.append({"type": "wait", "duration": duration})
	return self


## Add a tween-based action. The sequence waits for the tween to finish.
func add_tween(tween_creator: Callable) -> ActionSequence:
	_actions.append({"type": "tween", "callable": tween_creator})
	return self


## Execute all actions in order. Blocks until all blocking actions complete.
func execute(runner: Node) -> void:
	for entry in _actions:
		if fast_forward:
			# In fast-forward mode, call actions but skip waits
			if entry.type == "action":
				entry.callable.call()
			elif entry.type == "tween":
				var tween: Tween = entry.callable.call()
				if tween:
					tween.custom_step(999.0)  # Jump to end
			continue

		match entry.type:
			"action":
				var result = entry.callable.call()
				if result is Signal:
					await result
			"wait":
				await runner.get_tree().create_timer(entry.duration).timeout
			"tween":
				var tween: Tween = entry.callable.call()
				if tween:
					await tween.finished


## Clear all actions for reuse.
func clear() -> void:
	_actions.clear()


## Get the number of actions in this sequence.
func size() -> int:
	return _actions.size()
