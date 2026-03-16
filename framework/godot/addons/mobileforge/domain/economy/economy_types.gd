class_name MFEconomyTypes extends RefCounted


class CurrencyType extends RefCounted:
	var id: String
	var name: String
	var max_amount: int  # -1 = unlimited

	func _init(p_id: String = "", p_name: String = "", p_max: int = -1) -> void:
		id = p_id
		name = p_name
		max_amount = p_max


class StaminaConfig extends RefCounted:
	var max_stamina: int
	var refill_rate_seconds: float  # Seconds per 1 stamina point
	var refill_cost_currency: String
	var refill_cost_amount: int

	func _init(p_max: int = 100, p_rate: float = 300.0, p_currency: String = "gems", p_cost: int = 1) -> void:
		max_stamina = p_max
		refill_rate_seconds = p_rate
		refill_cost_currency = p_currency
		refill_cost_amount = p_cost
