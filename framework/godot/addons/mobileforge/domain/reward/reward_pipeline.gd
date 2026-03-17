class_name MFRewardPipeline extends RefCounted
## Unifies reward granting from any source (loot, gacha, shop, battle pass, quests).
## Rewards flow through: validate -> transform -> grant -> notify.

var _economy: Object  # MFEconomy
var _monster_manager: Object  # MFMonsterManager (optional)
var _event_bus: Object
var _transforms: Array = []  # Array of Callable(reward) -> reward


func _init(economy: Object = null, event_bus: Object = null) -> void:
	_economy = economy
	_event_bus = event_bus


func set_monster_manager(mm: Object) -> void:
	_monster_manager = mm


## Register a transform that modifies rewards before granting (e.g. double drop events).
func add_transform(transform: Callable) -> void:
	_transforms.append(transform)


## Grant a batch of rewards. Returns Array of GrantResult.
func grant(rewards: Array, source: String = "unknown") -> Array:
	var results: Array = []
	for reward_data in rewards:
		var reward: Dictionary = reward_data.duplicate()
		# Apply transforms
		for xform in _transforms:
			reward = xform.call(reward)
		var result := _grant_single(reward, source)
		results.append(result)
	_emit("rewards_granted", {"source": source, "results": results})
	return results


## Grant a single reward dictionary.
func _grant_single(reward: Dictionary, source: String) -> Dictionary:
	var reward_type: String = reward.get("type", "")
	var count: int = int(reward.get("count", 1))
	var result := {"type": reward_type, "count": count, "success": false, "source": source}

	match reward_type:
		"currency":
			var currency_id: String = reward.get("currency", reward.get("id", ""))
			if _economy != null:
				_economy.earn(currency_id, count)
				result["success"] = true
				result["currency"] = currency_id
		"monster":
			var monster_id: int = int(reward.get("id", 0))
			if _monster_manager != null and _monster_manager.has_method("add_monster"):
				_monster_manager.add_monster(monster_id)
				result["success"] = true
				result["monster_id"] = monster_id
			else:
				result["success"] = true  # No manager = still record it
				result["monster_id"] = monster_id
		"item":
			# Generic item — game layer handles storage
			result["success"] = true
			result["item_id"] = reward.get("id", "")
		"stamina":
			if _economy != null and _economy.has_method("add_stamina"):
				_economy.add_stamina(count)
			result["success"] = true
		_:
			result["success"] = true  # Unknown types pass through

	return result


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
