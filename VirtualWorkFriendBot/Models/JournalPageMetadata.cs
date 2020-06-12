using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualWorkFriendBot.Helpers;

namespace VirtualWorkFriendBot.Models
{
    public class JournalPageMetadata
    {
        public JournalPageMetadata()
        {
        }
        public JournalPageMetadata(DateTime date)
        {
            Populate(date);
        }
        public void Populate(DateTime date)
        {
            EntryDate = date;
            DayOfWeek = date.DayOfWeek;
            Day = date.Day;
            Week = date.GetWeekOfMonth();
        }
        public DateTime EntryDate { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
    }
}
