class_name MFDeepLinkRouter extends RefCounted
## Route registration + URL parsing for push notifications and deep links.

var _routes: Dictionary = {}  # pattern -> Callable(params: Dictionary)


## Register a route handler. Pattern uses {param} placeholders.
## e.g. "event/{event_id}" matches "event/123" -> {event_id: "123"}
func register(pattern: String, handler: Callable) -> void:
	_routes[pattern] = handler


## Handle a deep link URL. Returns true if a route matched.
func handle(url: String) -> bool:
	# Strip scheme (myapp://)
	var path := url
	var scheme_end := url.find("://")
	if scheme_end >= 0:
		path = url.substr(scheme_end + 3)

	# Strip leading/trailing slashes
	path = path.strip_edges().trim_prefix("/").trim_suffix("/")

	for pattern in _routes:
		var params := _match_pattern(pattern, path)
		if params != null:
			_routes[pattern].call(params)
			return true
	return false


func _match_pattern(pattern: String, path: String) -> Variant:
	var pattern_parts: Array = pattern.split("/")
	var path_parts: Array = path.split("/")
	if pattern_parts.size() != path_parts.size():
		return null
	var params: Dictionary = {}
	for i in range(pattern_parts.size()):
		var pp: String = pattern_parts[i]
		if pp.begins_with("{") and pp.ends_with("}"):
			var key := pp.substr(1, pp.length() - 2)
			params[key] = path_parts[i]
		elif pp != path_parts[i]:
			return null
	return params
