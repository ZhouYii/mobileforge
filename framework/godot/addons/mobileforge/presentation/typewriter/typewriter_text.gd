extends RichTextLabel
class_name MFTypewriterText
## Character-by-character text reveal with punctuation-aware delays.

signal reveal_completed

@export var chars_per_second: float = 30.0
@export var punctuation_delay: float = 0.15  ## Extra delay after . , ! ? : ;
@export var paragraph_delay: float = 0.3  ## Extra delay after newline

var _full_text: String = ""
var _char_index: int = 0
var _timer: float = 0.0
var _is_revealing: bool = false
var _skip_requested: bool = false

const PUNCTUATION := ".!?;:"


func start_reveal(text: String) -> void:
	_full_text = text
	_char_index = 0
	_timer = 0.0
	_is_revealing = true
	_skip_requested = false
	visible_characters = 0
	bbcode_enabled = true
	text = _full_text


func skip() -> void:
	if _is_revealing:
		_skip_requested = true


func _process(delta: float) -> void:
	if not _is_revealing:
		return

	if _skip_requested:
		visible_characters = -1
		_is_revealing = false
		reveal_completed.emit()
		return

	_timer += delta
	var interval := 1.0 / chars_per_second

	while _timer >= interval and _char_index < _full_text.length():
		_char_index += 1
		visible_characters = _char_index
		_timer -= interval

		# Add extra delay for punctuation
		if _char_index < _full_text.length():
			var ch := _full_text[_char_index - 1]
			if ch in PUNCTUATION:
				_timer -= punctuation_delay
			elif ch == "\n":
				_timer -= paragraph_delay

	if _char_index >= _full_text.length():
		_is_revealing = false
		reveal_completed.emit()


func is_revealing() -> bool:
	return _is_revealing
