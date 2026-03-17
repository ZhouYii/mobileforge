class_name MFBattlePass extends RefCounted
## Tiered free + premium battle/season pass.
## Tracks XP, tier progress, and reward claims. Depends on RewardPipeline.

var _reward_pipeline: Object  # MFRewardPipeline
var _player_state: Object
var _event_bus: Object
var _section_name: StringName = &"battle_pass"


class PassDef extends RefCounted:
	var id: String
	var name: String
	var tiers: Array  ## Array[TierDef]
	var xp_per_tier: int
	var end_time: int  ## Unix timestamp

	func _init(data: Dictionary = {}) -> void:
		id = str(data.get("id", ""))
		name = str(data.get("name", ""))
		xp_per_tier = int(data.get("xp_per_tier", 100))
		end_time = int(data.get("end_time", 0))
		tiers = []
		for tier_data in data.get("tiers", []):
			tiers.append(TierDef.new(tier_data))


class TierDef extends RefCounted:
	var tier: int
	var free_rewards: Array  ## Array[Dictionary] — reward defs
	var premium_rewards: Array

	func _init(data: Dictionary = {}) -> void:
		tier = int(data.get("tier", 0))
		free_rewards = data.get("free_rewards", [])
		premium_rewards = data.get("premium_rewards", [])


func _init(reward_pipeline: Object = null, player_state: Object = null, event_bus: Object = null) -> void:
	_reward_pipeline = reward_pipeline
	_player_state = player_state
	_event_bus = event_bus


## Add XP to the battle pass. Returns number of tiers gained.
func add_xp(pass_def: PassDef, xp: int) -> int:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var current_xp: int = int(section.get_value(&"xp", 0))
	var old_tier := current_xp / pass_def.xp_per_tier
	current_xp += xp
	section.set_value(&"xp", current_xp)
	var new_tier := current_xp / pass_def.xp_per_tier
	if new_tier > old_tier:
		_emit("battle_pass_tier_up", {"old_tier": old_tier, "new_tier": new_tier})
	return new_tier - old_tier


## Get current tier (0-indexed).
func get_tier(pass_def: PassDef) -> int:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var xp: int = int(section.get_value(&"xp", 0))
	return mini(xp / pass_def.xp_per_tier, pass_def.tiers.size() - 1)


## Claim rewards for a tier. track = "free" or "premium".
func claim(pass_def: PassDef, tier_index: int, track: String = "free") -> Dictionary:
	var current_tier := get_tier(pass_def)
	if tier_index > current_tier:
		return {"success": false, "error": "tier_not_reached"}

	var claim_key := StringName("claimed_%s_%d" % [track, tier_index])
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	if bool(section.get_value(claim_key, false)):
		return {"success": false, "error": "already_claimed"}

	if track == "premium" and not is_premium():
		return {"success": false, "error": "premium_required"}

	if tier_index >= pass_def.tiers.size():
		return {"success": false, "error": "invalid_tier"}

	var tier_def: TierDef = pass_def.tiers[tier_index]
	var rewards: Array = tier_def.free_rewards if track == "free" else tier_def.premium_rewards
	var granted: Array = []
	if _reward_pipeline != null and not rewards.is_empty():
		granted = _reward_pipeline.grant(rewards, "battle_pass:" + track)

	section.set_value(claim_key, true)
	_emit("battle_pass_claimed", {"tier": tier_index, "track": track})
	return {"success": true, "rewards_granted": granted}


## Whether the player has the premium pass.
func is_premium() -> bool:
	_ensure_section()
	if _player_state == null:
		return false
	var section = _player_state.get_section(_section_name)
	return bool(section.get_value(&"is_premium", false))


## Activate premium pass.
func activate_premium() -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	section.set_value(&"is_premium", true)
	_emit("battle_pass_premium_activated", {})


func _ensure_section() -> void:
	if _player_state != null and not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
