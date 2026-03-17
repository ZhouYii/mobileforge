extends Node
class_name MFParticleManager
## Particle effect manager with pooling, deduplication, and LOD quality tiers.

var _pools: Dictionary = {}  # effect_id -> MFScenePool
var _active: Array = []  # Array of {effect_id, node, timer}
var _quality: int = 2  ## 0=off, 1=low, 2=medium, 3=high
var _max_active: int = 32


func set_quality(level: int) -> void:
	_quality = clampi(level, 0, 3)
	_max_active = [0, 8, 32, 64][_quality]


## Register a particle effect with a factory.
func register_effect(effect_id: StringName, factory: Callable) -> void:
	_pools[effect_id] = MFScenePool.new(factory, 16)


## Spawn a particle effect at a position.
func spawn(effect_id: StringName, position: Vector2, duration: float = 1.0) -> Node:
	if _quality == 0:
		return null
	if _active.size() >= _max_active:
		return null  # LOD: skip if too many active
	if not _pools.has(effect_id):
		return null

	var pool: MFScenePool = _pools[effect_id]
	var node: Node = pool.acquire()
	if node is Node2D:
		node.position = position
	add_child(node)
	_active.append({"effect_id": effect_id, "node": node, "timer": duration})
	return node


func _process(delta: float) -> void:
	var i := _active.size() - 1
	while i >= 0:
		_active[i].timer -= delta
		if _active[i].timer <= 0.0:
			var entry = _active[i]
			_active.remove_at(i)
			if _pools.has(entry.effect_id):
				_pools[entry.effect_id].release(entry.node)
		i -= 1
