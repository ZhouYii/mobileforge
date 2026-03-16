using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Element advantage/disadvantage multiplier lookups.
    /// Loads data from element_chart.json or can be configured manually.
    /// </summary>
    public class ElementChart
    {
        private readonly Dictionary<int, Dictionary<int, float>> _advantages = new();
        private float _defaultMultiplier = 1.0f;

        public ElementChart()
        {
            SetupDefaultChart();
        }

        /// <summary>
        /// Set up the ToS default element chart.
        /// WATER(1) > FIRE(2), FIRE(2) > GRASS(3), GRASS(3) > WATER(1)
        /// LIGHT(4) and DARK(5) mutual advantage
        /// </summary>
        private void SetupDefaultChart()
        {
            Set(1, 2, 1.5f);  // Water beats Fire
            Set(2, 3, 1.5f);  // Fire beats Grass
            Set(3, 1, 1.5f);  // Grass beats Water
            Set(4, 5, 1.5f);  // Light beats Dark
            Set(5, 4, 1.5f);  // Dark beats Light
            Set(1, 3, 0.5f);  // Water weak to Grass
            Set(2, 1, 0.5f);  // Fire weak to Water
            Set(3, 2, 0.5f);  // Grass weak to Fire
        }

        private void Set(int atk, int def, float mult)
        {
            if (!_advantages.ContainsKey(atk))
                _advantages[atk] = new Dictionary<int, float>();
            _advantages[atk][def] = mult;
        }

        /// <summary>
        /// Return the element multiplier for attacker vs defender.
        /// </summary>
        public float GetMultiplier(int attackerElement, int defenderElement)
        {
            if (_advantages.TryGetValue(attackerElement, out var defMap))
            {
                if (defMap.TryGetValue(defenderElement, out float mult))
                    return mult;
            }
            return _defaultMultiplier;
        }

        /// <summary>
        /// Load chart from parsed JSON data (the element_chart.json format).
        /// Expected keys: "default_multiplier" (float), "advantages" (list), "disadvantages" (list).
        /// Each entry: { "attacker": int, "defender": int, "multiplier": float }.
        /// </summary>
        public void LoadFromData(Dictionary<string, object> data)
        {
            _advantages.Clear();

            if (data.TryGetValue("default_multiplier", out var defMult))
                _defaultMultiplier = System.Convert.ToSingle(defMult);
            else
                _defaultMultiplier = 1.0f;

            LoadEntries(data, "advantages");
            LoadEntries(data, "disadvantages");
        }

        private void LoadEntries(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var entriesObj))
                return;

            if (entriesObj is List<object> entries)
            {
                foreach (var entryObj in entries)
                {
                    if (entryObj is Dictionary<string, object> entry)
                    {
                        int atk = System.Convert.ToInt32(entry["attacker"]);
                        int def = System.Convert.ToInt32(entry["defender"]);
                        float mult = System.Convert.ToSingle(entry["multiplier"]);
                        Set(atk, def, mult);
                    }
                }
            }
        }
    }
}
