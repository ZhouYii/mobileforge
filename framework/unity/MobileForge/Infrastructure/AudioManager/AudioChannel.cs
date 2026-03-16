using System;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Audio channel with volume and mute state.
    /// Channels are named groups (e.g., "sfx", "bgm", "voice") that
    /// allow independent volume control.
    /// </summary>
    public class AudioChannel
    {
        private float _volume = 1.0f;

        /// <summary>
        /// Channel name (e.g., "sfx", "bgm", "voice").
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Channel volume (0.0 to 1.0). Clamped on set.
        /// </summary>
        public float Volume
        {
            get => _volume;
            set => _volume = Math.Max(0f, Math.Min(1f, value));
        }

        /// <summary>
        /// Whether this channel is muted.
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Effective volume: 0 if muted, otherwise the channel volume.
        /// </summary>
        public float EffectiveVolume => IsMuted ? 0f : _volume;

        /// <summary>
        /// Fired when volume or mute state changes.
        /// Parameter: this channel.
        /// </summary>
        public Action<AudioChannel> OnChanged;

        public AudioChannel(string name, float volume = 1.0f, bool isMuted = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _volume = Math.Max(0f, Math.Min(1f, volume));
            IsMuted = isMuted;
        }

        /// <summary>
        /// Set volume and notify listeners.
        /// </summary>
        public void SetVolume(float volume)
        {
            Volume = volume;
            OnChanged?.Invoke(this);
        }

        /// <summary>
        /// Set muted state and notify listeners.
        /// </summary>
        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            OnChanged?.Invoke(this);
        }

        public override string ToString()
        {
            return $"AudioChannel({Name}: vol={_volume:F2}, muted={IsMuted})";
        }
    }
}
