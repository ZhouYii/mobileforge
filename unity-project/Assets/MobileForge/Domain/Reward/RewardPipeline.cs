using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Unifies reward granting from any source (loot, gacha, shop, battle pass, quests).
    /// Rewards flow through: validate -> transform -> grant -> notify.
    /// </summary>
    public class RewardPipeline
    {
        private readonly Action<string, Dictionary<string, object>> _emitEvent;
        private readonly List<Func<Dictionary<string, object>, Dictionary<string, object>>> _transforms = new();

        /// <summary>Injected: grant currency. (currencyId, amount)</summary>
        public Action<string, int> OnGrantCurrency { get; set; }

        /// <summary>Injected: grant monster. (monsterId)</summary>
        public Action<int> OnGrantMonster { get; set; }

        /// <summary>Injected: grant generic item. (itemId, count)</summary>
        public Action<string, int> OnGrantItem { get; set; }

        /// <summary>Injected: grant stamina. (amount)</summary>
        public Action<int> OnGrantStamina { get; set; }

        /// <summary>Injected: grant equipment. (equipmentDefId, level)</summary>
        public Action<string, int> OnGrantEquipment { get; set; }

        /// <summary>Injected: grant player EXP. (amount)</summary>
        public Action<int> OnGrantPlayerExp { get; set; }

        public RewardPipeline(Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _emitEvent = emitEvent;
        }

        /// <summary>Register a transform that modifies rewards before granting.</summary>
        public void AddTransform(Func<Dictionary<string, object>, Dictionary<string, object>> transform)
        {
            _transforms.Add(transform);
        }

        /// <summary>Grant a batch of rewards. Returns list of grant results.</summary>
        public List<Dictionary<string, object>> Grant(
            List<Dictionary<string, object>> rewards, string source = "unknown")
        {
            var results = new List<Dictionary<string, object>>();
            foreach (var rewardData in rewards)
            {
                var reward = new Dictionary<string, object>(rewardData);
                foreach (var xform in _transforms)
                    reward = xform(reward);
                results.Add(GrantSingle(reward, source));
            }
            _emitEvent?.Invoke("rewards_granted", new Dictionary<string, object>
            {
                ["source"] = source, ["count"] = results.Count,
            });
            return results;
        }

        private Dictionary<string, object> GrantSingle(Dictionary<string, object> reward, string source)
        {
            var type = reward.TryGetValue("type", out var t) ? t as string ?? "" : "";
            var count = reward.TryGetValue("count", out var c) ? Convert.ToInt32(c) : 1;
            var result = new Dictionary<string, object>
            {
                ["type"] = type, ["count"] = count, ["success"] = false, ["source"] = source,
            };

            switch (type)
            {
                case "currency":
                    var currencyId = reward.TryGetValue("currency", out var cid)
                        ? cid as string ?? ""
                        : (reward.TryGetValue("id", out var rid) ? rid as string ?? "" : "");
                    OnGrantCurrency?.Invoke(currencyId, count);
                    result["success"] = true;
                    result["currency"] = currencyId;
                    break;
                case "monster":
                    var monsterId = reward.TryGetValue("id", out var mid) ? Convert.ToInt32(mid) : 0;
                    OnGrantMonster?.Invoke(monsterId);
                    result["success"] = true;
                    result["monster_id"] = monsterId;
                    break;
                case "item":
                    var itemId = reward.TryGetValue("id", out var iid) ? iid as string ?? "" : "";
                    OnGrantItem?.Invoke(itemId, count);
                    result["success"] = true;
                    result["item_id"] = itemId;
                    break;
                case "stamina":
                    OnGrantStamina?.Invoke(count);
                    result["success"] = true;
                    break;
                case "equipment":
                    var equipId = reward.TryGetValue("id", out var eid) ? eid as string ?? "" : "";
                    var equipLevel = reward.TryGetValue("level", out var elv) ? Convert.ToInt32(elv) : 1;
                    OnGrantEquipment?.Invoke(equipId, equipLevel);
                    result["success"] = true;
                    result["equipment_id"] = equipId;
                    break;
                case "player_exp":
                    OnGrantPlayerExp?.Invoke(count);
                    result["success"] = true;
                    break;
                default:
                    result["success"] = true;
                    break;
            }
            return result;
        }
    }
}
