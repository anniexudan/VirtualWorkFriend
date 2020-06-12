// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using VirtualWorkFriendBot.Services;

namespace VirtualWorkFriendBot.Dialogs
{
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using System;
    using System.Collections.Generic;

    public class ChitchatDialog : ComponentDialog
    {
        private BotServices _services;

        private CancelDialog _cancelDialog;
        private StressHandlingDialog _stressHandlingDialog;

        public ChitchatDialog(
        BotServices botServices,
        IServiceProvider serviceProvider,
        IBotTelemetryClient telemetryClient)
            : base(nameof(ChitchatDialog))
        {
            _services = botServices;

            InitialDialogId = nameof(ChitchatDialog);


            var steps = new WaterfallStep[]
            {
                Initiate,
                ChitchatQNA,
                Feedback,
                FeedbackProcessAsync
            };

            _cancelDialog = serviceProvider.GetService<CancelDialog>();

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(_cancelDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, steps));

        }

        private async Task<DialogTurnResult> Initiate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = MessageFactory.Text("What can I do for you today?"),
                RetryPrompt = MessageFactory.Text("What can I do for you?"),
            }, cancellationToken);
        }
        private async Task<DialogTurnResult> ChitchatQNA(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.BeginDialogAsync("Chitchat");

        }

        private async Task<DialogTurnResult> Feedback(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newFeedback = new List<string> { "Yes, bye", "More Chat" };
            int milliseconds = 1000;
            Thread.Sleep(milliseconds);

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
            {
                Prompt = MessageFactory.Text("Was that helpful?"),
                Choices = ChoiceFactory.ToChoices(newFeedback),
                RetryPrompt = MessageFactory.Text("Let me know if it is helpful")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> FeedbackProcessAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get User Stress Level Choice
            var choice = (FoundChoice)stepContext.Result;
            if (choice.Value == "More Chat")
            {
                return await stepContext.ReplaceDialogAsync(nameof(ChitchatDialog));
            }
            else
            {

                return await stepContext.BeginDialogAsync(_cancelDialog.Id);

            }

        }

    }
}
