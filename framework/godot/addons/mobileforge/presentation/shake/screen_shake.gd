extends Node
class_name MFScreenShake
## Screen/camera shake using trauma accumulator + damped sinusoidal.
## Attach to a Camera2D or any Node2D to apply shake offset.

@export var max_offset: Vector2 = Vector2(16, 12)
@export var max_rotation: float = 0.05  ## radians
@export var decay_rate: float = 3.0  ## trauma per second decay
@export var frequency: float = 15.0  ## oscillation frequency

var _trauma: float = 0.0
var _time: float = 0.0
var _target: Node2D  ## Node to shake (set via setup or parent)


func setup(target: Node2D) -> void:
	_target = target


func _ready() -> void:
	if _target == null and get_parent() is Node2D:
		_target = get_parent() as Node2D


func _process(delta: float) -> void:
	if _trauma <= 0.0:
		return
	_time += delta
	_trauma = maxf(_trauma - decay_rate * delta, 0.0)
	var shake := _trauma * _trauma  # Quadratic falloff
	if _target != null:
		_target.offset.x = max_offset.x * shake * sin(frequency * _time * 1.0) if _target is Camera2D else 0.0
		_target.offset.y = max_offset.y * shake * sin(frequency * _time * 1.3) if _target is Camera2D else 0.0
		if _target is Camera2D:
			pass  # offset already applied
		else:
			_target.position = Vector2(
				max_offset.x * shake * sin(frequency * _time),
				max_offset.y * shake * sin(frequency * _time * 1.3)
			)


## Add trauma (0.0 - 1.0). Trauma stacks but is clamped to 1.0.
func add_trauma(amount: float) -> void:
	_trauma = clampf(_trauma + amount, 0.0, 1.0)


## Preset: light shake (e.g. button press)
func shake_light() -> void:
	add_trauma(0.2)

## Preset: medium shake (e.g. damage taken)
func shake_medium() -> void:
	add_trauma(0.5)

## Preset: heavy shake (e.g. critical hit, boss defeat)
func shake_heavy() -> void:
	add_trauma(0.8)
