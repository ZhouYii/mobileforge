class_name MFIAPProvider extends RefCounted
## Strategy pattern interface for in-app purchase platforms.
## Game code sets the active provider (Google Play, Apple, Amazon, etc.).

signal purchase_completed(product_id: String, receipt: Dictionary)
signal purchase_failed(product_id: String, error: String)

var _provider: Object  ## Platform-specific provider (set via set_provider)


## Set the platform-specific IAP provider.
func set_provider(provider: Object) -> void:
	_provider = provider


## Get available products (calls provider.get_products()).
func get_products() -> Array:
	if _provider != null and _provider.has_method("get_products"):
		return _provider.get_products()
	return []


## Initiate a purchase (calls provider.purchase(product_id)).
func purchase(product_id: String) -> void:
	if _provider == null:
		purchase_failed.emit(product_id, "No IAP provider configured")
		return
	if _provider.has_method("purchase"):
		_provider.purchase(product_id)


## Restore previous purchases (calls provider.restore_purchases()).
func restore_purchases() -> void:
	if _provider != null and _provider.has_method("restore_purchases"):
		_provider.restore_purchases()


## Call from provider when purchase succeeds.
func on_purchase_success(product_id: String, receipt: Dictionary = {}) -> void:
	purchase_completed.emit(product_id, receipt)


## Call from provider when purchase fails.
func on_purchase_error(product_id: String, error: String) -> void:
	purchase_failed.emit(product_id, error)
