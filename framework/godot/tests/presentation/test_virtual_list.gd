extends MFTestBase
## Tests for VirtualList data model and ListItemPool recycling logic.
## VirtualList extends ScrollContainer (Node), so we test the data/pool layer
## that can operate without a scene tree.

const ListItemPoolScript = preload("res://addons/mobileforge/presentation/virtual_list/list_item_pool.gd")
const GridCellPoolScript = preload("res://addons/mobileforge/presentation/virtual_list/grid_cell_pool.gd")
const VirtualListModelScript = preload("res://addons/mobileforge/presentation/virtual_list/virtual_list_model.gd")

var _model: RefCounted
var _pool: RefCounted


# ---------------------------------------------------------------------------
# Lifecycle
# ---------------------------------------------------------------------------

func before_each() -> void:
	_model = VirtualListModelScript.new()
	_pool = ListItemPoolScript.new()


# ---------------------------------------------------------------------------
# Tests: Data Model
# ---------------------------------------------------------------------------

func test_set_data() -> void:
	var items: Array = []
	for i in range(100):
		items.append({"id": i, "name": "Item_%d" % i})
	_model.set_data(items)
	assert_eq(_model.get_data_count(), 100, "data count should be 100")


# ---------------------------------------------------------------------------
# Tests: ListItemPool
# ---------------------------------------------------------------------------

func test_pool_acquire_release() -> void:
	# Acquire an item (pool is empty, so it creates a new one)
	var item1 = _pool.acquire()
	assert_not_null(item1, "acquired item should not be null")
	# Release it back
	_pool.release(item1)
	assert_eq(_pool.pool_size(), 1, "pool should have 1 item after release")
	# Acquire again — should reuse the released item
	var item2 = _pool.acquire()
	assert_eq(_pool.pool_size(), 0, "pool should be empty after reacquire")
	# The reused item should be the same instance
	assert_true(item1 == item2, "acquired item should be the same reused instance")


func test_list_item_pool_size() -> void:
	var items: Array = []
	for i in range(3):
		items.append(_pool.acquire())
	# Release all 3
	for item in items:
		_pool.release(item)
	assert_eq(_pool.pool_size(), 3, "pool should have 3 items after releasing 3")


func test_pool_clear() -> void:
	var item = _pool.acquire()
	_pool.release(item)
	assert_eq(_pool.pool_size(), 1, "pool should have 1 item")
	_pool.clear()
	assert_eq(_pool.pool_size(), 0, "pool should be empty after clear")


# ---------------------------------------------------------------------------
# Tests: GridCellPool
# ---------------------------------------------------------------------------

func test_grid_cell_pool() -> void:
	var grid_pool = GridCellPoolScript.new()
	# Acquire 3 cells
	var cells: Array = []
	for i in range(3):
		cells.append(grid_pool.acquire())
	assert_eq(grid_pool.pool_size(), 0, "grid pool should be empty while items are in use")
	# Release all 3
	for cell in cells:
		grid_pool.release(cell)
	assert_eq(grid_pool.pool_size(), 3, "grid pool should have 3 items after release")
	# Acquire one — should reuse
	var reused = grid_pool.acquire()
	assert_eq(grid_pool.pool_size(), 2, "grid pool should have 2 items after one reacquire")
	assert_not_null(reused, "reused cell should not be null")
	# Clear
	grid_pool.clear()
	assert_eq(grid_pool.pool_size(), 0, "grid pool should be empty after clear")
