using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Cloud save with conflict resolution.
    /// Three strategies: TakeMax, UserChoice, LastModifiedWins.
    /// </summary>
    public class CloudSave
    {
        public enum ConflictStrategy { TakeMax, UserChoice, LastModifiedWins }

        private ConflictStrategy _strategy = ConflictStrategy.LastModifiedWins;

        /// <summary>Injected: upload save data. (data) -> success</summary>
        public Func<Dictionary<string, object>, Task<bool>> OnUpload { get; set; }

        /// <summary>Injected: download save data. () -> data or null</summary>
        public Func<Task<Dictionary<string, object>>> OnDownload { get; set; }

        /// <summary>Fired when conflict requires user choice. (localData, remoteData)</summary>
        public event Action<Dictionary<string, object>, Dictionary<string, object>> ConflictDetected;

        /// <summary>Fired when sync completes. (success)</summary>
        public event Action<bool> SyncCompleted;

        public void SetStrategy(ConflictStrategy strategy) => _strategy = strategy;

        /// <summary>Upload local save to cloud.</summary>
        public async Task<bool> Upload(Dictionary<string, object> data)
        {
            if (OnUpload == null) return false;
            var success = await OnUpload(data);
            SyncCompleted?.Invoke(success);
            return success;
        }

        /// <summary>Download remote save and resolve conflicts.</summary>
        public async Task<Dictionary<string, object>> Download(Dictionary<string, object> localData = null)
        {
            if (OnDownload == null) { SyncCompleted?.Invoke(false); return null; }
            var remoteData = await OnDownload();
            if (remoteData == null) { SyncCompleted?.Invoke(false); return null; }

            switch (_strategy)
            {
                case ConflictStrategy.LastModifiedWins:
                    var localTs = localData?.TryGetValue("timestamp", out var lt) == true ? Convert.ToInt64(lt) : 0;
                    var remoteTs = remoteData.TryGetValue("timestamp", out var rt) ? Convert.ToInt64(rt) : 0;
                    SyncCompleted?.Invoke(true);
                    return remoteTs > localTs ? remoteData : localData;
                case ConflictStrategy.UserChoice:
                    ConflictDetected?.Invoke(localData, remoteData);
                    return null; // Caller must wait for user choice
                case ConflictStrategy.TakeMax:
                    SyncCompleted?.Invoke(true);
                    return MergeMax(localData, remoteData);
                default:
                    SyncCompleted?.Invoke(true);
                    return remoteData;
            }
        }

        private static Dictionary<string, object> MergeMax(Dictionary<string, object> local, Dictionary<string, object> remote)
        {
            if (local == null) return remote;
            if (remote == null) return local;
            var merged = new Dictionary<string, object>(local);
            foreach (var kvp in remote)
            {
                if (!merged.ContainsKey(kvp.Key))
                    merged[kvp.Key] = kvp.Value;
                else if (kvp.Value is IComparable c && merged[kvp.Key] is IComparable lc && c.CompareTo(lc) > 0)
                    merged[kvp.Key] = kvp.Value;
            }
            return merged;
        }
    }
}
