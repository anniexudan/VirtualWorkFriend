using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VirtualWorkFriendBot.Helpers;
using Microsoft.Bot.Schema;

namespace VirtualWorkFriendBot.Models
{
    public class DiscussionState : VirtualFriendBotState<ConversationState>
    {
        public string SignedInUserId { get; set; }

        public TokenResponse UserToken { get; set; }

        public bool Welcomed { get; set; }

        public void UpdateActivityFromId(ITurnContext context)
        {
            if (!String.IsNullOrEmpty(this.SignedInUserId))
            {
                context.Activity.From.Id = this.SignedInUserId;
            }
        }
    }
}
