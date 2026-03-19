using System;
using System.Collections.Generic;
using System.Linq;
using MobileForge.Presentation;
using MobileForge.Infrastructure;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Team management screen matching original Team_Tab_Team_View.
    /// Supports multiple saved team presets, team naming, leader selection.
    /// </summary>
    public class TeamManageScreen : IScreen
    {
        private UIRouter _router;
        private MonsterManager _monsterManager;
        private PlayerState _playerState;

        public List<TeamPreset> Teams { get; } = new();
        public int ActiveTeamIndex { get; private set; }
        public TeamPreset ActiveTeam => ActiveTeamIndex >= 0 && ActiveTeamIndex < Teams.Count ? Teams[ActiveTeamIndex] : null;
        public List<MonsterBoxEntry> AvailableMonsters { get; } = new();
        public string StatusMessage { get; private set; } = "";

        /// <summary>Selected slot index in current team for editing (-1 = none).</summary>
        public int SelectedSlot { get; set; } = -1;

        public class TeamPreset
        {
            public int Index { get; set; }
            public string Name { get; set; }
            public int[] MonsterIds { get; set; } = new int[5];
            public int LeaderSlot { get; set; }
        }

        public TeamManageScreen() { }

        public void Setup(MonsterManager monsterManager, PlayerState playerState, UIRouter router)
        {
            _monsterManager = monsterManager;
            _playerState = playerState;
            _router = router;

            // Initialize 5 team presets
            for (int i = 0; i < 5; i++)
            {
                Teams.Add(new TeamPreset
                {
                    Index = i,
                    Name = $"Team {i + 1}",
                    MonsterIds = new int[5] { -1, -1, -1, -1, -1 },
                    LeaderSlot = 0
                });
            }

            // Load saved teams from player state
            LoadTeams();

            // Build available monster list
            RefreshMonsterList();
            ActiveTeamIndex = 0;
        }

        private void LoadTeams()
        {
            for (int t = 0; t < Teams.Count; t++)
            {
                var teamData = _playerState?.GetValue("teams", $"team_{t}", null);
                if (teamData is Dictionary<string, object> dict)
                {
                    if (dict.TryGetValue("name", out var name))
                        Teams[t].Name = name?.ToString() ?? Teams[t].Name;
                    if (dict.TryGetValue("leader", out var leader))
                        Teams[t].LeaderSlot = Convert.ToInt32(leader);
                    for (int s = 0; s < 5; s++)
                    {
                        if (dict.TryGetValue($"slot_{s}", out var mid))
                            Teams[t].MonsterIds[s] = Convert.ToInt32(mid);
                    }
                }
            }
        }

        private void SaveTeam(int teamIndex)
        {
            var team = Teams[teamIndex];
            var dict = new Dictionary<string, object>
            {
                { "name", team.Name },
                { "leader", team.LeaderSlot }
            };
            for (int s = 0; s < 5; s++)
                dict[$"slot_{s}"] = team.MonsterIds[s];
            _playerState?.SetValue("teams", $"team_{teamIndex}", dict);
        }

        public void RefreshMonsterList()
        {
            AvailableMonsters.Clear();
            // Create sample monsters (mirroring MonsterBoxScreen's approach)
            // In production, this would read from PlayerState inventory
            int[] sampleDefIds = { 101, 201, 301, 401, 501, 102, 202, 302 };
            for (int i = 0; i < sampleDefIds.Length; i++)
            {
                var inst = _monsterManager?.CreateInstance(sampleDefIds[i], level: 10 + i * 5);
                var def = _monsterManager?.GetDef(sampleDefIds[i]);
                if (inst != null)
                {
                    var stats = _monsterManager?.GetStats(inst);
                    AvailableMonsters.Add(new MonsterBoxEntry
                    {
                        Instance = inst,
                        Def = def,
                        Stats = stats
                    });
                }
            }
        }

        public void SelectTeam(int index)
        {
            if (index >= 0 && index < Teams.Count)
            {
                ActiveTeamIndex = index;
                SelectedSlot = -1;
                StatusMessage = $"Viewing {Teams[index].Name}";
            }
        }

        public void AssignMonster(int slotIndex, int monsterId)
        {
            if (ActiveTeam == null || slotIndex < 0 || slotIndex >= 5) return;

            // Remove monster from other slots in same team
            for (int i = 0; i < 5; i++)
                if (ActiveTeam.MonsterIds[i] == monsterId)
                    ActiveTeam.MonsterIds[i] = -1;

            ActiveTeam.MonsterIds[slotIndex] = monsterId;
            SaveTeam(ActiveTeamIndex);
            StatusMessage = "Monster assigned";
        }

        public void ClearSlot(int slotIndex)
        {
            if (ActiveTeam == null || slotIndex < 0 || slotIndex >= 5) return;
            ActiveTeam.MonsterIds[slotIndex] = -1;
            SaveTeam(ActiveTeamIndex);
        }

        public void SetLeader(int slotIndex)
        {
            if (ActiveTeam == null || slotIndex < 0 || slotIndex >= 5) return;
            ActiveTeam.LeaderSlot = slotIndex;
            SaveTeam(ActiveTeamIndex);
            StatusMessage = $"Leader set to slot {slotIndex + 1}";
        }

        public void RenameTeam(string newName)
        {
            if (ActiveTeam == null || string.IsNullOrWhiteSpace(newName)) return;
            ActiveTeam.Name = newName;
            SaveTeam(ActiveTeamIndex);
        }

        public MonsterBoxEntry GetMonsterForSlot(int slotIndex)
        {
            if (ActiveTeam == null || slotIndex < 0 || slotIndex >= 5) return null;
            int mid = ActiveTeam.MonsterIds[slotIndex];
            if (mid < 0) return null;
            return AvailableMonsters.FirstOrDefault(m => m.Instance.InstanceId == mid);
        }

        public void OnEnter(Dictionary<string, object> parameters) { }
        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        public void GoBack()
        {
            _router?.Pop();
        }
    }
}
