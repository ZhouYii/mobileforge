class_name MFLoadingOverlay extends ColorRect
## Full-screen loading spinner overlay with optional cycling tips.

var _label: Label
var _tip_label: Label
var _tips: Array[String] = []
var _tip_timer: float = 0.0
var _tip_interval: float = 4.0  ## Seconds between tip changes
var _current_tip_index: int = -1

func _init() -> void:
	color = Color(0, 0, 0, 0.7)
	set_anchors_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP

	_label = Label.new()
	_label.text = "Loading..."
	_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_label.set_anchors_preset(Control.PRESET_CENTER)
	add_child(_label)

	_tip_label = Label.new()
	_tip_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_tip_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_tip_label.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_tip_label.position.y = -40
	_tip_label.visible = false
	add_child(_tip_label)

func set_message(text: String) -> void:
	_label.text = text


## Set tips to cycle during loading.
func set_tips(tips: Array[String], interval: float = 4.0) -> void:
	_tips = tips
	_tip_interval = interval
	if not _tips.is_empty():
		_tip_label.visible = true
		_show_random_tip()
	else:
		_tip_label.visible = false


func _process(delta: float) -> void:
	if _tips.is_empty() or not visible:
		return
	_tip_timer += delta
	if _tip_timer >= _tip_interval:
		_tip_timer = 0.0
		_show_random_tip()


func _show_random_tip() -> void:
	if _tips.is_empty():
		return
	var idx := randi() % _tips.size()
	# Avoid showing the same tip twice in a row
	if _tips.size() > 1:
		while idx == _current_tip_index:
			idx = randi() % _tips.size()
	_current_tip_index = idx
	_tip_label.text = _tips[idx]
