class_name MFShop extends RefCounted
## Generalized shop system. Multi-section, rotating inventory, timed bundles.
## Extends the EventShop pattern to support permanent shops and server-driven catalogs.

var _economy: Object  # MFEconomy
var _reward_pipeline: Object  # MFRewardPipeline
var _player_state: Object
var _event_bus: Object
var _section_name: StringName = &"shop"


class ShopSection extends RefCounted:
	var id: String
	var name: String
	var items: Array  ## Array[ShopItem]
	var refresh_interval: int  ## Seconds. 0 = permanent
	var last_refresh_time: int  ## Unix timestamp

	func _init(data: Dictionary = {}) -> void:
		id = str(data.get("id", ""))
		name = str(data.get("name", ""))
		items = []
		for item_data in data.get("items", []):
			items.append(ShopItem.new(item_data))
		refresh_interval = int(data.get("refresh_interval", 0))
		last_refresh_time = int(data.get("last_refresh_time", 0))


class ShopItem extends RefCounted:
	var id: String
	var name: String
	var cost_type: String  ## "currency", "iap"
	var cost_currency: String
	var cost_amount: int
	var iap_product_id: String  ## For IAP items
	var rewards: Array  ## Array[Dictionary] — passed to RewardPipeline
	var buy_limit: int  ## 0 = unlimited
	var available_until: int  ## Unix timestamp. 0 = permanent
	var tags: Array  ## ["featured", "limited", "bundle"]

	func _init(data: Dictionary = {}) -> void:
		id = str(data.get("id", ""))
		name = str(data.get("name", ""))
		cost_type = str(data.get("cost_type", "currency"))
		cost_currency = str(data.get("cost_currency", "gems"))
		cost_amount = int(data.get("cost_amount", 0))
		iap_product_id = str(data.get("iap_product_id", ""))
		rewards = data.get("rewards", [])
		buy_limit = int(data.get("buy_limit", 0))
		available_until = int(data.get("available_until", 0))
		tags = []
		for tag in data.get("tags", []):
			tags.append(str(tag))


func _init(economy: Object = null, reward_pipeline: Object = null, player_state: Object = null, event_bus: Object = null) -> void:
	_economy = economy
	_reward_pipeline = reward_pipeline
	_player_state = player_state
	_event_bus = event_bus


## Purchase an item from a section. Returns {success, error, rewards_granted}.
func purchase(section: ShopSection, item_id: String) -> Dictionary:
	var item: ShopItem = null
	for si in section.items:
		if si.id == item_id:
			item = si
			break
	if item == null:
		return {"success": false, "error": "item_not_found"}

	# Check availability
	if item.available_until > 0:
		var now := int(Time.get_unix_time_from_system())
		if now > item.available_until:
			return {"success": false, "error": "item_expired"}

	# Check buy limit
	if item.buy_limit > 0:
		var bought := _get_purchase_count(section.id, item_id)
		if bought >= item.buy_limit:
			return {"success": false, "error": "limit_reached"}

	# Check/spend currency
	if item.cost_type == "currency":
		if _economy == null or not _economy.can_afford(item.cost_currency, item.cost_amount):
			return {"success": false, "error": "insufficient_funds"}
		_economy.spend(item.cost_currency, item.cost_amount)
	elif item.cost_type == "iap":
		return {"success": false, "error": "iap_requires_platform", "iap_product_id": item.iap_product_id}

	# Grant rewards
	var granted: Array = []
	if _reward_pipeline != null and not item.rewards.is_empty():
		granted = _reward_pipeline.grant(item.rewards, "shop:" + section.id)

	_increment_purchase_count(section.id, item_id)
	_emit("shop_purchase", {"section": section.id, "item": item_id})
	return {"success": true, "rewards_granted": granted}


func _get_purchase_count(section_id: String, item_id: String) -> int:
	if _player_state == null:
		return 0
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	return int(section.get_value(StringName("shop_%s_%s" % [section_id, item_id]), 0))


func _increment_purchase_count(section_id: String, item_id: String) -> void:
	if _player_state == null:
		return
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var key := StringName("shop_%s_%s" % [section_id, item_id])
	section.set_value(key, int(section.get_value(key, 0)) + 1)


func _ensure_section() -> void:
	if _player_state != null and not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})


func _emit(event_name: StringName, payload: Dictionary) -> void:
	if _event_bus != null and _event_bus.has_method("emit_event"):
		_event_bus.emit_event(event_name, payload)
