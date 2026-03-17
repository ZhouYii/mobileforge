using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// HTTP client abstraction. Manages base URL, auth token, and request dispatch.
    /// Actual HTTP transport is injected via a delegate — this class does not depend
    /// on UnityWebRequest or any engine-specific networking.
    /// </summary>
    public class NetworkClient
    {
        private string _baseUrl = "";
        private string _authToken;
        private readonly Dictionary<string, string> _defaultHeaders = new();
        private readonly RequestQueue _offlineQueue = new();

        /// <summary>
        /// Whether the client considers itself online.
        /// When false, requests are queued instead of sent.
        /// </summary>
        public bool IsOnline { get; set; } = true;

        /// <summary>
        /// Number of requests currently queued for offline replay.
        /// </summary>
        public int QueuedRequestCount => _offlineQueue.Size;

        /// <summary>
        /// Injected HTTP transport. Must be set before making requests.
        /// Parameters: (url, method, body, headers, callback).
        /// </summary>
        public Action<string, string, string, Dictionary<string, string>, Action<Response>> Transport { get; set; }

        /// <summary>
        /// Set the base URL for all requests (e.g., "https://api.example.com/v1").
        /// </summary>
        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? "";
        }

        /// <summary>
        /// Set the authentication token. Added as "Authorization: Bearer {token}" header.
        /// Pass null to clear.
        /// </summary>
        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        /// <summary>
        /// Set a default header that is included in every request.
        /// </summary>
        public void SetDefaultHeader(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (value == null)
                _defaultHeaders.Remove(key);
            else
                _defaultHeaders[key] = value;
        }

        /// <summary>
        /// Send an HTTP request.
        /// </summary>
        /// <param name="endpoint">API endpoint path (appended to base URL).</param>
        /// <param name="method">HTTP method (GET, POST, PUT, DELETE, etc.).</param>
        /// <param name="body">Request body (JSON string, may be null for GET).</param>
        /// <param name="headers">Additional headers for this request (may be null).</param>
        /// <param name="callback">Callback invoked with the response.</param>
        public void Request(string endpoint, string method = "GET", string body = null,
            Dictionary<string, string> headers = null, Action<Response> callback = null)
        {
            var mergedHeaders = BuildHeaders(headers);
            var url = BuildUrl(endpoint);

            if (!IsOnline)
            {
                _offlineQueue.Enqueue(endpoint, method, body, mergedHeaders, callback);
                return;
            }

            if (Transport == null)
            {
                callback?.Invoke(Response.Failure(0, "No HTTP transport configured."));
                return;
            }

            Transport.Invoke(url, method, body, mergedHeaders, callback);
        }

        /// <summary>
        /// Convenience: GET request.
        /// </summary>
        public void Get(string endpoint, Action<Response> callback,
            Dictionary<string, string> headers = null)
        {
            Request(endpoint, "GET", null, headers, callback);
        }

        /// <summary>
        /// Convenience: POST request.
        /// </summary>
        public void Post(string endpoint, string body, Action<Response> callback,
            Dictionary<string, string> headers = null)
        {
            Request(endpoint, "POST", body, headers, callback);
        }

        /// <summary>
        /// Replay all queued offline requests. Call when connectivity is restored.
        /// </summary>
        public void FlushQueue()
        {
            while (!_offlineQueue.IsEmpty)
            {
                var req = _offlineQueue.Dequeue();
                if (req == null) break;

                var url = BuildUrl(req.Endpoint);
                var mergedHeaders = BuildHeaders(req.Headers);

                if (Transport != null)
                {
                    Transport.Invoke(url, req.Method, req.Body, mergedHeaders, req.Callback);
                }
                else
                {
                    req.Callback?.Invoke(Response.Failure(0, "No HTTP transport configured."));
                }
            }
        }

        /// <summary>
        /// Clear the offline queue without sending.
        /// </summary>
        public void ClearQueue()
        {
            _offlineQueue.Clear();
        }

        private string BuildUrl(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return _baseUrl;

            var sep = endpoint.StartsWith("/") ? "" : "/";
            return _baseUrl + sep + endpoint;
        }

        private Dictionary<string, string> BuildHeaders(Dictionary<string, string> extra)
        {
            var result = new Dictionary<string, string>(_defaultHeaders);

            if (!string.IsNullOrEmpty(_authToken))
                result["Authorization"] = "Bearer " + _authToken;

            if (extra != null)
            {
                foreach (var kvp in extra)
                    result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }
}
