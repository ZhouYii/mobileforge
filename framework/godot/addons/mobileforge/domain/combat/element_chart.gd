class_name MFElementChart extends RefCounted
## Element advantage/disadvantage multiplier lookups.
## Loads data from the shared element_chart.json or can be configured manually.


var _advantages: Dictionary = {}  ## {attacker_element: {defender_element: multiplier}}
var _default_multiplier: float = 1.0


func _init() -> void:
	_setup_default_chart()


## Set up the ToS default element chart.
func _setup_default_chart() -> void:
	# WATER(1) > FIRE(2), FIRE(2) > GRASS(3), GRASS(3) > WATER(1)
	# LIGHT(4) <> DARK(5) mutual advantage
	_set(1, 2, 1.5)  # Water beats Fire
	_set(2, 3, 1.5)  # Fire beats Grass
	_set(3, 1, 1.5)  # Grass beats Water
	_set(4, 5, 1.5)  # Light beats Dark
	_set(5, 4, 1.5)  # Dark beats Light
	_set(1, 3, 0.5)  # Water weak to Grass
	_set(2, 1, 0.5)  # Fire weak to Water
	_set(3, 2, 0.5)  # Grass weak to Fire


func _set(atk: int, def: int, mult: float) -> void:
	if not _advantages.has(atk):
		_advantages[atk] = {}
	_advantages[atk][def] = mult


## Return the element multiplier for attacker vs defender.
func get_multiplier(attacker_element: int, defender_element: int) -> float:
	if _advantages.has(attacker_element) and _advantages[attacker_element].has(defender_element):
		return _advantages[attacker_element][defender_element]
	return _default_multiplier


## Load chart from parsed JSON data (the element_chart.json format).
func load_from_data(data: Dictionary) -> void:
	_advantages.clear()
	_default_multiplier = data.get("default_multiplier", 1.0)
	for adv in data.get("advantages", []):
		_set(adv["attacker"], adv["defender"], adv["multiplier"])
	for dis in data.get("disadvantages", []):
		_set(dis["attacker"], dis["defender"], dis["multiplier"])
