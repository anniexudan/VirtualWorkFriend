using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualWorkFriendBot.Models
{
    public class JournalInformation
    {
        public bool IsJournaling { get; set; } = false;

        public int CurrentStreak { get; set; } = 0;
        public DateTime LastStreakDate { get; set; } = DateTime.MinValue;
        public int LongestStreak { get; set; } = 0;

        public string NotebookId { get; set; }
        public DateTime LastEntry { get; set; } = DateTime.MinValue;
    }
}
