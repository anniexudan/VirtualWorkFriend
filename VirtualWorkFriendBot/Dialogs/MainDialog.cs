// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Models;
using VirtualWorkFriendBot.Services;

namespace VirtualWorkFriendBot.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : VirtualFriendDialog
    {
        #region Fields
        private BotServices _services;
        private BotSettings _settings;
        private OnboardingDialog _onboardingDialog;
        private SwitchSkillDialog _switchSkillDialog;
        private SkillsConfiguration _skillsConfig;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<UserProfileState> _userProfileState;
        private IStatePropertyAccessor<List<Activity>> _previousResponseAccessor;
        private EscalateDialog _escalateDialog;
        private CancelDialog _cancelDialog;
        private EntertainDialog _entertainDialog;
        private ChitchatDialog _chitchatdialog;
        private LoginDialog _loginDialog;
        private OAuthPrompt _oauthPrompt;
        private StressDialog _stressDialog;
        private HighStressHandlingDialog _highstresshandlingDialog;
        private StressHandlingDialog _stresshandlingDialog;
        private JournalingDialog _journalingDialog;
        #endregion

        #region Constructors
        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), serviceProvider, telemetryClient)
        {
            _services = serviceProvider.GetService<BotServices>();
            _settings = serviceProvider.GetService<BotSettings>();
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
            _skillsConfig = serviceProvider.GetService<SkillsConfiguration>();

            var userState = serviceProvider.GetService<UserState>();
            _userProfileState = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));
            var conversationState = serviceProvider.GetService<ConversationState>();
            _previousResponseAccessor = conversationState.CreateProperty<List<Activity>>(StateProperties.PreviousBotResponse);

            WaterfallStep[] steps = SetupWaterfallSteps();

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)) { Style = ListStyle.HeroCard });
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _oauthPrompt = serviceProvider.GetService<OAuthPrompt>();
            _loginDialog = serviceProvider.GetService<LoginDialog>();
            _onboardingDialog = serviceProvider.GetService<OnboardingDialog>();
            _switchSkillDialog = serviceProvider.GetService<SwitchSkillDialog>();
            _escalateDialog = serviceProvider.GetService<EscalateDialog>();
            _cancelDialog = serviceProvider.GetService<CancelDialog>();
            _entertainDialog = serviceProvider.GetService<EntertainDialog>();
            _chitchatdialog = serviceProvider.GetService<ChitchatDialog>();
            _stressDialog = serviceProvider.GetService<StressDialog>();
            _highstresshandlingDialog = serviceProvider.GetService<HighStressHandlingDialog>();
            _stresshandlingDialog = serviceProvider.GetService<StressHandlingDialog>();
            _journalingDialog = serviceProvider.GetService<JournalingDialog>();

            AddDialog(_oauthPrompt);
            AddDialog(_loginDialog);
            AddDialog(_onboardingDialog);
            AddDialog(_switchSkillDialog);
            AddDialog(_escalateDialog);
            AddDialog(_cancelDialog);
            AddDialog(_entertainDialog);
            AddDialog(_chitchatdialog);
            AddDialog(_stressDialog);
            AddDialog(_highstresshandlingDialog);
            AddDialog(_stresshandlingDialog);
            AddDialog(_journalingDialog);




            // Register a QnAMakerDialog for each registered knowledgebase and ensure localised responses are provided.
            var localizedServices = _services.GetCognitiveModels();
            foreach (var knowledgebase in localizedServices.QnAConfiguration)
            {
                var qnaDialog = new QnAMakerDialog(
                    knowledgeBaseId: knowledgebase.Value.KnowledgeBaseId,
                    endpointKey: knowledgebase.Value.EndpointKey,
                    hostName: knowledgebase.Value.Host,
                    noAnswer: _templateEngine.GenerateActivityForLocale("UnsupportedMessage"),
                    activeLearningCardTitle: _templateEngine.GenerateActivityForLocale("QnaMakerAdaptiveLearningCardTitle").Text,
                    cardNoMatchText: _templateEngine.GenerateActivityForLocale("QnaMakerNoMatchText").Text)
                {
                    Id = knowledgebase.Key
                };
                AddDialog(qnaDialog);
            }

            // Register skill dialogs
            var skillDialogs = serviceProvider.GetServices<SkillDialog>();
            foreach (var dialog in skillDialogs)
            {
                AddDialog(dialog);
            }
        }


        #endregion

        private WaterfallStep[] SetupWaterfallSteps()
        {
            return new WaterfallStep[] {
                LoginDialogStepAsync,
                OnboardingStepAsync,
                IntroStepAsync,
                StressLevelProcessAsync,
                RouteStepAsync,
                FinalStepAsync,
            };
        }

        #region Overrides
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition and store result in turn state.
                var dispatchResult = await localizedServices.DispatchService.RecognizeAsync<DispatchLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.DispatchResult, dispatchResult);

                if (dispatchResult.TopIntent().intent == DispatchLuis.Intent.l_General)
                {
                    // Run LUIS recognition on General model and store result in turn state.
                    var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            // Set up response caching for "repeat" functionality.
            innerDc.Context.OnSendActivities(StoreOutgoingActivities);
            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition and store result in turn state.
                var dispatchResult = await localizedServices.DispatchService.RecognizeAsync<DispatchLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.DispatchResult, dispatchResult);

                if (dispatchResult.TopIntent().intent == DispatchLuis.Intent.l_General)
                {
                    // Run LUIS recognition on General model and store result in turn state.
                    var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralResult, generalResult);
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            // Set up response caching for "repeat" functionality.
            innerDc.Context.OnSendActivities(StoreOutgoingActivities);
            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;
            var userProfile = await _userProfileState.GetAsync(innerDc.Context, () => new UserProfileState());
            var dialog = innerDc.ActiveDialog?.Id != null ? innerDc.FindDialog(innerDc.ActiveDialog?.Id) : null;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Check if the active dialog is a skill for conditional interruption.
                var isSkill = dialog is SkillDialog;

                // Get Dispatch LUIS result from turn state.
                var dispatchResult = innerDc.Context.TurnState.Get<DispatchLuis>(StateProperties.DispatchResult);
                (var dispatchIntent, var dispatchScore) = dispatchResult.TopIntent();

                // Check if we need to switch skills.
                if (isSkill && IsSkillIntent(dispatchIntent) && dispatchIntent.ToString() != dialog.Id && dispatchScore > 0.9)
                {
                    EnhancedBotFrameworkSkill identifiedSkill;
                    if (_skillsConfig.Skills.TryGetValue(dispatchIntent.ToString(), out identifiedSkill))
                    {
                        var prompt = _templateEngine.GenerateActivityForLocale("SkillSwitchPrompt", new { Skill = identifiedSkill.Name });
                        await innerDc.BeginDialogAsync(_switchSkillDialog.Id, new SwitchSkillDialogOptions(prompt, identifiedSkill));
                        interrupted = true;
                    }
                    else
                    {
                        throw new ArgumentException($"{dispatchIntent.ToString()} is not in the skills configuration");
                    }
                }

                if (dispatchIntent == DispatchLuis.Intent.l_General)
                {
                    // Get connected LUIS result from turn state.
                    var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralResult);
                    (var generalIntent, var generalScore) = generalResult.TopIntent();

                    if (generalScore > 0.5)
                    {
                        switch (generalIntent)
                        {
                            case GeneralLuis.Intent.Cancel:
                                {
                                    await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("CancelledMessage", userProfile));
                                    await innerDc.CancelAllDialogsAsync();
                                    await innerDc.BeginDialogAsync(InitialDialogId);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Escalate:
                                {
                                    await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("EscalateMessage", userProfile));
                                    await innerDc.RepromptDialogAsync();
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Help:
                                {
                                    if (!isSkill)
                                    {
                                        // If current dialog is a skill, allow it to handle its own help intent.
                                        await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("HelpCard", userProfile));
                                        await innerDc.RepromptDialogAsync();
                                        interrupted = true;
                                    }

                                    break;
                                }

                            case GeneralLuis.Intent.Logout:
                                {
                                    // Log user out of all accounts.
                                    await LogUserOut(innerDc);

                                    await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("LogoutMessage", userProfile));
                                    await innerDc.CancelAllDialogsAsync();
                                    await innerDc.BeginDialogAsync(InitialDialogId);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Repeat:
                                {
                                    // Sends the activities since the last user message again.
                                    var previousResponse = await _previousResponseAccessor.GetAsync(innerDc.Context, () => new List<Activity>());

                                    foreach (var response in previousResponse)
                                    {
                                        // Reset id of original activity so it can be processed by the channel.
                                        response.Id = string.Empty;
                                        await innerDc.Context.SendActivityAsync(response);
                                    }

                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.StartOver:
                                {
                                    await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("StartOverMessage", userProfile));

                                    // Cancel all dialogs on the stack.
                                    await innerDc.CancelAllDialogsAsync();
                                    await innerDc.BeginDialogAsync(InitialDialogId);
                                    interrupted = true;
                                    break;
                                }

                            case GeneralLuis.Intent.Stop:
                                {
                                    // Use this intent to send an event to your device that can turn off the microphone in speech scenarios.
                                    break;
                                }
                            case GeneralLuis.Intent.Stress:
                                {
                                    await innerDc.BeginDialogAsync(nameof(StressDialog));
                                    interrupted = true;
                                    break;
                                }
                            case GeneralLuis.Intent.UpdateProfile:
                                {
                                    await innerDc.BeginDialogAsync(nameof(OnboardingDialog));
                                    interrupted = true;
                                    break;
                                }
                        }
                    }
                }
            }

            return interrupted;
        }
        #endregion

        private async Task<DialogTurnResult> LoginDialogStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            if (_discussionState.UserToken == null)
            {
                return await stepContext.BeginDialogAsync(_loginDialog.Id);
            }

            return await stepContext.NextAsync();
        }
        private async Task<DialogTurnResult> OnboardingStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            if (!_onboardingState.PrivacyAccepted)
            {
                return await stepContext.BeginDialogAsync(_onboardingDialog.Id);
            }

            return await stepContext.NextAsync();
        }

        private enum StressResponses
        {
            [Description("Low")]
            Low,

            [Description("Medium")]
            Medium,

            [Description("High")]
            High,

            [Description("Talk to a person directly")]
            Escalate,

            [Description("Reflect on my journey")]
            Journal,

            [Description("See my journal trends")]
            SeeTrends
        }

        
        /**** Annie Edit ***/
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var newStressLevelList = EnumHelpers.GetDescriptions<StressResponses>().ToList();
               
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
            {
                Prompt = stepContext.Options as Activity ?? _templateEngine.GenerateActivityForLocale("FirstPromptMessage"),
                Choices = ChoiceFactory.ToChoices(newStressLevelList),
                RetryPrompt = MessageFactory.Text("Your Stress Level")
            }, cancellationToken);
        }
       
        private async Task<DialogTurnResult> StressLevelProcessAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get User Stress Level Choice
            var choice = (FoundChoice)stepContext.Result;
            var targetDialogId = String.Empty;
            if (choice.Value.TryGetValueFromDescription<StressResponses>(
                out StressResponses response))
            {
                switch (response)
                {
                    case StressResponses.Low:
                        targetDialogId = _chitchatdialog.Id;
                        break;
                    case StressResponses.Medium:
                        targetDialogId = _stresshandlingDialog.Id;
                        break;
                    case StressResponses.High:
                        targetDialogId = _highstresshandlingDialog.Id;
                        break;

                    case StressResponses.Escalate:
                        targetDialogId = _escalateDialog.Id;
                        break;
                    case StressResponses.Journal:
                        targetDialogId = _journalingDialog.Id;
                        break;
                    case StressResponses.SeeTrends:
                        await ShowTrendsLink(stepContext, cancellationToken);
                        return await stepContext.NextAsync(new ProcessingComplete(), cancellationToken);
                    default:
                        break;
                }
            }            

            if (!String.IsNullOrEmpty(targetDialogId))
            {
                stepContext.SuppressCompletionMessage(true);
                return await stepContext.BeginDialogAsync(targetDialogId);
            }            
            return await stepContext.NextAsync();
        }


        private async Task ShowTrendsLink(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {            
            Action<List<CardAction>> fnAddButtons = new Action<List<CardAction>>((buttons) =>
            {
                _configuration.GetSection("powerBIDashboards")
                    .GetChildren()
                    .Select(item => new
                    {
                        Title = item.GetValue<string>("Title"),
                        Url = item.GetValue<string>("Url")
                    })
                    .ToList()
                    .ForEach(item => buttons.Add(CreateOpenUrlAction(item.Title, item.Url)));
            });

            await CreateLinkCard(stepContext, cancellationToken, "My Trends", fnAddButtons);            
        }        

        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var pc = stepContext.Result as ProcessingComplete;

            if (pc != null)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var activity = stepContext.Context.Activity.AsMessageActivity();
            var userProfile = await _userProfileState.GetAsync(stepContext.Context, () => new UserProfileState());

            if (!string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Get dispatch result from turn state.
                var dispatchResult = stepContext.Context.TurnState.Get<DispatchLuis>(StateProperties.DispatchResult);
                (var dispatchIntent, var dispatchScore) = dispatchResult.TopIntent();

                if (IsSkillIntent(dispatchIntent))
                {
                    var dispatchIntentSkill = dispatchIntent.ToString();
                    var skillDialogArgs = new SkillDialogArgs { SkillId = dispatchIntentSkill };

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

                    await stepContext.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale("UnsupportedMessage", userProfile));
                    return await stepContext.NextAsync();
                }
            }
            else
            {
                return await stepContext.NextAsync();
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            //  return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateEngine.GenerateActivityForLocale("CompletedMessage"), cancellationToken);

            return await stepContext.EndDialogAsync();
        }

        #region Support
        private async Task LogUserOut(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private async Task<ResourceResponse[]> StoreOutgoingActivities(ITurnContext turnContext, List<Activity> activities, Func<Task<ResourceResponse[]>> next)
        {
            var messageActivities = activities
                .Where(a => a.Type == ActivityTypes.Message)
                .ToList();

            // If the bot is sending message activities to the user (as opposed to trace activities)
            if (messageActivities.Any())
            {
                var botResponse = await _previousResponseAccessor.GetAsync(turnContext, () => new List<Activity>());

                // Get only the activities sent in response to last user message
                botResponse = botResponse
                    .Concat(messageActivities)
                    .Where(a => a.ReplyToId == turnContext.Activity.Id)
                    .ToList();

                await _previousResponseAccessor.SetAsync(turnContext, botResponse);
            }

            return await next();
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
        #endregion
    }
}