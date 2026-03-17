class_name MFSpring extends RefCounted
## Damped harmonic oscillator for float, Vector2, or Vector3.
## Provides organic, springy motion for UI elements.

var _value: float = 0.0
var _velocity: float = 0.0
var _target: float = 0.0
var _damping: float = 0.5  ## 0 = no damping, 1 = critical damping
var _frequency: float = 15.0  ## Oscillations per second
var _threshold: float = 0.001  ## Values below this are considered settled


func _init(initial: float = 0.0, damping: float = 0.5, frequency: float = 15.0) -> void:
	_value = initial
	_target = initial
	_damping = damping
	_frequency = frequency


## Set the target value the spring moves toward.
func set_target(target: float) -> void:
	_target = target


## Apply an impulse (add to velocity).
func impulse(force: float) -> void:
	_velocity += force


## Update the spring. Call each frame with delta.
func update(delta: float) -> float:
	var omega: float = _frequency * TAU
	var damping_force: float = 2.0 * _damping * omega
	var spring_force: float = omega * omega

	var displacement: float = _value - _target
	var acceleration: float = -spring_force * displacement - damping_force * _velocity
	_velocity += acceleration * delta
	_value += _velocity * delta

	return _value


## Get the current value.
func get_value() -> float:
	return _value


## Whether the spring has settled (close enough to target with near-zero velocity).
func is_settled() -> bool:
	return absf(_value - _target) < _threshold and absf(_velocity) < _threshold


## Reset to a value with zero velocity.
func reset(value: float) -> void:
	_value = value
	_target = value
	_velocity = 0.0
