// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using Luis;
using System.Threading.Tasks;
using VirtualWorkFriendBot.Services;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Models;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Extensions;


namespace VirtualWorkFriendBot.Dialogs
{
    using Microsoft.Bot.Builder.Dialogs.Choices;
    using Microsoft.Bot.Schema;
    using System;
    using System.Collections.Generic;

    public class KnowledgeBaseDialog : ComponentDialog
    {
        private BotServices _services;
        private IStatePropertyAccessor<UserProfileState> _userProfileState;
        private CancelDialog _cancelDialog;
        private StressHandlingDialog _stressHandlingDialog;
        private LocaleTemplateEngineManager _templateEngine;

        public KnowledgeBaseDialog(
        BotServices botServices,
        IServiceProvider serviceProvider,
        IBotTelemetryClient telemetryClient)
            : base(nameof(KnowledgeBaseDialog))
        {
            _services = botServices;

            InitialDialogId = nameof(KnowledgeBaseDialog);


            var steps = new WaterfallStep[]
            {
                Initiate,
                TriggerQNA,
                Feedback,
                FeedbackProcessAsync,
                Complete
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
                Prompt = MessageFactory.Text("Let me know what do you want to know?"),
                RetryPrompt = MessageFactory.Text("Let me know what do you want to know?"),
            }, cancellationToken);
        }
        private async Task<DialogTurnResult> TriggerQNA(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var activity = stepContext.Context.Activity.AsMessageActivity();
        //    var userProfile = await _userProfileState.GetAsync(stepContext.Context, () => new VirtualWorkFriendBot.Models.UserProfileState());

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Get dispatch result from turn state.
                var dispatchResult = stepContext.Context.TurnState.Get<Luis.DispatchLuis>(StateProperties.DispatchResult);
                (var dispatchIntent, var dispatchScore) = dispatchResult.TopIntent();

                if (IsSkillIntent(dispatchIntent))
                {
                    var dispatchIntentSkill = dispatchIntent.ToString();
                    var skillDialogArgs = new Microsoft.Bot.Solutions.Skills.SkillDialogArgs { SkillId = dispatchIntentSkill };

                    // Start the skill dialog.
                    return await stepContext.BeginDialogAsync(dispatchIntentSkill, skillDialogArgs);
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_Faq)
                {
                    stepContext.SuppressCompletionMessage(true);

                    return await stepContext.BeginDialogAsync("Faq");
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_Chitchat)
                {
                    stepContext.SuppressCompletionMessage(true);

                    return await stepContext.BeginDialogAsync("Chitchat");
                }
                else if (dispatchIntent == DispatchLuis.Intent.q_COVID19)
                {
                    stepContext.SuppressCompletionMessage(true);

                    return await stepContext.BeginDialogAsync("COVID19");
                }
                else
                {
                    stepContext.SuppressCompletionMessage(true);

                    return await stepContext.BeginDialogAsync("Faq");
                }
            }
            else
            {
                return await stepContext.NextAsync();
            }

        }
        private bool IsSkillIntent(DispatchLuis.Intent dispatchIntent)
        {
            if (dispatchIntent.ToString().Equals(DispatchLuis.Intent.l_General.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_Faq.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_Chitchat.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.q_COVID19.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                dispatchIntent.ToString().Equals(DispatchLuis.Intent.None.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }
        private async Task<DialogTurnResult> Feedback(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newFeedback = new List<string> { "Yes, bye", "More Knowledge" };
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
            if (choice.Value == "More Knowledge")
            {
                return await stepContext.ReplaceDialogAsync(nameof(KnowledgeBaseDialog));
            }
            else
            {

                return await stepContext.EndDialogAsync();

            }

        }
        private async Task<DialogTurnResult> Complete(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync();
        }

    }
}
