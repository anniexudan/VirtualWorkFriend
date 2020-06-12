namespace VirtualWorkFriendBot.Helpers
{
    public class BotStateContext
    {
        public string ContextId { get; set; } 
        public BotStorageCategory Category { get; set; }
        public string PropertyName { get; set; }
    }
}