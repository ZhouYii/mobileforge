using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Audio manager. Manages named audio channels, preloading, and playback requests.
    /// Actual audio playback is delegated to engine-specific code via callbacks.
    /// Pure C# — no Unity AudioSource dependency.
    /// </summary>
    public class AudioManager
    {
        private readonly Dictionary<string, AudioChannel> _channels = new();
        private readonly HashSet<string> _preloaded = new();

        private string _currentBgm;

        /// <summary>
        /// Default channel names.
        /// </summary>
        public const string ChannelSfx = "sfx";
        public const string ChannelBgm = "bgm";

        /// <summary>
        /// Injected: preload an audio clip by key.
        /// </summary>
        public Action<string> OnPreload;

        /// <summary>
        /// Injected: play a sound effect. Parameters: (audioKey, volume).
        /// </summary>
        public Action<string, float> OnPlaySfx;

        /// <summary>
        /// Injected: play background music. Parameters: (audioKey, volume, loop).
        /// </summary>
        public Action<string, float, bool> OnPlayBgm;

        /// <summary>
        /// Injected: stop background music.
        /// </summary>
        public Action OnStopBgm;

        /// <summary>
        /// The currently playing BGM key, or null if none.
        /// </summary>
        public string CurrentBgm => _currentBgm;

        public AudioManager()
        {
            // Create default channels
            _channels[ChannelSfx] = new AudioChannel(ChannelSfx);
            _channels[ChannelBgm] = new AudioChannel(ChannelBgm);
        }

        /// <summary>
        /// Get or create a channel by name.
        /// </summary>
        public AudioChannel GetChannel(string channelName)
        {
            if (!_channels.TryGetValue(channelName, out var channel))
            {
                channel = new AudioChannel(channelName);
                _channels[channelName] = channel;
            }

            return channel;
        }

        /// <summary>
        /// Preload an audio clip so it is ready for instant playback.
        /// </summary>
        public void PreloadAudio(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey)) return;
            if (_preloaded.Contains(audioKey)) return;

            _preloaded.Add(audioKey);
            OnPreload?.Invoke(audioKey);
        }

        /// <summary>
        /// Play a sound effect on the SFX channel.
        /// </summary>
        /// <param name="audioKey">Key of the audio clip to play.</param>
        public void PlaySfx(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey)) return;

            var channel = GetChannel(ChannelSfx);
            float vol = channel.EffectiveVolume;

            if (vol <= 0f) return;

            OnPlaySfx?.Invoke(audioKey, vol);
        }

        /// <summary>
        /// Play background music. Stops any currently playing BGM first.
        /// </summary>
        /// <param name="audioKey">Key of the music clip to play.</param>
        /// <param name="loop">Whether to loop the music. Default true.</param>
        public void PlayBgm(string audioKey, bool loop = true)
        {
            if (string.IsNullOrEmpty(audioKey)) return;

            // Don't restart if same BGM is already playing
            if (_currentBgm == audioKey) return;

            if (_currentBgm != null)
                OnStopBgm?.Invoke();

            var channel = GetChannel(ChannelBgm);
            float vol = channel.EffectiveVolume;

            _currentBgm = audioKey;

            if (vol <= 0f) return;

            OnPlayBgm?.Invoke(audioKey, vol, loop);
        }

        /// <summary>
        /// Stop background music.
        /// </summary>
        public void StopBgm()
        {
            if (_currentBgm == null) return;

            _currentBgm = null;
            OnStopBgm?.Invoke();
        }

        /// <summary>
        /// Set the volume for a channel.
        /// </summary>
        public void SetChannelVolume(string channelName, float volume)
        {
            GetChannel(channelName).SetVolume(volume);
        }

        /// <summary>
        /// Set the muted state for a channel.
        /// </summary>
        public void SetChannelMuted(string channelName, bool muted)
        {
            GetChannel(channelName).SetMuted(muted);
        }

        /// <summary>
        /// Check if an audio key has been preloaded.
        /// </summary>
        public bool IsPreloaded(string audioKey)
        {
            return _preloaded.Contains(audioKey);
        }

        /// <summary>
        /// Get the number of channels.
        /// </summary>
        public int ChannelCount => _channels.Count;
    }
}
