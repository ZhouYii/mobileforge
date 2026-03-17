class_name MFRewardedAds extends RefCounted
## Strategy pattern for rewarded ad networks.
## Game code sets the platform-specific provider.

signal ad_completed(placement_id: String)
signal ad_failed(placement_id: String, error: String)
signal ad_skipped(placement_id: String)

var _provider: Object  ## Platform-specific ad provider


func set_provider(provider: Object) -> void:
	_provider = provider


## Check if an ad is ready for a placement.
func is_ready(placement_id: String) -> bool:
	if _provider != null and _provider.has_method("is_ready"):
		return _provider.is_ready(placement_id)
	return false


## Show a rewarded ad. Listen for ad_completed signal.
func show(placement_id: String) -> void:
	if _provider == null:
		ad_failed.emit(placement_id, "No ad provider configured")
		return
	if _provider.has_method("show"):
		_provider.show(placement_id)


## Load/preload an ad for a placement.
func load_ad(placement_id: String) -> void:
	if _provider != null and _provider.has_method("load_ad"):
		_provider.load_ad(placement_id)


## Callbacks for ad provider to call:
func on_ad_completed(placement_id: String) -> void:
	ad_completed.emit(placement_id)

func on_ad_failed(placement_id: String, error: String) -> void:
	ad_failed.emit(placement_id, error)

func on_ad_skipped(placement_id: String) -> void:
	ad_skipped.emit(placement_id)
