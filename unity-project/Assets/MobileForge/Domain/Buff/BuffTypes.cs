using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>Buff category constants.</summary>
    public static class BuffCategory
    {
        public const string Offensive = "offensive";
        public const string Defensive = "defensive";
        public const string Utility = "utility";
        public const string Debuff = "debuff";
    }

    /// <summary>Stack rule constants controlling how duplicate buffs interact.</summary>
    public static class BuffStackRule
    {
        public const string Replace = "replace";
        public const string Extend = "extend";
        public const string Stack = "stack";
        public const string Highest = "highest";
        public const string Refresh = "refresh";
    }

    /// <summary>
    /// Buff definition — immutable blueprint describing a buff or debuff.
    /// </summary>
    public class BuffDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } = BuffCategory.Offensive;
        public string StackRule { get; set; } = BuffStackRule.Replace;
        public int MaxStacks { get; set; } = 1;
        public int BaseDuration { get; set; }
        public bool Dispellable { get; set; } = true;
        public bool IsDebuff { get; set; }
        public Dictionary<string, float> StatModifiers { get; set; } = new();
        public Dictionary<string, float> FlatModifiers { get; set; } = new();
        public Dictionary<string, object> Params { get; set; } = new();
    }

    /// <summary>
    /// Mutable runtime buff instance attached to an entity.
    /// </summary>
    public class BuffInstance
    {
        public string DefId { get; set; }
        public string SourceId { get; set; }
        public int RemainingDuration { get; set; }
        public int Stacks { get; set; }
        public float Value { get; set; }

        public bool IsExpired => RemainingDuration <= 0;
    }
}
