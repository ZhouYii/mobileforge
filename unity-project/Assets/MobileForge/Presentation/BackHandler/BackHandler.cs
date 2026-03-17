using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Android back button / Escape key handler.
    /// Maintains a stack of handlers. Top handler gets called first.
    /// Pure C# — input detection delegated to engine layer.
    /// </summary>
    public class BackHandler
    {
        private readonly List<HandlerEntry> _handlers = new();

        /// <summary>Push a back handler onto the stack.</summary>
        public void Push(string id, Func<bool> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers.Add(new HandlerEntry { Id = id, Handler = handler });
        }

        /// <summary>Remove a handler by id.</summary>
        public void Remove(string id)
        {
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (_handlers[i].Id == id)
                {
                    _handlers.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>Try to handle back press. Returns true if a handler consumed it.</summary>
        public bool HandleBack()
        {
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (_handlers[i].Handler.Invoke())
                    return true;
            }
            return false;
        }

        /// <summary>Clear all handlers.</summary>
        public void Clear() => _handlers.Clear();

        public int HandlerCount => _handlers.Count;

        private struct HandlerEntry
        {
            public string Id;
            public Func<bool> Handler;
        }
    }
}
