using Azure.AI.TextAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualWorkFriendBot.Models
{
    public class JournalEntryAssessment
    {
        public string Id { get; set; }
        public DocumentSentiment Sentiment { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
