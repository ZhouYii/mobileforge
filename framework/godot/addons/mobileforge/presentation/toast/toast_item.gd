class_name MFToastItem extends PanelContainer
## Individual toast notification widget.
## Auto-dismisses after a duration.

var _label: Label
var _timer: float = 0.0
var _duration: float = 2.0
var _fading: bool = false

func _init() -> void:
	_label = Label.new()
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	add_child(_label)
	modulate.a = 0.0  # Start invisible, fade in

func setup(text: String, duration: float = 2.0) -> void:
	_label.text = text
	_duration = duration

func _process(delta: float) -> void:
	if not _fading:
		# Fade in
		modulate.a = minf(modulate.a + delta * 4.0, 1.0)
		_timer += delta
		if _timer >= _duration:
			_fading = true
	else:
		# Fade out
		modulate.a -= delta * 2.0
		if modulate.a <= 0.0:
			queue_free()
