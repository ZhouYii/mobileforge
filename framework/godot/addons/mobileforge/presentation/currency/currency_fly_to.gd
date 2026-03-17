extends Node
class_name MFCurrencyFlyTo
## Spawns icons that fly from source position to currency bar via bezier arc.
## Uses MFBarrier to track all arrivals.

var _currency_bar: Control
var _pool: MFObjectPool
var _active_tweens: Array = []


## Setup with target currency bar control.
func setup(currency_bar: Control) -> void:
	_currency_bar = currency_bar


## Fly icons from source position to currency bar.
## count: how many logical items earned
## from_pos: global position where icons spawn
## icon_factory: Callable() -> Control (creates an icon node)
## config: {duration: 0.5, spread: 30.0, max_icons: 10, stagger: 0.03}
## Returns an MFBarrier that resolves when all icons arrive.
func fly(count: int, from_pos: Vector2, icon_factory: Callable, config: Dictionary = {}) -> MFBarrier:
	var duration: float = config.get("duration", 0.5)
	var spread: float = config.get("spread", 30.0)
	var max_icons: int = config.get("max_icons", 10)
	var stagger: float = config.get("stagger", 0.03)

	var icon_count := mini(count, max_icons)
	var barrier := MFBarrier.new()

	if _currency_bar == null or icon_count <= 0:
		return barrier  # Already resolved (empty)

	var target_pos := _currency_bar.global_position + _currency_bar.size * 0.5

	for i in range(icon_count):
		var token_id := &"fly_%d" % i
		barrier.add(token_id)

		# Spawn icon
		var icon: Control = icon_factory.call()
		add_child(icon)
		icon.global_position = from_pos + Vector2(
			randf_range(-spread, spread),
			randf_range(-spread, spread)
		)

		# Animate with delay
		var delay := stagger * i
		var start_pos := icon.global_position
		var arc_height := 50.0 + randf_range(0, 30)
		var mid_x := (start_pos.x + target_pos.x) * 0.5
		var mid_y := minf(start_pos.y, target_pos.y) - arc_height
		var mid_pos := Vector2(mid_x, mid_y)

		var tween := create_tween()
		if delay > 0:
			tween.tween_interval(delay)
		tween.tween_method(
			func(t: float):
				var u := 1.0 - t
				icon.global_position = u * u * start_pos + 2.0 * u * t * mid_pos + t * t * target_pos,
			0.0, 1.0, duration
		).set_ease(Tween.EASE_IN).set_trans(Tween.TRANS_QUAD)
		tween.tween_callback(func():
			icon.queue_free()
			barrier.resolve(token_id)
		)
		_active_tweens.append(tween)

	return barrier
