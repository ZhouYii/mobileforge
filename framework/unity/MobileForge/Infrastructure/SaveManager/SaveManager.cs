using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Save/load orchestrator. Coordinates ISaveable objects, applies migrations,
    /// and delegates actual I/O to an injected storage backend.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class SaveManager
    {
        private readonly Dictionary<string, ISaveable> _saveables = new();
        private readonly SaveMigrator _migrator = new();
        private bool _isDirty;

        /// <summary>
        /// Current save format version. Increment when save data schema changes.
        /// </summary>
        public int CurrentVersion { get; set; } = 1;

        /// <summary>
        /// Injected storage backend. Must be set before Save/Load calls.
        /// Write: (slotName, serializedData) -> void
        /// Read: (slotName) -> serializedData or null
        /// </summary>
        public Action<string, Dictionary<string, object>> WriteStorage { get; set; }
        public Func<string, Dictionary<string, object>> ReadStorage { get; set; }

        /// <summary>
        /// Whether any saveable has been marked dirty since the last save.
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Register a saveable object under a key.
        /// </summary>
        public void RegisterSaveable(string key, ISaveable saveable)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (saveable == null)
                throw new ArgumentNullException(nameof(saveable));

            _saveables[key] = saveable;
        }

        /// <summary>
        /// Unregister a saveable object.
        /// </summary>
        public void UnregisterSaveable(string key)
        {
            _saveables.Remove(key);
        }

        /// <summary>
        /// Register a migration step on the internal migrator.
        /// </summary>
        public void RegisterMigration(int fromVersion, int toVersion,
            Func<Dictionary<string, object>, Dictionary<string, object>> migrationFunc)
        {
            _migrator.Register(fromVersion, toVersion, migrationFunc);
        }

        /// <summary>
        /// Mark the save state as dirty (needs saving).
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Save all registered saveables to the given slot.
        /// </summary>
        /// <param name="slot">Slot name (e.g., "slot_1", "autosave").</param>
        /// <returns>True if save succeeded.</returns>
        public bool Save(string slot)
        {
            if (WriteStorage == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveManager: No WriteStorage backend configured.");
                return false;
            }

            var data = new Dictionary<string, object>();
            foreach (var kvp in _saveables)
            {
                data[kvp.Key] = kvp.Value.SaveToDict();
            }

            var envelope = SaveFormat.CreateEnvelope(CurrentVersion, data);

            try
            {
                WriteStorage.Invoke(slot, envelope);
                _isDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveManager: Save failed — {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load from the given slot into all registered saveables.
        /// Applies migrations if the save version is older than CurrentVersion.
        /// </summary>
        /// <param name="slot">Slot name.</param>
        /// <returns>True if load succeeded.</returns>
        public bool Load(string slot)
        {
            if (ReadStorage == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveManager: No ReadStorage backend configured.");
                return false;
            }

            Dictionary<string, object> envelope;
            try
            {
                envelope = ReadStorage.Invoke(slot);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveManager: Read failed — {ex.Message}");
                return false;
            }

            if (envelope == null)
                return false;

            // Validate integrity
            if (!SaveFormat.ValidateEnvelope(envelope))
            {
                System.Diagnostics.Debug.WriteLine("SaveManager: Envelope validation failed.");
                return false;
            }

            // Migrate if needed
            int savedVersion = SaveFormat.GetVersion(envelope);
            var data = SaveFormat.GetData(envelope);

            if (data == null)
                return false;

            if (savedVersion < CurrentVersion)
            {
                try
                {
                    data = _migrator.Migrate(data, savedVersion, CurrentVersion);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveManager: Migration failed — {ex.Message}");
                    return false;
                }
            }

            // Distribute data to saveables
            foreach (var kvp in _saveables)
            {
                if (data.TryGetValue(kvp.Key, out var sectionObj) &&
                    sectionObj is Dictionary<string, object> sectionData)
                {
                    kvp.Value.LoadFromDict(sectionData);
                }
            }

            _isDirty = false;
            return true;
        }

        /// <summary>
        /// Check if a save exists in the given slot.
        /// </summary>
        public bool HasSave(string slot)
        {
            if (ReadStorage == null)
                return false;

            try
            {
                var envelope = ReadStorage.Invoke(slot);
                return envelope != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the number of registered saveables.
        /// </summary>
        public int SaveableCount => _saveables.Count;
    }
}
