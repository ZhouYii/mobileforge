namespace TowerOfSaviors
{
    /// <summary>
    /// Enemy display state for rendering.
    /// Reads from EnemyState and provides display-ready data.
    /// </summary>
    public class EnemyViewModel
    {
        public string Name { get; set; } = "";
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int Countdown { get; set; }
        public int ElementId { get; set; }
        public bool IsAlive { get; set; } = true;

        /// <summary>
        /// HP ratio for rendering health bars (0.0 to 1.0).
        /// </summary>
        public float HpRatio => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

        /// <summary>
        /// Health bar color tier based on HP ratio.
        /// </summary>
        public HealthTier HealthColor
        {
            get
            {
                if (HpRatio > 0.5f) return HealthTier.Green;
                if (HpRatio > 0.2f) return HealthTier.Yellow;
                return HealthTier.Red;
            }
        }

        public enum HealthTier { Green, Yellow, Red }

        /// <summary>
        /// Update from a domain EnemyState object.
        /// </summary>
        public void UpdateFrom(object enemyState)
        {
            if (enemyState is MobileForge.Domain.EnemyState enemy)
            {
                Name = enemy.Name;
                CurrentHp = enemy.Hp;
                MaxHp = enemy.MaxHp;
                Countdown = enemy.Countdown;
                ElementId = enemy.Element;
                IsAlive = enemy.IsAlive;
            }
        }
    }
}
