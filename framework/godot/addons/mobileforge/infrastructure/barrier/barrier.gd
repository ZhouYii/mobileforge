class_name MFBarrier extends RefCounted
## Async synchronization primitive. Tracks N pending tokens; emits when all resolved.
## Usage: add tokens for pending async operations, resolve each when done,
## await all_resolved or check is_resolved().

signal all_resolved

var _pending: Dictionary = {}  # StringName -> true


## Add a pending token. Does nothing if already pending.
func add(token_id: StringName) -> void:
	_pending[token_id] = true


## Resolve a pending token. Emits all_resolved when last token resolved.
func resolve(token_id: StringName) -> void:
	if not _pending.has(token_id):
		return
	_pending.erase(token_id)
	if _pending.is_empty():
		all_resolved.emit()


## Whether all tokens have been resolved (or none were added).
func is_resolved() -> bool:
	return _pending.is_empty()


## Number of tokens still pending.
func pending_count() -> int:
	return _pending.size()


## Get array of pending token IDs.
func get_pending() -> Array:
	return _pending.keys()


## Reset: clear all pending tokens without emitting.
func reset() -> void:
	_pending.clear()


## Coroutine-friendly: await this to wait for all tokens to resolve.
## Returns immediately if already resolved.
func await_all() -> void:
	if _pending.is_empty():
		return
	await all_resolved
