using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Offline request queue. Stores requests when the network is unavailable
    /// and replays them when connectivity is restored.
    /// </summary>
    public class RequestQueue
    {
        private readonly Queue<QueuedRequest> _queue = new();

        /// <summary>
        /// Number of queued requests.
        /// </summary>
        public int Size => _queue.Count;

        /// <summary>
        /// Whether the queue is empty.
        /// </summary>
        public bool IsEmpty => _queue.Count == 0;

        /// <summary>
        /// Enqueue a request for later execution.
        /// </summary>
        /// <param name="endpoint">API endpoint path.</param>
        /// <param name="method">HTTP method (GET, POST, etc.).</param>
        /// <param name="body">Request body (may be null).</param>
        /// <param name="headers">Additional headers (may be null).</param>
        /// <param name="callback">Callback to invoke when the request eventually completes.</param>
        public void Enqueue(string endpoint, string method, string body,
            Dictionary<string, string> headers, Action<Response> callback)
        {
            _queue.Enqueue(new QueuedRequest
            {
                Endpoint = endpoint,
                Method = method,
                Body = body,
                Headers = headers != null ? new Dictionary<string, string>(headers) : null,
                Callback = callback,
                EnqueuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            });
        }

        /// <summary>
        /// Dequeue the next request. Returns null if the queue is empty.
        /// </summary>
        public QueuedRequest Dequeue()
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }

        /// <summary>
        /// Peek at the next request without removing it. Returns null if empty.
        /// </summary>
        public QueuedRequest Peek()
        {
            return _queue.Count > 0 ? _queue.Peek() : null;
        }

        /// <summary>
        /// Clear all queued requests. Callbacks are not invoked.
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
        }

        /// <summary>
        /// Represents a single queued request.
        /// </summary>
        public class QueuedRequest
        {
            public string Endpoint;
            public string Method;
            public string Body;
            public Dictionary<string, string> Headers;
            public Action<Response> Callback;
            public long EnqueuedAt;
        }
    }
}
