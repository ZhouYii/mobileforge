class_name MFMonsterExchange extends RefCounted
## Monster exchange system — trade N monsters of min rarity for a guaranteed specific monster.
## Tracks exchange counts per offer (some have limits).


class ExchangeOffer extends RefCounted:
	var id: int
	var name: String
	var target_monster_id: int  ## Monster you receive
	var required_count: int  ## Number of monsters to sacrifice
	var required_min_rarity: int  ## Minimum rarity of sacrificed monsters
	var required_element: int  ## 0 = any element; >0 = specific element required
	var exchange_limit: int  ## Max times this exchange can be done (0 = unlimited)

	func _init(data: Dictionary = {}) -> void:
		id = int(data.get("id", 0))
		name = str(data.get("name", ""))
		target_monster_id = int(data.get("target_monster_id", 0))
		required_count = int(data.get("required_count", 5))
		required_min_rarity = int(data.get("required_min_rarity", 4))
		required_element = int(data.get("required_element", 0))
		exchange_limit = int(data.get("exchange_limit", 0))


class ExchangeResult extends RefCounted:
	var success: bool
	var error: String
	var received_monster_id: int

	func _init(p_success: bool = false, p_error: String = "", p_monster_id: int = 0) -> void:
		success = p_success
		error = p_error
		received_monster_id = p_monster_id


var _player_state: Object
var _section_name: StringName = &"monster_exchange"


func _init(player_state: Object = null) -> void:
	_player_state = player_state


## Validate an exchange: check that offered monsters meet the requirements.
## Returns empty string on success, error message on failure.
func validate(offer: ExchangeOffer, offered_monsters: Array, monster_manager: RefCounted) -> String:
	if offered_monsters.size() != offer.required_count:
		return "Need exactly %d monsters (got %d)" % [offer.required_count, offered_monsters.size()]

	# Check limit
	if offer.exchange_limit > 0:
		var done := get_exchange_count(offer.id)
		if done >= offer.exchange_limit:
			return "Exchange limit reached (%d/%d)" % [done, offer.exchange_limit]

	# Check each offered monster meets requirements
	for inst in offered_monsters:
		var def = monster_manager.get_def(inst.def_id)
		if def == null:
			return "Invalid monster"
		if def.rarity < offer.required_min_rarity:
			return "%s is below minimum rarity (%d★ required)" % [def.name, offer.required_min_rarity]
		if offer.required_element > 0 and def.element != offer.required_element:
			return "%s is wrong element" % def.name
		if inst.is_favorite:
			return "%s is favorited (unfavorite first)" % def.name

	# Check for duplicates (same instance_id)
	var seen: Dictionary = {}
	for inst in offered_monsters:
		if seen.has(inst.instance_id):
			return "Cannot offer the same monster twice"
		seen[inst.instance_id] = true

	return ""


## Execute an exchange. Caller is responsible for removing offered monsters and adding the received one.
## Returns ExchangeResult.
func execute(offer: ExchangeOffer, offered_monsters: Array, monster_manager: RefCounted) -> ExchangeResult:
	var error := validate(offer, offered_monsters, monster_manager)
	if error != "":
		return ExchangeResult.new(false, error)

	# Record exchange
	_increment_exchange_count(offer.id)
	return ExchangeResult.new(true, "", offer.target_monster_id)


func get_exchange_count(offer_id: int) -> int:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	return int(section.get_value(StringName("exchange_%d" % offer_id), 0))


func _increment_exchange_count(offer_id: int) -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var key := StringName("exchange_%d" % offer_id)
	section.set_value(key, int(section.get_value(key, 0)) + 1)


func _ensure_section() -> void:
	if _player_state != null and not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})
