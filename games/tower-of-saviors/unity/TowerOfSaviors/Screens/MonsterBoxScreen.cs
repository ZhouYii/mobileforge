using System.Collections.Generic;
using System.Linq;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Monster collection/inventory screen with management actions.
    /// Supports fusion, evolution, +stat, skill up, favorite, awaken.
    /// Mirrors monster_box_screen.gd.
    /// </summary>
    public class MonsterBoxScreen : IScreen
    {
        private MonsterManager _monsterManager;
        private UIRouter _router;

        public enum ActionMode { None, Fuse, PlusFuse, SkillUp, Inherit }

        public List<MonsterBoxEntry> Monsters { get; } = new();
        public MonsterBoxEntry SelectedMonster { get; private set; }
        public ActionMode CurrentMode { get; private set; } = ActionMode.None;
        public string StatusMessage { get; private set; } = "";

        public void Setup(MonsterManager monsterManager, UIRouter router)
        {
            _monsterManager = monsterManager;
            _router = router;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            Monsters.Clear();
            SelectedMonster = null;
            CurrentMode = ActionMode.None;
            CreateSampleMonsters();
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        public void SelectMonster(int index)
        {
            if (CurrentMode != ActionMode.None)
            {
                // In action mode, this selects a fodder
                SelectFodder(index);
                return;
            }
            if (index >= 0 && index < Monsters.Count)
                SelectedMonster = Monsters[index];
        }

        public void GoBack()
        {
            if (CurrentMode != ActionMode.None)
            {
                CurrentMode = ActionMode.None;
                StatusMessage = "";
                return;
            }
            _router.Pop();
        }

        // ── Fusion (Level Up) ──

        public void StartFusion()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            CurrentMode = ActionMode.Fuse;
            StatusMessage = $"Select fodder to fuse into {SelectedMonster.DisplayName}";
        }

        private void SelectFodder(int index)
        {
            if (index < 0 || index >= Monsters.Count) return;
            var fodder = Monsters[index];
            if (fodder == SelectedMonster) { StatusMessage = "Can't fuse a monster into itself"; return; }
            if (fodder.Instance.IsFavorite) { StatusMessage = "Can't use favorited monster as fodder"; return; }

            switch (CurrentMode)
            {
                case ActionMode.Fuse:
                    ExecuteFusion(fodder);
                    break;
                case ActionMode.PlusFuse:
                    ExecutePlusFuse(fodder);
                    break;
                case ActionMode.SkillUp:
                    ExecuteSkillUp(fodder);
                    break;
                case ActionMode.Inherit:
                    ExecuteInherit(fodder);
                    break;
            }
        }

        private void ExecuteFusion(MonsterBoxEntry fodder)
        {
            int exp = _monsterManager.Fuse(SelectedMonster.Instance, fodder.Instance);
            SelectedMonster.Stats = _monsterManager.GetStats(SelectedMonster.Instance);
            Monsters.Remove(fodder);
            CurrentMode = ActionMode.None;
            StatusMessage = $"+{exp} EXP! {SelectedMonster.DisplayName} is now Lv.{SelectedMonster.Instance.Level}";
        }

        // ── Evolution ──

        public bool CanEvolve()
        {
            if (SelectedMonster == null) return false;
            return _monsterManager.CanEvolve(SelectedMonster.Instance);
        }

        public string GetEvolveInfo()
        {
            if (SelectedMonster?.Def == null) return "No monster selected";
            var def = SelectedMonster.Def;
            if (def.EvolveTo < 0) return "This monster cannot evolve";
            if (SelectedMonster.Instance.Level < def.MaxLevel)
                return $"Needs max level ({def.MaxLevel}). Currently Lv.{SelectedMonster.Instance.Level}";
            var targetDef = _monsterManager.GetDef(def.EvolveTo);
            return $"Evolve to: {targetDef?.Name ?? $"#{def.EvolveTo}"}";
        }

        public void ExecuteEvolution()
        {
            if (!CanEvolve()) { StatusMessage = GetEvolveInfo(); return; }
            int oldDefId = SelectedMonster.Instance.DefId;
            int newDefId = _monsterManager.Evolve(SelectedMonster.Instance);
            if (newDefId >= 0)
            {
                SelectedMonster.Def = _monsterManager.GetDef(newDefId);
                SelectedMonster.Stats = _monsterManager.GetStats(SelectedMonster.Instance);
                StatusMessage = $"Evolution complete! {SelectedMonster.DisplayName} Lv.1";
            }
            else
            {
                StatusMessage = "Evolution failed";
            }
        }

        // ── Plus Stats ──

        public void StartPlusFuse()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            CurrentMode = ActionMode.PlusFuse;
            StatusMessage = $"Select fodder to transfer +stats to {SelectedMonster.DisplayName}";
        }

        private void ExecutePlusFuse(MonsterBoxEntry fodder)
        {
            // Transfer plus-stats from fodder + add bonus (+1 per stat)
            SelectedMonster.Instance.PlusHp += fodder.Instance.PlusHp + 1;
            SelectedMonster.Instance.PlusAtk += fodder.Instance.PlusAtk + 1;
            SelectedMonster.Instance.PlusRec += fodder.Instance.PlusRec + 1;

            // Clamp to +99 each
            if (SelectedMonster.Instance.PlusHp > 99) SelectedMonster.Instance.PlusHp = 99;
            if (SelectedMonster.Instance.PlusAtk > 99) SelectedMonster.Instance.PlusAtk = 99;
            if (SelectedMonster.Instance.PlusRec > 99) SelectedMonster.Instance.PlusRec = 99;

            SelectedMonster.Stats = _monsterManager.GetStats(SelectedMonster.Instance);
            Monsters.Remove(fodder);
            CurrentMode = ActionMode.None;

            int total = SelectedMonster.Instance.PlusHp + SelectedMonster.Instance.PlusAtk + SelectedMonster.Instance.PlusRec;
            StatusMessage = $"+Stats updated! Total: +{total}";
        }

        // ── Skill Up ──

        public void StartSkillUp()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            if (SelectedMonster.Instance.SkillLevel >= 10) { StatusMessage = "Skill already at max level"; return; }
            CurrentMode = ActionMode.SkillUp;
            StatusMessage = $"Select fodder for skill up (same skill = guaranteed)";
        }

        private void ExecuteSkillUp(MonsterBoxEntry fodder)
        {
            // Same active_skill_id = guaranteed, same element = 20% chance, else 10%
            bool success;
            if (fodder.Def?.ActiveSkillId == SelectedMonster.Def?.ActiveSkillId && fodder.Def?.ActiveSkillId >= 0)
            {
                success = true;
            }
            else if (fodder.Def?.Element == SelectedMonster.Def?.Element)
            {
                success = UnityEngine.Random.value < 0.2f;
            }
            else
            {
                success = UnityEngine.Random.value < 0.1f;
            }

            Monsters.Remove(fodder);
            CurrentMode = ActionMode.None;

            if (success && SelectedMonster.Instance.SkillLevel < 10)
            {
                SelectedMonster.Instance.SkillLevel++;
                StatusMessage = $"Skill Up! Skill Lv.{SelectedMonster.Instance.SkillLevel}";
            }
            else if (success)
            {
                StatusMessage = "Skill already at max level";
            }
            else
            {
                StatusMessage = "Skill Up failed...";
            }
        }

        // ── Favorite ──

        public void ToggleFavorite()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            SelectedMonster.Instance.IsFavorite = !SelectedMonster.Instance.IsFavorite;
            StatusMessage = SelectedMonster.Instance.IsFavorite
                ? $"\u2605 {SelectedMonster.DisplayName} is now favorited"
                : $"\u2606 {SelectedMonster.DisplayName} unfavorited";
        }

        // ── Awaken ──

        public void Awaken()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            var awk = SelectedMonster.Instance.Awakenings;
            int nextSlot = awk.IndexOf(false);
            if (nextSlot < 0) { StatusMessage = "All awakenings already unlocked!"; return; }

            // Cost: 5000 coins per awakening
            // (Simplified: just unlock without currency check for now)
            awk[nextSlot] = true;
            int unlocked = awk.Count(a => a);
            StatusMessage = $"Awakening slot {nextSlot + 1} unlocked! ({unlocked}/{awk.Count})";
        }

        // ── Limit Break ──

        public void LimitBreak()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            var def = SelectedMonster.Def;
            if (def == null) { StatusMessage = "No definition found"; return; }

            // Require max level and all awakenings
            if (SelectedMonster.Instance.Level < def.MaxLevel)
            {
                StatusMessage = $"Needs max level ({def.MaxLevel}). Currently Lv.{SelectedMonster.Instance.Level}";
                return;
            }
            if (SelectedMonster.Instance.Awakenings.Any(a => !a))
            {
                StatusMessage = "Need all awakenings unlocked first";
                return;
            }
            if (SelectedMonster.Instance.LimitBreakLevel >= 2)
            {
                StatusMessage = "Max limit break reached (Lv.119)";
                return;
            }

            SelectedMonster.Instance.LimitBreakLevel++;
            int newMaxLevel = def.MaxLevel + SelectedMonster.Instance.LimitBreakLevel * 10;
            StatusMessage = $"Limit Break! Max level raised to Lv.{newMaxLevel}";
        }

        // ── Skill Inherit ──

        public void StartInherit()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            if (SelectedMonster.Instance.InheritedSkillId >= 0)
            {
                StatusMessage = "Already has an inherited skill. Remove it first.";
                return;
            }
            CurrentMode = ActionMode.Inherit;
            StatusMessage = $"Select donor monster to inherit skill from";
        }

        private void ExecuteInherit(MonsterBoxEntry donor)
        {
            if (donor.Def?.ActiveSkillId < 0)
            {
                StatusMessage = "Donor has no active skill";
                CurrentMode = ActionMode.None;
                return;
            }

            SelectedMonster.Instance.InheritedSkillId = donor.Def.ActiveSkillId;
            Monsters.Remove(donor); // Donor is consumed
            CurrentMode = ActionMode.None;
            StatusMessage = $"Skill inherited! Gained skill #{SelectedMonster.Instance.InheritedSkillId}";
        }

        public void RemoveInheritedSkill()
        {
            if (SelectedMonster == null) { StatusMessage = "Select a monster first"; return; }
            if (SelectedMonster.Instance.InheritedSkillId < 0)
            {
                StatusMessage = "No inherited skill to remove";
                return;
            }
            SelectedMonster.Instance.InheritedSkillId = -1;
            StatusMessage = "Inherited skill removed";
        }

        // ── Rebuild ──

        public void RefreshMonsterList()
        {
            foreach (var m in Monsters)
            {
                m.Stats = _monsterManager.GetStats(m.Instance);
            }
        }

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

        public string DisplayName => Def?.Name ?? $"#{Instance.DefId}";
        public string ElementName => Def != null ? ElementToString(Def.Element) : "None";

        private static string ElementToString(int element) => element switch
        {
            1 => "Water", 2 => "Fire", 3 => "Grass",
            4 => "Light", 5 => "Dark", 6 => "Heart", _ => "None"
        };
    }
}
