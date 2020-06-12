using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VirtualWorkFriendBot.Models
{
    public class VirtualFriendBotState<T> where T : BotState
    {
        public static async Task<U> RetrieveFromStateAsync<U>(IServiceProvider serviceProvider, ITurnContext context)
            where U : new()
        {
            BotState sp = (BotState)serviceProvider.GetService<T>();
            var accessor = sp.CreateProperty<U>(nameof(U));
            return await accessor.GetAsync(context, () => new U());
        }
        public static async Task PersistInStateAsync<U>(IServiceProvider serviceProvider, ITurnContext context, U value)
        {
            BotState sp = (BotState)serviceProvider.GetService<T>();
            var accessor = sp.CreateProperty<U>(nameof(U));
            await accessor.SetAsync(context, value);
        }

    }
}
