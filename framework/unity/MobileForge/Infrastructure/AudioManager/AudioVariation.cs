using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Audio variation system. Random pitch/clip selection per SFX group.
    /// </summary>
    public class AudioVariation
    {
        private readonly Dictionary<string, SfxGroup> _groups = new();
        private readonly Random _rng = new();

        /// <summary>Register an SFX group with multiple clip variants and pitch range.</summary>
        public void RegisterGroup(string groupName, List<string> clipIds, float pitchMin = 0.9f, float pitchMax = 1.1f)
        {
            _groups[groupName] = new SfxGroup
            {
                ClipIds = clipIds,
                PitchMin = pitchMin,
                PitchMax = pitchMax,
            };
        }

        /// <summary>Pick a random clip and pitch from a group.</summary>
        public (string clipId, float pitch) Pick(string groupName)
        {
            if (!_groups.TryGetValue(groupName, out var group))
                return (groupName, 1f);
            var clipId = group.ClipIds[_rng.Next(group.ClipIds.Count)];
            var pitch = group.PitchMin + (float)_rng.NextDouble() * (group.PitchMax - group.PitchMin);
            return (clipId, pitch);
        }

        /// <summary>Play a varied SFX. Uses the injected play callback.</summary>
        public void Play(string groupName, Action<string, float> playSfxWithPitch)
        {
            var (clipId, pitch) = Pick(groupName);
            playSfxWithPitch?.Invoke(clipId, pitch);
        }

        private class SfxGroup
        {
            public List<string> ClipIds;
            public float PitchMin;
            public float PitchMax;
        }
    }
}
