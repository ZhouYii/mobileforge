class_name MFRequestQueue extends RefCounted
## Offline request queue. Stores requests when offline, replays when online.

var _queue: Array = []  # Array of {url, method, body, headers, callback}
var _max_size: int = 100

func enqueue(url: String, method: String, body: String, headers: Dictionary, callback: Callable) -> void:
    if _queue.size() >= _max_size:
        _queue.pop_front()  # Drop oldest
    _queue.append({"url": url, "method": method, "body": body, "headers": headers, "callback": callback})

func dequeue() -> Dictionary:
    if _queue.is_empty():
        return {}
    return _queue.pop_front()

func size() -> int:
    return _queue.size()

func is_empty() -> bool:
    return _queue.is_empty()

func clear() -> void:
    _queue.clear()
