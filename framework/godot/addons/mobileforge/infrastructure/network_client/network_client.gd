extends Node
## HTTP request client with retry and offline queue.
## Retries transient failures (5xx, connection errors) with exponential backoff.
## Queues requests when offline; call flush_queue() to replay when back online.

signal online_status_changed(is_online: bool)

var _base_url: String = ""
var _auth_token: String = ""
var _request_queue: MFRequestQueue
var _max_retries: int = 3
var _base_retry_delay: float = 1.0  # seconds; doubles each retry
var _is_online: bool = true
var _is_flushing: bool = false


func _ready() -> void:
	_request_queue = MFRequestQueue.new()


func set_base_url(url: String) -> void:
	_base_url = url.rstrip("/")


func set_auth_token(token: String) -> void:
	_auth_token = token


func set_max_retries(retries: int) -> void:
	_max_retries = maxi(retries, 0)


## Whether the client considers itself online.
func is_online() -> bool:
	return _is_online


## Manually set online status (e.g. from OS connectivity check).
func set_online(online: bool) -> void:
	if _is_online != online:
		_is_online = online
		online_status_changed.emit(online)
		if online:
			flush_queue()


## Number of requests waiting in the offline queue.
func queued_request_count() -> int:
	return _request_queue.size()


## Make an HTTP request. Returns a Response via callback.
## Automatically retries transient failures and queues if offline.
func request(endpoint: String, method: String = "GET", body: String = "",
		headers: Dictionary = {}, callback: Callable = Callable()) -> void:
	var url := _base_url + "/" + endpoint.lstrip("/")

	if _auth_token != "":
		headers["Authorization"] = "Bearer " + _auth_token
	headers["Content-Type"] = headers.get("Content-Type", "application/json")

	if not _is_online:
		# Queue immediately when offline
		_request_queue.enqueue(url, method, body, headers, callback)
		return

	_execute_with_retry(url, method, body, headers, callback, 0)


## Replay all queued requests. Called automatically when set_online(true).
func flush_queue() -> void:
	if _is_flushing or _request_queue.is_empty():
		return
	_is_flushing = true
	_flush_next()


func _flush_next() -> void:
	if _request_queue.is_empty() or not _is_online:
		_is_flushing = false
		return

	var entry: Dictionary = _request_queue.dequeue()
	if entry.is_empty():
		_is_flushing = false
		return

	var original_callback: Callable = entry.get("callback", Callable())

	# Wrap callback to chain the next flush
	_execute_with_retry(
		entry["url"], entry["method"], entry["body"], entry["headers"],
		func(resp: MFResponse):
			if original_callback.is_valid():
				original_callback.call(resp)
			# Continue flushing after this request completes
			_flush_next(),
		0
	)


func _execute_with_retry(url: String, method: String, body: String,
		headers: Dictionary, callback: Callable, attempt: int) -> void:
	var header_array: PackedStringArray = []
	for key in headers:
		header_array.append("%s: %s" % [key, headers[key]])

	var http_method := _parse_method(method)

	var http := HTTPRequest.new()
	add_child(http)

	http.request_completed.connect(func(result: int, code: int, resp_headers: PackedStringArray, resp_body: PackedByteArray):
		http.queue_free()

		# Check if this is a retryable failure
		var is_retryable := _is_retryable(result, code)

		if is_retryable and attempt < _max_retries:
			# Retry with exponential backoff
			var delay := _base_retry_delay * pow(2.0, attempt)
			var timer := get_tree().create_timer(delay)
			timer.timeout.connect(func():
				_execute_with_retry(url, method, body, headers, callback, attempt + 1))
			return

		# Build response
		var resp: MFResponse
		if result != HTTPRequest.RESULT_SUCCESS:
			resp = MFResponse.new(0, "", {}, "Connection failed (result: %d)" % result)
		else:
			resp = MFResponse.new(code, resp_body.get_string_from_utf8(), {})

		# If still failing after all retries, check if we should queue
		if not resp.is_success and is_retryable:
			# Mark offline and queue this + future requests
			set_online(false)
			_request_queue.enqueue(url, method, body, headers, callback)
			return

		if callback.is_valid():
			callback.call(resp)
	)

	var err := http.request(url, header_array, http_method, body)
	if err != OK:
		http.queue_free()
		if attempt < _max_retries:
			var delay := _base_retry_delay * pow(2.0, attempt)
			var timer := get_tree().create_timer(delay)
			timer.timeout.connect(func():
				_execute_with_retry(url, method, body, headers, callback, attempt + 1))
		else:
			set_online(false)
			_request_queue.enqueue(url, method, body, headers, callback)


## Returns true for failures that are worth retrying (server errors, connection issues).
static func _is_retryable(result: int, status_code: int) -> bool:
	# Connection-level failures are always retryable
	if result != HTTPRequest.RESULT_SUCCESS:
		return true
	# 5xx server errors are retryable
	if status_code >= 500:
		return true
	# 429 Too Many Requests is retryable
	if status_code == 429:
		return true
	return false


static func _parse_method(method: String) -> int:
	match method.to_upper():
		"POST": return HTTPClient.METHOD_POST
		"PUT": return HTTPClient.METHOD_PUT
		"DELETE": return HTTPClient.METHOD_DELETE
		"PATCH": return HTTPClient.METHOD_PATCH
		_: return HTTPClient.METHOD_GET
