# NetworkClient

## Purpose

NetworkClient is the HTTP request layer for MobileForge. It handles outgoing HTTP requests with automatic retry logic, authentication header injection, request queuing for offline scenarios, and response parsing. All network communication in the game flows through this single module.

## Design Rationale

- **Single choke point.** All HTTP traffic goes through NetworkClient. This makes it easy to add auth headers, logging, rate limiting, and offline handling in one place.
- **Automatic retry.** Transient failures (timeouts, 5xx errors) are retried with exponential backoff. The caller does not need to implement retry logic.
- **Offline queue.** When the device is offline, requests are queued and replayed when connectivity resumes. Critical requests (like save sync) are not lost.
- **Response parsing.** JSON responses are automatically parsed into dictionaries. Error responses are wrapped in a standardized error object.
- **Event-driven status.** NetworkClient emits events for connection state changes, enabling the UI to show/hide offline indicators.

## API Reference

### Godot (GDScript)

| Method | Params | Return | Description |
|---|---|---|---|
| `get_request(url: String, headers: Dictionary)` | URL, optional headers | `NetworkResponse` | HTTP GET request (async) |
| `post_request(url: String, body: Dictionary, headers: Dictionary)` | URL, JSON body, optional headers | `NetworkResponse` | HTTP POST request (async) |
| `put_request(url: String, body: Dictionary, headers: Dictionary)` | URL, JSON body, optional headers | `NetworkResponse` | HTTP PUT request (async) |
| `delete_request(url: String, headers: Dictionary)` | URL, optional headers | `NetworkResponse` | HTTP DELETE request (async) |
| `set_auth_token(token: String)` | auth token | `void` | Set the bearer token for all requests |
| `set_base_url(url: String)` | base URL | `void` | Set the base URL prepended to all paths |
| `set_retry_config(max_retries: int, base_delay: float)` | max attempts, base delay seconds | `void` | Configure retry behavior |
| `is_online()` | none | `bool` | Check current connectivity status |
| `queue_size()` | none | `int` | Number of requests in offline queue |
| `flush_queue()` | none | `void` | Force-send all queued requests |

### Unity (C#)

| Method | Params | Return | Description |
|---|---|---|---|
| `GetAsync(string url, Dictionary<string, string> headers)` | URL, optional headers | `Task<NetworkResponse>` | HTTP GET |
| `PostAsync(string url, Dictionary<string, object> body, Dictionary<string, string> headers)` | URL, body, headers | `Task<NetworkResponse>` | HTTP POST |
| `PutAsync(string url, Dictionary<string, object> body, Dictionary<string, string> headers)` | URL, body, headers | `Task<NetworkResponse>` | HTTP PUT |
| `DeleteAsync(string url, Dictionary<string, string> headers)` | URL, headers | `Task<NetworkResponse>` | HTTP DELETE |
| `SetAuthToken(string token)` | auth token | `void` | Set bearer token |
| `SetBaseUrl(string url)` | base URL | `void` | Set base URL |
| `SetRetryConfig(int maxRetries, float baseDelay)` | max attempts, delay | `void` | Configure retry |
| `IsOnline` | -- | `bool` | Connectivity status |
| `QueueSize` | -- | `int` | Offline queue size |
| `FlushQueue()` | none | `void` | Send queued requests |

## NetworkResponse

```gdscript
# Godot
class_name NetworkResponse extends RefCounted

var success: bool        # true if status code 2xx
var status_code: int     # HTTP status code
var body: Dictionary     # parsed JSON response
var error_message: String  # human-readable error (if failed)
var headers: Dictionary  # response headers
```

```csharp
// Unity
public class NetworkResponse
{
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, object> Body { get; set; }
    public string ErrorMessage { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}
```

## Retry Logic

Transient failures are retried automatically with exponential backoff:

```
Attempt 1: immediate
Attempt 2: wait base_delay seconds (default 1.0)
Attempt 3: wait base_delay * 2 seconds
Attempt 4: wait base_delay * 4 seconds
...up to max_retries (default 3)
```

Retryable conditions:
- Network timeout
- HTTP 408 (Request Timeout)
- HTTP 429 (Too Many Requests) -- uses `Retry-After` header if present
- HTTP 500, 502, 503, 504 (Server errors)

Non-retryable conditions:
- HTTP 400 (Bad Request)
- HTTP 401 (Unauthorized)
- HTTP 403 (Forbidden)
- HTTP 404 (Not Found)
- HTTP 409 (Conflict)

## Offline Queue

When the device is offline, requests are stored in a FIFO queue:

```
1. Request made while offline
2. Request added to queue with full context (method, url, body, headers)
3. Device comes back online (detected via connectivity check)
4. Queue is flushed: requests sent in order
5. Results are delivered to original callbacks
```

The queue persists in memory. It is not written to disk -- if the app is killed while offline, queued requests are lost. For critical operations (save sync), the caller should handle this by re-sending on next launch.

## Authentication

Set the auth token once. It is automatically injected as a `Bearer` token in the `Authorization` header of every request:

```gdscript
# Godot
NetworkClient.set_auth_token("eyJhbGciOiJIUzI1NiIs...")

# All subsequent requests include:
# Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

```csharp
// Unity
NetworkClient.SetAuthToken("eyJhbGciOiJIUzI1NiIs...");
```

To clear the token (e.g., on logout):

```gdscript
NetworkClient.set_auth_token("")
```

## Usage Examples

### Simple GET Request

```gdscript
# Godot
var response := await NetworkClient.get_request("/api/v1/player/profile")
if response.success:
    var profile: Dictionary = response.body
    print("Player name: ", profile["name"])
else:
    print("Error: ", response.error_message)
```

```csharp
// Unity
var response = await NetworkClient.GetAsync("/api/v1/player/profile");
if (response.Success)
{
    var profile = response.Body;
    Debug.Log($"Player name: {profile["name"]}");
}
else
{
    Debug.LogError($"Error: {response.ErrorMessage}");
}
```

### POST with Body

```gdscript
# Godot
var response := await NetworkClient.post_request("/api/v1/gacha/pull", {
    "banner_id": "featured_001",
    "pull_count": 10
})
if response.success:
    var results: Array = response.body["results"]
    EventBus.emit_event("gacha_pull_completed", {"results": results})
```

## Events Emitted

| Event | Payload | When |
|---|---|---|
| `network_status_changed` | `{ "online": bool }` | Connectivity state changes |
| `network_request_failed` | `{ "url": String, "status_code": int, "error": String }` | Request fails after all retries |
| `network_queue_flushed` | `{ "count": int, "success": int, "failed": int }` | Offline queue has been processed |

## Best Practices

1. **Always check `response.success`.** Never assume a request succeeded.
2. **Set `base_url` once during initialization.** Use relative paths in all requests.
3. **Handle 401 globally.** Subscribe to `network_request_failed` and redirect to login if status is 401.
4. **Do not store auth tokens in save files.** Tokens should come from a secure auth flow and be stored in platform-specific secure storage.
5. **Keep request bodies small.** Mobile networks are unreliable. Smaller payloads have higher success rates.
6. **Use retry for idempotent operations only.** GET, PUT, and DELETE are typically safe to retry. POST may need special handling to avoid duplicate actions.
