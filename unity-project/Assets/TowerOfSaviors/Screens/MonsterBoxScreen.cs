using System.Collections.Generic;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Monster collection/inventory screen. Shows all owned monsters with detail view.
    /// Mirrors monster_box_screen.gd.
    /// </summary>
    public class MonsterBoxScreen : IScreen
    {
        private MonsterManager _monsterManager;
        private UIRouter _router;

        /// <summary>
        /// All monsters in the box, populated on OnEnter.
        /// </summary>
        public List<MonsterBoxEntry> Monsters { get; } = new();

        /// <summary>
        /// Currently selected monster for detail display, or null.
        /// </summary>
        public MonsterBoxEntry SelectedMonster { get; private set; }

        public void Setup(MonsterManager monsterManager, UIRouter router)
        {
            _monsterManager = monsterManager;
            _router = router;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            Monsters.Clear();
            SelectedMonster = null;
            CreateSampleMonsters();
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Select a monster by index to show its detail.
        /// </summary>
        public void SelectMonster(int index)
        {
            if (index >= 0 && index < Monsters.Count)
                SelectedMonster = Monsters[index];
        }

        /// <summary>
        /// Navigate back to the previous screen.
        /// </summary>
        public void GoBack() => _router.Pop();

        /// <summary>
        /// Populate the box with sample monsters (ids 1-10) at various levels,
        /// mirroring the Godot version's _create_sample_monsters().
        /// </summary>
        private void CreateSampleMonsters()
        {
            for (int id = 1; id <= 10; id++)
            {
                int level = (id * 10) % 99 + 1;
                var instance = _monsterManager.CreateInstance(id, level);
                var def = _monsterManager.GetDef(id);
                var stats = _monsterManager.GetStats(instance);
                Monsters.Add(new MonsterBoxEntry
                {
                    Instance = instance,
                    Def = def,
                    Stats = stats
                });
            }
        }
    }

    /// <summary>
    /// Display model for a single monster in the box.
    /// </summary>
    public class MonsterBoxEntry
    {
        public MonsterInstance Instance;
        public MonsterDef Def;
        public MonsterStats Stats;

        /// <summary>
        /// Display name — falls back to "#DefId" when the definition is missing.
        /// </summary>
        public string DisplayName => Def?.Name ?? $"#{Instance.DefId}";

        /// <summary>
        /// Element as a human-readable string.
        /// </summary>
        public string ElementName => Def != null ? ElementToString(Def.Element) : "None";

        private static string ElementToString(int element)
        {
            return element switch
            {
                1 => "Water",
                2 => "Fire",
                3 => "Grass",
                4 => "Light",
                5 => "Dark",
                6 => "Heart",
                _ => "None"
            };
        }
    }
}
