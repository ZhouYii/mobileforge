namespace TowerOfSaviors
{
    /// <summary>
    /// Damage display data for rendering floating damage numbers.
    /// Created from combat events and consumed by the rendering layer.
    /// </summary>
    public class DamageEvent
    {
        /// <summary>
        /// Damage amount to display.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// Target position in the enemy list (for positioning the label).
        /// </summary>
        public int TargetIndex { get; set; }

        /// <summary>
        /// Element ID for color coding (1=water/blue, 2=fire/red, etc.).
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// Whether this was a critical hit (larger font).
        /// </summary>
        public bool IsCrit { get; set; }

        /// <summary>
        /// Whether this was an element advantage hit (show "EFFECTIVE!").
        /// </summary>
        public bool IsElementAdvantage { get; set; }

        /// <summary>
        /// Combo number that produced this damage (for combo display).
        /// </summary>
        public int ComboNumber { get; set; }

        public DamageEvent(int amount, int targetIndex, int elementId,
            bool isCrit = false, bool isElementAdvantage = false, int comboNumber = 0)
        {
            Amount = amount;
            TargetIndex = targetIndex;
            ElementId = elementId;
            IsCrit = isCrit;
            IsElementAdvantage = isElementAdvantage;
            ComboNumber = comboNumber;
        }
    }
}
