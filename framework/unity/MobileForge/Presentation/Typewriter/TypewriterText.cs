using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Character-by-character text reveal with punctuation-aware delays.
    /// Pure C# — delegates character display to engine layer.
    /// </summary>
    public class TypewriterText
    {
        private string _fullText = "";
        private int _charIndex;
        private float _timer;
        private bool _isRevealing;
        private bool _skipRequested;

        public float CharsPerSecond { get; set; } = 30f;
        public float PunctuationDelay { get; set; } = 0.15f;
        public float ParagraphDelay { get; set; } = 0.3f;

        public bool IsRevealing => _isRevealing;

        /// <summary>Fired each time visible character count changes. (visibleCount)</summary>
        public Action<int> OnCharacterRevealed { get; set; }

        /// <summary>Fired when all characters are revealed.</summary>
        public event Action RevealCompleted;

        /// <summary>Start revealing text character by character.</summary>
        public void StartReveal(string text)
        {
            _fullText = text;
            _charIndex = 0;
            _timer = 0f;
            _isRevealing = true;
            _skipRequested = false;
            OnCharacterRevealed?.Invoke(0);
        }

        /// <summary>Skip to full reveal instantly.</summary>
        public void Skip()
        {
            if (_isRevealing) _skipRequested = true;
        }

        /// <summary>Call each frame with delta time.</summary>
        public void Update(float delta)
        {
            if (!_isRevealing) return;

            if (_skipRequested)
            {
                OnCharacterRevealed?.Invoke(_fullText.Length);
                _isRevealing = false;
                RevealCompleted?.Invoke();
                return;
            }

            _timer += delta;
            float interval = 1f / CharsPerSecond;

            while (_timer >= interval && _charIndex < _fullText.Length)
            {
                _charIndex++;
                _timer -= interval;
                OnCharacterRevealed?.Invoke(_charIndex);

                if (_charIndex < _fullText.Length)
                {
                    char ch = _fullText[_charIndex - 1];
                    if (".!?;:".IndexOf(ch) >= 0)
                        _timer -= PunctuationDelay;
                    else if (ch == '\n')
                        _timer -= ParagraphDelay;
                }
            }

            if (_charIndex >= _fullText.Length)
            {
                _isRevealing = false;
                RevealCompleted?.Invoke();
            }
        }
    }
}
