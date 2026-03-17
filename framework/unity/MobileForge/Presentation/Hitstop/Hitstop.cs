using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Hit pause / hitstop: freeze the scene for N frames.
    /// Pure C# — delegates time scale control to engine layer.
    /// </summary>
    public class Hitstop
    {
        private int _freezeFrames;
        private bool _isFrozen;

        public bool IsFrozen => _isFrozen;

        /// <summary>Injected: set time scale. (scale)</summary>
        public Action<float> OnSetTimeScale { get; set; }

        /// <summary>Freeze for a number of frames.</summary>
        public void Freeze(int frames = 4)
        {
            if (_isFrozen) return;
            _freezeFrames = frames;
            _isFrozen = true;
            OnSetTimeScale?.Invoke(0f);
        }

        /// <summary>Freeze for a duration in seconds.</summary>
        public void FreezeSeconds(float duration = 0.067f)
        {
            Freeze((int)(duration * 60f));
        }

        /// <summary>Call each frame (unscaled). Decrements freeze counter.</summary>
        public void Update()
        {
            if (!_isFrozen) return;
            _freezeFrames--;
            if (_freezeFrames <= 0)
            {
                _isFrozen = false;
                OnSetTimeScale?.Invoke(1f);
            }
        }
    }
}
