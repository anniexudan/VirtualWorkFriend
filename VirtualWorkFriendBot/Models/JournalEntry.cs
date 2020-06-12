using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualWorkFriendBot.Models
{
    public class JournalEntry : JournalEntryAssessment
    {
        public string UserContextId { get; set; }

        public int PromptSourceId { get; set; }
        public int PromptIndexId { get; set; }

        public DateTime EntryDate { get; set; }


    }
}
