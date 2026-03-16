extends ScrollContainer
## Recycling virtual 1D scroll list. Only creates visible items + buffer.
## Dramatically reduces node count for large lists.

var _data: Array = []
var _item_factory: Callable
var _bind_callback: Callable  # func(item: Node, data: Variant, index: int)
var _item_height: float = 60.0
var _pool: MFListItemPool
var _content: VBoxContainer
var _visible_items: Dictionary = {}  # index -> Node
var _buffer_count: int = 2  # Extra items above/below viewport

func setup(item_factory: Callable, bind_callback: Callable, item_height: float = 60.0) -> void:
	_item_factory = item_factory
	_bind_callback = bind_callback
	_item_height = item_height
	_pool = MFListItemPool.new(item_factory)

	_content = VBoxContainer.new()
	add_child(_content)

func set_data(data: Array) -> void:
	_recycle_all()
	_data = data
	# Set content height
	_content.custom_minimum_size.y = data.size() * _item_height
	_update_visible()

func get_data() -> Array:
	return _data

func get_data_count() -> int:
	return _data.size()

func _process(_delta: float) -> void:
	_update_visible()

func _update_visible() -> void:
	if _data.is_empty():
		return

	var scroll_y := get_v_scroll_bar().value if get_v_scroll_bar() != null else 0.0
	var viewport_h := size.y

	var first_visible := maxi(int(scroll_y / _item_height) - _buffer_count, 0)
	var last_visible := mini(int((scroll_y + viewport_h) / _item_height) + _buffer_count, _data.size() - 1)

	# Recycle items outside visible range
	for idx in _visible_items.keys():
		if idx < first_visible or idx > last_visible:
			_pool.release(_visible_items[idx])
			_visible_items.erase(idx)

	# Create items in visible range
	for idx in range(first_visible, last_visible + 1):
		if _visible_items.has(idx):
			continue
		var item := _pool.acquire()
		item.visible = true
		item.position.y = idx * _item_height
		item.custom_minimum_size.y = _item_height
		if item.get_parent() != _content:
			_content.add_child(item)
		_bind_callback.call(item, _data[idx], idx)
		_visible_items[idx] = item

func _recycle_all() -> void:
	for idx in _visible_items.keys():
		_pool.release(_visible_items[idx])
	_visible_items.clear()
