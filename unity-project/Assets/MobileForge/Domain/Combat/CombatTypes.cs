using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Hook stages in the 5-hook damage pipeline.
    /// </summary>
    public enum DamageHook
    {
        PreElement = 0,    // Before element multiplier
        PostElement = 1,   // After element multiplier
        Main = 2,          // Main calculation
        PostDefense = 3,   // After defense subtraction
        CanZero = 4,       // Final check, can force damage to 0
    }

    /// <summary>
    /// Mutable context passed through the hook pipeline.
    /// </summary>
    public class DamageContext
    {
        public int AttackerElement;
        public int DefenderElement;
        public float BaseDamage;
        public float Damage;
        public int ComboCount;
        public int ComboIndex;
        public int GemsMatched;
        public float AttackerAtk;
        public float DefenderDefense;
        public bool IsSkill;
        public Dictionary<string, object> Extra = new();

        public DamageContext()
        {
            AttackerElement = 0;
            DefenderElement = 0;
            BaseDamage = 0f;
            Damage = 0f;
            ComboCount = 0;
            ComboIndex = 0;
            GemsMatched = 0;
            AttackerAtk = 0f;
            DefenderDefense = 0f;
            IsSkill = false;
        }
    }

    /// <summary>
    /// Immutable output from damage resolution.
    /// </summary>
    public class DamageResult
    {
        public int FinalDamage { get; }
        public float ElementMultiplier { get; }
        public float ComboMultiplier { get; }
        public List<string> HooksApplied { get; }
        public int Overkill { get; set; }

        public DamageResult(int damage, float elemMult, float comboMult, List<string> hooks = null)
        {
            FinalDamage = damage;
            ElementMultiplier = elemMult;
            ComboMultiplier = comboMult;
            HooksApplied = hooks ?? new List<string>();
            Overkill = 0;
        }
    }
}
