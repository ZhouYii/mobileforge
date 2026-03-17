class_name MFEventShop extends RefCounted
## Limited-time shop that uses event-specific currencies.
## Tracks purchase counts per item (some items have buy limits).
## State stored in PlayerState for persistence.


class ShopItem extends RefCounted:
	var id: int
	var name: String
	var type: String  ## "monster", "currency", "item"
	var item_id: int  ## monster_id, or 0 for currency
	var currency_reward: String  ## only for type="currency" — which currency to grant
	var count: int  ## How many of the item per purchase
	var cost_currency: String  ## Event currency name
	var cost_amount: int
	var buy_limit: int  ## Max purchases (0 = unlimited)

	func _init(data: Dictionary = {}) -> void:
		id = int(data.get("id", 0))
		name = str(data.get("name", ""))
		type = str(data.get("type", "item"))
		item_id = int(data.get("item_id", 0))
		currency_reward = str(data.get("currency_reward", ""))
		count = int(data.get("count", 1))
		cost_currency = str(data.get("cost_currency", "event_tokens"))
		cost_amount = int(data.get("cost_amount", 1))
		buy_limit = int(data.get("buy_limit", 0))


class EventShopDef extends RefCounted:
	var id: int
	var name: String
	var currency: String  ## The event currency this shop uses
	var items: Array  ## Array[ShopItem]

	func _init(data: Dictionary = {}) -> void:
		id = int(data.get("id", 0))
		name = str(data.get("name", ""))
		currency = str(data.get("currency", "event_tokens"))
		items = []
		for item_data in data.get("items", []):
			items.append(ShopItem.new(item_data))


class PurchaseResult extends RefCounted:
	var success: bool
	var error: String
	var item: ShopItem

	func _init(p_success: bool = false, p_error: String = "", p_item: ShopItem = null) -> void:
		success = p_success
		error = p_error
		item = p_item


var _player_state: Object
var _economy: Object  ## MFEconomy
var _section_name: StringName = &"event_shop"


func _init(player_state: Object = null, economy: Object = null) -> void:
	_player_state = player_state
	_economy = economy


## Attempt to purchase an item. Returns PurchaseResult.
func purchase(shop_def: EventShopDef, item_id: int) -> PurchaseResult:
	var item: ShopItem = null
	for si in shop_def.items:
		if si.id == item_id:
			item = si
			break
	if item == null:
		return PurchaseResult.new(false, "Item not found")

	# Check buy limit
	if item.buy_limit > 0:
		var bought := get_purchase_count(shop_def.id, item_id)
		if bought >= item.buy_limit:
			return PurchaseResult.new(false, "Purchase limit reached (%d/%d)" % [bought, item.buy_limit])

	# Check currency
	if _economy == null or not _economy.can_afford(item.cost_currency, item.cost_amount):
		return PurchaseResult.new(false, "Not enough %s (need %d)" % [item.cost_currency, item.cost_amount])

	# Spend currency
	_economy.spend(item.cost_currency, item.cost_amount)

	# Record purchase
	_increment_purchase_count(shop_def.id, item_id)

	return PurchaseResult.new(true, "", item)


## Get how many times an item has been purchased.
func get_purchase_count(shop_id: int, item_id: int) -> int:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var key := StringName("shop_%d_item_%d" % [shop_id, item_id])
	return int(section.get_value(key, 0))


## Reset all purchase counts for a shop (e.g., when event restarts).
func reset_shop(shop_id: int) -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	# Find and reset all keys matching this shop
	for key in section.keys():
		if str(key).begins_with("shop_%d_" % shop_id):
			section.set_value(key, 0)


func _increment_purchase_count(shop_id: int, item_id: int) -> void:
	_ensure_section()
	var section = _player_state.get_section(_section_name)
	var key := StringName("shop_%d_item_%d" % [shop_id, item_id])
	var current := int(section.get_value(key, 0))
	section.set_value(key, current + 1)


func _ensure_section() -> void:
	if _player_state != null and not _player_state.has_section(_section_name):
		_player_state.register_section(_section_name, {})
