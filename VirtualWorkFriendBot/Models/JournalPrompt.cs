namespace VirtualWorkFriendBot.Models
{
    public class JournalPrompt
    {
        public int SourceIndex { get; set; }
        public int PromptIndex { get; set; }
        public string Prompt { get; set; }
        public string Details { get; set; }
        public string Source { get; set; }
    }
}