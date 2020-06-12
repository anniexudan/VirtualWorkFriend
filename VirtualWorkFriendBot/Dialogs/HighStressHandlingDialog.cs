// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA.Dialogs;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Dialogs;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Extensions.DependencyInjection;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Models;
using VirtualWorkFriendBot.Services;


namespace VirtualWorkFriendBot.Dialogs
{
    using Helpers;
    using Luis;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Graph;
    using System.Collections.Generic;
   
    public class HighStressHandlingDialog : ComponentDialog
    {


        private BotServices _services;
        private EscalateDialog _escalateDialog;
        private StressHandlingDialog _stressHandlingDialog;
        private EntertainDialog _entertainDialog;
        private BreatherDialog _breatherDialog;

        public HighStressHandlingDialog(BotServices botServices,  IBotTelemetryClient telemetryClient, IServiceProvider serviceProvider)
            : base(nameof(HighStressHandlingDialog))
        {
            _services = botServices;
            InitialDialogId = nameof(HighStressHandlingDialog);

            var steps = new WaterfallStep[]
            {
                RespondChoice,
                ProposeTips,
                Complete
            };

            _escalateDialog = serviceProvider.GetService<EscalateDialog>();
            _entertainDialog = serviceProvider.GetService<EntertainDialog>();
            _stressHandlingDialog = serviceProvider.GetService<StressHandlingDialog>();
            _breatherDialog = serviceProvider.GetService<BreatherDialog>();
            AddDialog(_entertainDialog);
            AddDialog(_escalateDialog);
            AddDialog(_stressHandlingDialog);
            AddDialog(_breatherDialog);

            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new TextPrompt(DialogIds.TipsPrompt));
        }



        private async Task<DialogTurnResult> RespondChoice(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var newuserresponseList = new List<string> { "Breather", "Talk to me" };
            return await sc.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
            {
                Prompt = MessageFactory.Text(
                   "Oh I am sorry to hear that. \U0001F61F Do you want to take a moment to have a breather. Or do you want to just start talk to me about the things bothers you?"),
                   Choices = ChoiceFactory.ToChoices(newuserresponseList),
                RetryPrompt = MessageFactory.Text("Would you like breather or chat?")
            }, cancellationToken);
        }





private async Task<DialogTurnResult> ProposeTips(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // Get User Stress Handling Preference Choice
            var choice = (FoundChoice)sc.Result;
            if (choice.Value == "Breather")
            {
                sc.SuppressCompletionMessage(true);

                return await sc.BeginDialogAsync(_breatherDialog.Id);
            }
            
            if (choice.Value == "Talk to me")
            {
                sc.SuppressCompletionMessage(true);

                return await sc.BeginDialogAsync(_stressHandlingDialog.Id);
            }
            else 
            {
                sc.SuppressCompletionMessage(true);

                return await sc.BeginDialogAsync(_escalateDialog.Id);
            }
         
        }
        private async Task<DialogTurnResult> Complete(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync();
        }
        private class DialogIds
        {
            public const string TipsPrompt = "tipsPrompt";
        }



    }
}
