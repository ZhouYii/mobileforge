using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Fluent builder that chains popups sequentially through PopupStack.
    /// Usage: await dialogQueue.Chain("maint", f1).Chain("login", f2).Run();
    /// </summary>
    public class DialogQueue
    {
        private readonly PopupStack _popupStack;
        private readonly List<DialogEntry> _entries = new();
        private bool _running;

        public bool IsRunning => _running;

        public DialogQueue(PopupStack popupStack)
        {
            _popupStack = popupStack ?? throw new ArgumentNullException(nameof(popupStack));
        }

        /// <summary>Add a popup to the chain. Returns self for fluent chaining.</summary>
        public DialogQueue Chain(string popupId, Func<IPopup> factory,
            Dictionary<string, object> parameters = null, int priority = 0)
        {
            _entries.Add(new DialogEntry
            {
                PopupId = popupId,
                Factory = factory,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Priority = priority,
            });
            return this;
        }

        /// <summary>Run all chained popups sequentially.</summary>
        public async Task Run()
        {
            if (_running || _entries.Count == 0) return;
            _running = true;
            foreach (var entry in _entries)
            {
                await _popupStack.ShowAwait(entry.PopupId, entry.Factory, entry.Parameters, entry.Priority);
            }
            _entries.Clear();
            _running = false;
        }

        /// <summary>Clear queued popups without running them.</summary>
        public void Clear() => _entries.Clear();

        private struct DialogEntry
        {
            public string PopupId;
            public Func<IPopup> Factory;
            public Dictionary<string, object> Parameters;
            public int Priority;
        }
    }
}
