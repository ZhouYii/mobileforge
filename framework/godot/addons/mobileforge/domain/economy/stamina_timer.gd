class_name MFStaminaTimer extends RefCounted
## Calculates stamina refill based on elapsed time.

var config: RefCounted  # StaminaConfig
var _last_update_time: float  # Unix timestamp


func _init(stamina_config: RefCounted = null) -> void:
	config = stamina_config
	_last_update_time = 0.0


## Calculate how much stamina has regenerated since last update
func calculate_refill(current_stamina: int, current_time: float) -> Dictionary:
	if config == null or _last_update_time <= 0.0:
		_last_update_time = current_time
		return {"stamina": current_stamina, "remainder_seconds": 0.0}

	var elapsed := current_time - _last_update_time
	var points_gained := int(elapsed / config.refill_rate_seconds)
	var remainder := fmod(elapsed, config.refill_rate_seconds)

	var new_stamina := mini(current_stamina + points_gained, config.max_stamina)
	_last_update_time = current_time - remainder

	return {"stamina": new_stamina, "remainder_seconds": remainder}


## Get seconds until next stamina point
func seconds_until_next(current_stamina: int, current_time: float) -> float:
	if config == null or current_stamina >= config.max_stamina:
		return 0.0
	var elapsed := current_time - _last_update_time
	return maxf(config.refill_rate_seconds - fmod(elapsed, config.refill_rate_seconds), 0.0)


func set_last_update(time: float) -> void:
	_last_update_time = time
