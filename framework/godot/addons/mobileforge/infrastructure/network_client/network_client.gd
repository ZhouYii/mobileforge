extends Node
## HTTP request client with retry and offline queue.

var _base_url: String = ""
var _auth_token: String = ""
var _request_queue: MFRequestQueue
var _max_retries: int = 3

func _ready() -> void:
    _request_queue = MFRequestQueue.new()

func set_base_url(url: String) -> void:
    _base_url = url.rstrip("/")

func set_auth_token(token: String) -> void:
    _auth_token = token

## Make an HTTP request. Returns a Response via callback.
func request(endpoint: String, method: String = "GET", body: String = "", headers: Dictionary = {}, callback: Callable = Callable()) -> void:
    var url := _base_url + "/" + endpoint.lstrip("/")

    if _auth_token != "":
        headers["Authorization"] = "Bearer " + _auth_token
    headers["Content-Type"] = headers.get("Content-Type", "application/json")

    var http := HTTPRequest.new()
    add_child(http)

    var header_array: PackedStringArray = []
    for key in headers:
        header_array.append("%s: %s" % [key, headers[key]])

    var http_method := HTTPClient.METHOD_GET
    match method.to_upper():
        "POST": http_method = HTTPClient.METHOD_POST
        "PUT": http_method = HTTPClient.METHOD_PUT
        "DELETE": http_method = HTTPClient.METHOD_DELETE
        "PATCH": http_method = HTTPClient.METHOD_PATCH

    http.request_completed.connect(func(result, code, resp_headers, resp_body):
        http.queue_free()
        var resp := MFResponse.new(code, resp_body.get_string_from_utf8(), {})
        if callback.is_valid():
            callback.call(resp)
    )

    var err := http.request(url, header_array, http_method, body)
    if err != OK:
        http.queue_free()
        var resp := MFResponse.new(0, "", {}, "Request failed: %d" % err)
        if callback.is_valid():
            callback.call(resp)
