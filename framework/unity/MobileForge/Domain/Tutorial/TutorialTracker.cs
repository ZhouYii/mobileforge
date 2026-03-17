using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Domain-side tutorial state machine.
    /// Tracks completion, current step, per-feature phases.
    /// </summary>
    public class TutorialTracker
    {
        private readonly Dictionary<string, TutorialDef> _tutorials = new();
        private readonly HashSet<string> _completed = new();
        private readonly Action<string, Dictionary<string, object>> _emitEvent;
        private string _activeTutorial = "";
        private int _currentStep;

        public string ActiveId => _activeTutorial;

        public TutorialTracker(Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _emitEvent = emitEvent;
        }

        /// <summary>Register a tutorial definition.</summary>
        public void Register(string tutorialId, List<Dictionary<string, object>> steps)
        {
            _tutorials[tutorialId] = new TutorialDef { Id = tutorialId, Steps = steps };
        }

        /// <summary>Start a tutorial if not completed.</summary>
        public bool Start(string tutorialId)
        {
            if (_completed.Contains(tutorialId)) return false;
            if (!_tutorials.ContainsKey(tutorialId)) return false;
            _activeTutorial = tutorialId;
            _currentStep = 0;
            _emitEvent?.Invoke("tutorial_started", new Dictionary<string, object>
            {
                ["tutorial_id"] = tutorialId,
            });
            return true;
        }

        /// <summary>Advance to next step. Returns true if there's a next step.</summary>
        public bool Advance()
        {
            if (_activeTutorial == "" || !_tutorials.TryGetValue(_activeTutorial, out var tut))
                return false;
            _currentStep++;
            if (_currentStep >= tut.Steps.Count)
            {
                Complete(_activeTutorial);
                return false;
            }
            _emitEvent?.Invoke("tutorial_step", new Dictionary<string, object>
            {
                ["tutorial_id"] = _activeTutorial, ["step_index"] = _currentStep,
            });
            return true;
        }

        /// <summary>Get the current step definition.</summary>
        public Dictionary<string, object> GetCurrentStep()
        {
            if (_activeTutorial == "" || !_tutorials.TryGetValue(_activeTutorial, out var tut))
                return new Dictionary<string, object>();
            if (_currentStep < tut.Steps.Count)
                return tut.Steps[_currentStep];
            return new Dictionary<string, object>();
        }

        public void Skip()
        {
            if (_activeTutorial != "") Complete(_activeTutorial);
        }

        public bool IsCompleted(string tutorialId) => _completed.Contains(tutorialId);

        /// <summary>Mark completed from loaded state.</summary>
        public void MarkCompleted(string tutorialId) => _completed.Add(tutorialId);

        private void Complete(string tutorialId)
        {
            _completed.Add(tutorialId);
            _activeTutorial = "";
            _emitEvent?.Invoke("tutorial_completed", new Dictionary<string, object>
            {
                ["tutorial_id"] = tutorialId,
            });
        }

        private class TutorialDef
        {
            public string Id;
            public List<Dictionary<string, object>> Steps;
        }
    }
}
