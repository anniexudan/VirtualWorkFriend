using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using BS=Microsoft.Bot.Schema;

namespace VirtualWorkFriendBot.Dialogs
{
    public class VirtualFriendDialog : ComponentDialog
    {
        protected IServiceProvider _serviceProvider;
        protected IConfiguration _configuration;

        protected DiscussionState _discussionState;
        protected OnboardingState _onboardingState;
        protected LocaleTemplateEngineManager _templateEngine;

        public VirtualFriendDialog(string id,
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(id)
        {
            _serviceProvider = serviceProvider;
            _configuration = serviceProvider.GetService<IConfiguration>();
            this.TelemetryClient = telemetryClient;
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
        }

        protected virtual async Task PopulateStateObjects(WaterfallStepContext sc)
        {
            _discussionState = await StateHelper.RetrieveFromStateAsync
                <ConversationState, DiscussionState>(_serviceProvider, sc.Context);
            _discussionState.UpdateActivityFromId(sc.Context);
            _onboardingState = await StateHelper.RetrieveFromStateAsync
                <UserState, OnboardingState>(_serviceProvider, sc.Context, true);
        }

        protected async Task SaveOnboardingState(ITurnContext ctx)
        {
            await StateHelper.PersistInStateAsync<UserState, OnboardingState>(
                _serviceProvider, ctx, _onboardingState, true);
        }

        protected async Task<DialogTurnResult> EndDialogAndProcessing(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync(new ProcessingComplete(), cancellationToken);
        }

        public static async Task CreateLinkCard(WaterfallStepContext stepContext, CancellationToken cancellationToken
            , string title, Action<List<BS.CardAction>> fnAddButtons)
        {
            var attachments = new List<BS.Attachment>();
            var reply = MessageFactory.Attachment(attachments);

            var buttons = new List<BS.CardAction>();            

            fnAddButtons(buttons);
            var card = new BS.HeroCard
            {
                Title = title,
                Buttons = buttons
            };
            reply.Attachments.Add(BS.Extensions.ToAttachment(card));
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
        }
        public static BS.CardAction CreateOpenUrlAction(String linkText, string url)
        {
            return new BS.CardAction(BS.ActionTypes.OpenUrl,
                    linkText,
                    value: url);
        }
    }
}
