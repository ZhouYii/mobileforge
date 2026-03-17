namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Event payload types for PlayerState changes.
    /// </summary>
    public class ValueChangedEvent
    {
        public string Section { get; }
        public string Key { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public ValueChangedEvent(string section, string key, object oldValue, object newValue)
        {
            Section = section;
            Key = key;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class SectionChangedEvent
    {
        public string Section { get; }

        public SectionChangedEvent(string section)
        {
            Section = section;
        }
    }
}
