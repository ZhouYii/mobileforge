using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Team slot container. Holds monster instance references in ordered slots.
    /// Slot 0 = leader, last slot = friend.
    /// </summary>
    public class Team
    {
        private readonly object[] _slots;

        public int MaxSlots { get; }

        public Team(int maxSlots = 6)
        {
            MaxSlots = maxSlots;
            _slots = new object[maxSlots];
        }

        /// <summary>
        /// Get the object in a slot (null if empty or invalid index).
        /// </summary>
        public object GetSlot(int index)
        {
            if (index < 0 || index >= MaxSlots)
                return null;
            return _slots[index];
        }

        /// <summary>
        /// Set an object into a slot. Returns true if valid index.
        /// </summary>
        public bool SetSlot(int index, object member)
        {
            if (index < 0 || index >= MaxSlots)
                return false;
            _slots[index] = member;
            return true;
        }

        /// <summary>
        /// Get count of non-null slots.
        /// </summary>
        public int GetFilledCount()
        {
            int count = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != null)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Get the leader (slot 0).
        /// </summary>
        public object GetLeader()
        {
            return GetSlot(0);
        }

        /// <summary>
        /// Get the friend (last slot).
        /// </summary>
        public object GetFriend()
        {
            return GetSlot(MaxSlots - 1);
        }

        /// <summary>
        /// Convert to a list of all slot values (including nulls).
        /// </summary>
        public List<object> ToList()
        {
            var list = new List<object>(MaxSlots);
            for (int i = 0; i < MaxSlots; i++)
                list.Add(_slots[i]);
            return list;
        }
    }

    /// <summary>
    /// Calculated team-wide stats (sum of all member stats).
    /// </summary>
    public class TeamStats
    {
        public int TotalHp { get; set; }
        public int TotalAtk { get; set; }
        public int TotalRec { get; set; }
        public int TotalCost { get; set; }
        public int MemberCount { get; set; }

        public TeamStats()
        {
            TotalHp = 0;
            TotalAtk = 0;
            TotalRec = 0;
            TotalCost = 0;
            MemberCount = 0;
        }
    }
}
