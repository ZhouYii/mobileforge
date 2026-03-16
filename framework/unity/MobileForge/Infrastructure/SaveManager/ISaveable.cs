using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Interface for objects that can serialize/deserialize their state
    /// to a dictionary for persistence.
    /// </summary>
    public interface ISaveable
    {
        Dictionary<string, object> SaveToDict();
        void LoadFromDict(Dictionary<string, object> data);
    }
}
