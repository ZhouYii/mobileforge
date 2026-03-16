class_name MFResponse extends RefCounted
## HTTP response wrapper.

var status_code: int
var body: String
var headers: Dictionary
var is_success: bool
var error: String

func _init(p_status: int = 0, p_body: String = "", p_headers: Dictionary = {}, p_error: String = "") -> void:
    status_code = p_status
    body = p_body
    headers = p_headers
    error = p_error
    is_success = p_status >= 200 and p_status < 300

func parse_json() -> Variant:
    if body.is_empty():
        return null
    return JSON.parse_string(body)
