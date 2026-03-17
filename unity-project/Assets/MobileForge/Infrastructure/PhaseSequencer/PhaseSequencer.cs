using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Linear ordered phase pipeline with enter/exit/update and auto-advance modes.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class PhaseSequencer
    {
        private readonly List<PhaseDef> _phases = new();
        private int _currentIndex = -1;
        private float _elapsed;
        private bool _running;
        private bool _barrierConnected;

        public event Action<string> PhaseEntered;
        public event Action<string> PhaseExited;
        public event Action SequenceCompleted;

        /// <summary>Add a phase definition.</summary>
        public void AddPhase(PhaseDef phase)
        {
            if (phase == null) throw new ArgumentNullException(nameof(phase));
            _phases.Add(phase);
        }

        /// <summary>Start the sequence from the first phase.</summary>
        public void Start()
        {
            if (_phases.Count == 0)
            {
                SequenceCompleted?.Invoke();
                return;
            }
            _running = true;
            _currentIndex = -1;
            EnterNext();
        }

        /// <summary>Manually advance to the next phase.</summary>
        public void Advance()
        {
            if (!_running) return;
            ExitCurrent();
            EnterNext();
        }

        /// <summary>Call every frame to tick duration-based phases and on_update callbacks.</summary>
        public void Update(float delta)
        {
            if (!_running || _currentIndex < 0 || _currentIndex >= _phases.Count)
                return;

            var phase = _phases[_currentIndex];
            phase.OnUpdate?.Invoke(delta);

            if (phase.Duration > 0f)
            {
                _elapsed += delta;
                if (_elapsed >= phase.Duration)
                    Advance();
            }
        }

        /// <summary>Current phase ID, or null if not running.</summary>
        public string CurrentPhaseId =>
            _currentIndex >= 0 && _currentIndex < _phases.Count
                ? _phases[_currentIndex].Id
                : null;

        /// <summary>Current phase index (0-based), or -1 if not running.</summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>Whether the sequence has completed all phases.</summary>
        public bool IsComplete => !_running && _currentIndex >= _phases.Count;

        /// <summary>Whether the sequencer is currently running.</summary>
        public bool IsRunning => _running;

        /// <summary>Reset to initial state. Exits current phase if running.</summary>
        public void Reset()
        {
            if (_running)
                ExitCurrent();
            _currentIndex = -1;
            _elapsed = 0f;
            _running = false;
        }

        private void EnterNext()
        {
            _currentIndex++;
            if (_currentIndex >= _phases.Count)
            {
                _running = false;
                SequenceCompleted?.Invoke();
                return;
            }

            _elapsed = 0f;
            var phase = _phases[_currentIndex];
            PhaseEntered?.Invoke(phase.Id);
            phase.OnEnter?.Invoke();

            if (phase.AutoAdvance)
            {
                Advance();
                return;
            }

            if (phase.Barrier != null && !phase.Barrier.IsResolved)
            {
                _barrierConnected = true;
                phase.Barrier.AllResolved += OnBarrierResolved;
            }
        }

        private void ExitCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _phases.Count)
                return;

            var phase = _phases[_currentIndex];

            if (_barrierConnected && phase.Barrier != null)
            {
                phase.Barrier.AllResolved -= OnBarrierResolved;
                _barrierConnected = false;
            }

            phase.OnExit?.Invoke();
            PhaseExited?.Invoke(phase.Id);
        }

        private void OnBarrierResolved()
        {
            _barrierConnected = false;
            Advance();
        }
    }
}
