using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Monster card display data model. Binds to a MonsterInstance and MonsterDef
    /// to expose read-only display properties.
    /// Pure C# — rendering is delegated to engine-specific code.
    /// </summary>
    public class CardView
    {
        private Domain.MonsterInstance _instance;
        private Domain.MonsterDef _def;

        /// <summary>
        /// Monster name from the definition.
        /// </summary>
        public string Name => _def?.Name ?? "";

        /// <summary>
        /// Current level from the instance, or 0 if only def is bound.
        /// </summary>
        public int Level => _instance?.Level ?? 0;

        /// <summary>
        /// Element type from the definition.
        /// </summary>
        public int Element => _def?.Element ?? 0;

        /// <summary>
        /// Rarity from the definition.
        /// </summary>
        public int Rarity => _def?.Rarity ?? 0;

        /// <summary>
        /// Max level from the definition.
        /// </summary>
        public int MaxLevel => _def?.MaxLevel ?? 1;

        /// <summary>
        /// Definition ID.
        /// </summary>
        public int DefId => _def?.Id ?? 0;

        /// <summary>
        /// Instance ID, or -1 if only def is bound.
        /// </summary>
        public int InstanceId => _instance?.InstanceId ?? -1;

        /// <summary>
        /// Whether the monster is favorited.
        /// </summary>
        public bool IsFavorite => _instance?.IsFavorite ?? false;

        /// <summary>
        /// Current experience from the instance.
        /// </summary>
        public int Exp => _instance?.Exp ?? 0;

        /// <summary>
        /// Skill level from the instance.
        /// </summary>
        public int SkillLevel => _instance?.SkillLevel ?? 1;

        /// <summary>
        /// Plus HP bonus from the instance.
        /// </summary>
        public int PlusHp => _instance?.PlusHp ?? 0;

        /// <summary>
        /// Plus ATK bonus from the instance.
        /// </summary>
        public int PlusAtk => _instance?.PlusAtk ?? 0;

        /// <summary>
        /// Plus REC bonus from the instance.
        /// </summary>
        public int PlusRec => _instance?.PlusRec ?? 0;

        /// <summary>
        /// Whether a full instance is bound (vs. def-only).
        /// </summary>
        public bool HasInstance => _instance != null;

        /// <summary>
        /// Whether a definition is bound.
        /// </summary>
        public bool HasDef => _def != null;

        /// <summary>
        /// Fired when the card data changes.
        /// </summary>
        public Action OnChanged;

        /// <summary>
        /// Bind to a full monster instance with its definition.
        /// </summary>
        public void Bind(Domain.MonsterInstance instance, Domain.MonsterDef def)
        {
            _instance = instance;
            _def = def;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Bind to a definition only (no instance). Useful for monster catalog / preview.
        /// </summary>
        public void BindDef(Domain.MonsterDef def)
        {
            _instance = null;
            _def = def;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Clear all bindings.
        /// </summary>
        public void Unbind()
        {
            _instance = null;
            _def = null;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Refresh the card (re-fire OnChanged without changing data).
        /// Useful when the underlying instance has been mutated externally.
        /// </summary>
        public void Refresh()
        {
            OnChanged?.Invoke();
        }
    }
}
