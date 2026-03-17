class_name MFRatePrompt extends RefCounted
## Progressive rate-app prompt with Fibonacci spacing, session counting, dismissal tracking.

var _player_state: Object
var _section_name: StringName = &"rate_prompt"

## Fibonacci-spaced session thresholds for showing the prompt.
var _prompt_sessions: Array[int] = [3, 5, 8, 13, 21, 34, 55]


func _init(player_state: Object = null) -> void:
	_player_state = player_state


## Call at app launch to increment session count.
func on_session_start() -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var count: int = int(section.get_value(&"session_count", 0))
	section.set_value(&"session_count", count + 1)


## Check if the rate prompt should be shown this session.
func should_show() -> bool:
	if _player_state == null:
		return false
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	if bool(section.get_value(&"rated", false)):
		return false
	if bool(section.get_value(&"never_ask", false)):
		return false
	var sessions: int = int(section.get_value(&"session_count", 0))
	var dismiss_count: int = int(section.get_value(&"dismiss_count", 0))
	# Use Fibonacci-spaced thresholds
	if dismiss_count < _prompt_sessions.size():
		return sessions >= _prompt_sessions[dismiss_count]
	return false  # Exhausted all prompt opportunities


## User chose to rate the app.
func on_rated() -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	section.set_value(&"rated", true)


## User dismissed the prompt (ask later).
func on_dismissed() -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var count: int = int(section.get_value(&"dismiss_count", 0))
	section.set_value(&"dismiss_count", count + 1)


## User chose "never ask again".
func on_never_ask() -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	section.set_value(&"never_ask", true)


func _ensure_section() -> void:
	if _player_state != null and not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})
