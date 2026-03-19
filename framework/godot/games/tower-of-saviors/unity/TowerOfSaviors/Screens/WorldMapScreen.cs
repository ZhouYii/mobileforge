using System;
using System.Collections.Generic;
using System.Linq;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace TowerOfSaviors
{
    /// <summary>
    /// World Map screen matching original Worldmap_WorldMap_View / Worldmap_Zone_View.
    /// Shows game zones/regions as a hierarchical navigation to dungeon select.
    /// Flow: WorldMap → Zone (DungeonSelect with filter) → Team → Battle → Result
    /// </summary>
    public class WorldMapScreen : IScreen
    {
        private UIRouter _router;
        private GameData _gameData;

        public List<WorldZone> Zones { get; } = new();

        public class WorldZone
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int ElementTheme { get; set; }
            public int StageCount { get; set; }
            public bool IsNew { get; set; }
            public bool IsEvent { get; set; }
            public string Difficulty { get; set; }
        }

        public WorldMapScreen() { }

        public void Setup(GameData gameData, UIRouter router)
        {
            _gameData = gameData;
            _router = router;
            BuildZones();
        }

        private void BuildZones()
        {
            Zones.Clear();

            // Build zones from stage data (group stages by their zone/region prefix)
            var stages = _gameData?.GetAllDefinitions("stages");
            if (stages != null && stages.Count > 0)
            {
                // Group stages by difficulty as proxy for zones
                var zoneMap = new Dictionary<string, int>();
                foreach (var stage in stages)
                {
                    var raw = stage.Raw();
                    string zone = raw.ContainsKey("difficulty") ? raw["difficulty"]?.ToString() ?? "normal" : "normal";
                    if (!zoneMap.ContainsKey(zone))
                        zoneMap[zone] = 0;
                    zoneMap[zone]++;
                }

                foreach (var kvp in zoneMap)
                {
                    Zones.Add(new WorldZone
                    {
                        Id = kvp.Key,
                        Name = FormatZoneName(kvp.Key),
                        Description = $"{kvp.Value} stages available",
                        ElementTheme = GetZoneElement(kvp.Key),
                        StageCount = kvp.Value,
                        Difficulty = kvp.Key
                    });
                }
            }

            // Always add these standard zones if empty
            if (Zones.Count == 0)
            {
                Zones.Add(new WorldZone
                {
                    Id = "normal", Name = "Normal Dungeons",
                    Description = "Standard adventure stages",
                    ElementTheme = 1, StageCount = 5, Difficulty = "normal"
                });
            }

            // Special zones that always appear
            Zones.Add(new WorldZone
            {
                Id = "event", Name = "Event Dungeons",
                Description = "Limited-time special stages",
                ElementTheme = 4, StageCount = 3,
                IsEvent = true, IsNew = true
            });
            Zones.Add(new WorldZone
            {
                Id = "daily", Name = "Daily Dungeons",
                Description = "Rotating element dungeons",
                ElementTheme = 0, StageCount = 6,
                Difficulty = "expert"
            });
            Zones.Add(new WorldZone
            {
                Id = "technical", Name = "Technical Dungeons",
                Description = "Challenge stages with restrictions",
                ElementTheme = 5, StageCount = 4,
                Difficulty = "mythical"
            });
        }

        private static string FormatZoneName(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId)) return "Unknown";
            return char.ToUpper(zoneId[0]) + zoneId.Substring(1).Replace("_", " ") + " Realm";
        }

        private static int GetZoneElement(string zoneId)
        {
            return zoneId switch
            {
                "water" => 1, "fire" => 2, "earth" => 3,
                "light" => 4, "dark" => 5, _ => 0
            };
        }

        public void EnterZone(string zoneId)
        {
            // Navigate to dungeon select with zone filter
            _router?.Push("dungeon_select", new Dictionary<string, object>
            {
                { "zone", zoneId }
            });
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
