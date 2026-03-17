using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Checkpoint critical data before risky ops (gacha, IAP).
    /// Check on next boot to detect incomplete transactions and restore.
    /// Pure C# — delegates file I/O to engine layer.
    /// </summary>
    public class CrashRecovery
    {
        /// <summary>Injected: write checkpoint. (key, data) -> success</summary>
        public Func<string, Dictionary<string, object>, bool> WriteCheckpoint { get; set; }

        /// <summary>Injected: read checkpoint. (key) -> data or null</summary>
        public Func<string, Dictionary<string, object>> ReadCheckpoint { get; set; }

        /// <summary>Injected: delete checkpoint. (key)</summary>
        public Action<string> DeleteCheckpoint { get; set; }

        private const string CheckpointKey = "crash_checkpoint";

        /// <summary>Create a checkpoint before a risky operation.</summary>
        public bool CreateCheckpoint(string operation, Dictionary<string, object> data)
        {
            var checkpoint = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["data"] = data,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["completed"] = false,
            };
            return WriteCheckpoint?.Invoke(CheckpointKey, checkpoint) ?? false;
        }

        /// <summary>Mark checkpoint as completed (operation succeeded).</summary>
        public void CompleteCheckpoint() => DeleteCheckpoint?.Invoke(CheckpointKey);

        /// <summary>Check on boot for incomplete checkpoint. Returns data or null.</summary>
        public Dictionary<string, object> CheckOnBoot()
        {
            var checkpoint = ReadCheckpoint?.Invoke(CheckpointKey);
            if (checkpoint == null) return null;
            if (checkpoint.TryGetValue("completed", out var c) && c is bool completed && completed)
            {
                DeleteCheckpoint?.Invoke(CheckpointKey);
                return null;
            }
            return checkpoint;
        }
    }
}
