using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VirtualWorkFriendBot.Helpers
{
    public class StateHelper
    {
        public static async Task<U> RetrieveFromStateAsync<T,U>(IServiceProvider serviceProvider, ITurnContext context, bool useDbHelper = false) 
            where T : BotState
            where U : class, new()
        {
            U value = default(U);
            var target = typeof(U);
            Func<U> fnDefault = () => 
            {
                System.Diagnostics.Debug.WriteLine(context.Activity.From.Id);
                return new U();
            };

            if (!useDbHelper)
            {
                var sp = serviceProvider.GetService<T>();
                
                var accessor = sp.CreateProperty<U>(target.Name);
                value = await accessor.GetAsync(context, fnDefault);
            }
            else
            {
                bool isUserState = (typeof(T) == typeof(UserState));
                var bsc = new BotStateContext
                {
                    Category = isUserState
                        ? BotStorageCategory.User : BotStorageCategory.Conversation,
                    ContextId = isUserState
                        ? context.Activity.From.Id : context.Activity.Conversation.Id,
                    PropertyName = target.Name
                };
                value = DBHelper.GetStateObject(bsc, fnDefault);
            }
            return value;
        }
        public static async Task PersistInStateAsync<T, U>(IServiceProvider serviceProvider, ITurnContext context, U value, bool useDbHelper = false)
            where T : BotState
            where U : class
        {
            var target = typeof(U);
            if (!useDbHelper)
            { 
                var sp = serviceProvider.GetService<T>();
                var accessor = sp.CreateProperty<U>(target.Name);
                await accessor.SetAsync(context, value);
                await sp.SaveChangesAsync(context, true);
            }
            else
            {
                bool isUserState = (typeof(T) == typeof(UserState));
                var bsc = new BotStateContext
                {
                    Category = isUserState
                        ? BotStorageCategory.User : BotStorageCategory.Conversation,
                    ContextId = isUserState
                        ? context.Activity.From.Id : context.Activity.Conversation.Id,
                    PropertyName = target.Name
                };
                DBHelper.SaveStateObject(bsc, value);
            }

        }
    }
}
