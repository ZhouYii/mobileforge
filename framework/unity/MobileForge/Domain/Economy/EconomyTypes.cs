namespace MobileForge.Domain
{
    /// <summary>
    /// Definition for a currency type.
    /// </summary>
    public class CurrencyType
    {
        public string Id { get; }
        public string Name { get; }
        public int MaxAmount { get; } // -1 = unlimited

        public CurrencyType(string id = "", string name = "", int maxAmount = -1)
        {
            Id = id;
            Name = name;
            MaxAmount = maxAmount;
        }
    }

    /// <summary>
    /// Configuration for stamina regeneration.
    /// </summary>
    public class StaminaConfig
    {
        public int MaxStamina { get; }
        public double RefillRateSeconds { get; } // Seconds per 1 stamina point
        public string RefillCostCurrency { get; }
        public int RefillCostAmount { get; }

        public StaminaConfig(int maxStamina = 100, double refillRateSeconds = 300.0,
            string refillCostCurrency = "gems", int refillCostAmount = 1)
        {
            MaxStamina = maxStamina;
            RefillRateSeconds = refillRateSeconds;
            RefillCostCurrency = refillCostCurrency;
            RefillCostAmount = refillCostAmount;
        }
    }
}
