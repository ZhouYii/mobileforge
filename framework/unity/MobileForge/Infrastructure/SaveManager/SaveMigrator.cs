using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Migration chain runner. Registers version-to-version migration functions
    /// and applies them in sequence to upgrade save data from any old version
    /// to the current version.
    /// </summary>
    public class SaveMigrator
    {
        private readonly Dictionary<int, MigrationStep> _steps = new();

        /// <summary>
        /// Register a migration step from one version to the next.
        /// </summary>
        /// <param name="fromVersion">Source version.</param>
        /// <param name="toVersion">Target version (should be fromVersion + 1 for linear chains).</param>
        /// <param name="migrationFunc">Function that transforms the data dictionary.</param>
        public void Register(int fromVersion, int toVersion,
            Func<Dictionary<string, object>, Dictionary<string, object>> migrationFunc)
        {
            if (migrationFunc == null)
                throw new ArgumentNullException(nameof(migrationFunc));

            _steps[fromVersion] = new MigrationStep
            {
                FromVersion = fromVersion,
                ToVersion = toVersion,
                Migrate = migrationFunc,
            };
        }

        /// <summary>
        /// Migrate data from the source version to the target version by applying
        /// the chain of registered migration steps.
        /// </summary>
        /// <param name="data">The save data to migrate.</param>
        /// <param name="fromVersion">Current version of the data.</param>
        /// <param name="toVersion">Desired target version.</param>
        /// <returns>Migrated data dictionary.</returns>
        /// <exception cref="InvalidOperationException">If no migration path exists.</exception>
        public Dictionary<string, object> Migrate(Dictionary<string, object> data, int fromVersion, int toVersion)
        {
            if (fromVersion == toVersion)
                return data;

            if (fromVersion > toVersion)
                throw new InvalidOperationException(
                    $"Cannot downgrade save from version {fromVersion} to {toVersion}.");

            var current = data;
            int currentVersion = fromVersion;

            while (currentVersion < toVersion)
            {
                if (!_steps.TryGetValue(currentVersion, out var step))
                    throw new InvalidOperationException(
                        $"No migration registered from version {currentVersion}. " +
                        $"Cannot reach target version {toVersion}.");

                current = step.Migrate(current);
                currentVersion = step.ToVersion;

                // Safety: prevent infinite loops
                if (currentVersion <= step.FromVersion)
                    throw new InvalidOperationException(
                        $"Migration from {step.FromVersion} to {step.ToVersion} does not advance version.");
            }

            return current;
        }

        /// <summary>
        /// Check if a complete migration path exists from one version to another.
        /// </summary>
        public bool HasPath(int fromVersion, int toVersion)
        {
            if (fromVersion >= toVersion)
                return fromVersion == toVersion;

            int current = fromVersion;
            var visited = new HashSet<int>();

            while (current < toVersion)
            {
                if (!_steps.TryGetValue(current, out var step))
                    return false;

                if (!visited.Add(current))
                    return false; // Cycle detected

                current = step.ToVersion;
            }

            return current == toVersion;
        }

        /// <summary>
        /// Get the number of registered migration steps.
        /// </summary>
        public int StepCount => _steps.Count;

        private class MigrationStep
        {
            public int FromVersion;
            public int ToVersion;
            public Func<Dictionary<string, object>, Dictionary<string, object>> Migrate;
        }
    }
}
