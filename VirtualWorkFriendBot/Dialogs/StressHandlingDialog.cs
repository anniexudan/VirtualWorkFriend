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
    using System.Collections.Generic;

    public class StressHandlingDialog : ComponentDialog
    {
        private BotServices _services;
        private string DlgStressReason = "";
        private EscalateDialog _escalateDialog;
        private KnowledgeBaseDialog _knowledgebaseDialog;
        public StressHandlingDialog(BotServices botServices, EntertainDialog entertainDialog, IBotTelemetryClient telemetryClient, IServiceProvider serviceProvider)
            : base(nameof(StressHandlingDialog))
        {
            _services = botServices;
            InitialDialogId = nameof(StressHandlingDialog);

            var steps = new WaterfallStep[]
            {
                Initiate,
                RespondChoice,
                ProposeTips,
                Complete
            };

    
            _escalateDialog = serviceProvider.GetService<EscalateDialog>();
            AddDialog(_escalateDialog);

            _knowledgebaseDialog = serviceProvider.GetService<KnowledgeBaseDialog>();
            AddDialog(_knowledgebaseDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new TextPrompt(DialogIds.TipsPrompt));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
        }

        private async Task<DialogTurnResult> Initiate(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = MessageFactory.Text(
                    "I see. So, could you tell me what bothers you today?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> RespondChoice(WaterfallStepContext sc,CancellationToken cancellationToken)
        {
            var newStressLevelList = new List<string> { "Yes, show me something interesting", "Back up with knowledge","Talk to a therapist directly" };
            return await sc.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
            {
                Prompt = MessageFactory.Text(
                   "Yes," + "it is stressful. I have some tips for you to handle the stress. Would you like to know? \U0001F917"),
                   Choices = ChoiceFactory.ToChoices(newStressLevelList),
                RetryPrompt = MessageFactory.Text("Would you like to know my stress handling tips? \U0001F917")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ProposeTips(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // Get User Stress Level Choice
            var choice = (FoundChoice)sc.Result;
            if (choice.Value == "Yes, show me something interesting")
            {
           
                
                    return await sc.BeginDialogAsync(nameof(EntertainDialog));
              
            }
            else if (choice.Value == "Back up with knowledge") {
                sc.SuppressCompletionMessage(true);

                return await sc.BeginDialogAsync(_knowledgebaseDialog.Id);
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
