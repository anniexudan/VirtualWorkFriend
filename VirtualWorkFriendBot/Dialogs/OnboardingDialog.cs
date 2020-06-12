// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Models;
using VirtualWorkFriendBot.Responses.Onboarding;
using VirtualWorkFriendBot.Services;

namespace VirtualWorkFriendBot.Dialogs
{
    public class OnboardingDialog : VirtualFriendDialog
    {
        
        private static OnboardingResponses _responder = new OnboardingResponses();

        public OnboardingDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog), serviceProvider, telemetryClient)
        {
            InitialDialogId = nameof(OnboardingDialog);

            var onboarding = new WaterfallStep[]
            {
                PromptPrivacy,
                ValidatePrivacy,

                PromptTOU,
                ValidateTOU,

                AskForName,
                AskForReadingInterests,
                AskForMusicInterests,

                FinishOnboardingDialog,
            };
           
            AddDialog(new WaterfallDialog(InitialDialogId, onboarding) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(DialogIds.NamePrompt));
            AddDialog(new TextPrompt(DialogIds.ReadingInterestsPrompt));
            AddDialog(new TextPrompt(DialogIds.MusicInterestsPrompt));
            AddDialog(new TextPrompt(DialogIds.UpdateMusicInterests));
            AddDialog(new TextPrompt(DialogIds.UpdateReadingInterests));
            AddDialog(new TextPrompt(DialogIds.UpdateName));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmPrompt));
        }

        public async Task<DialogTurnResult> AskForName(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {
                // 'What would you like to be called?'
                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, OnboardingResponses.ResponseIds.UpdateName,
                                                                new { name = _onboardingState.Name } ),
                });
            }

            if (!string.IsNullOrEmpty(_onboardingState.Name))
            {
                return await sc.NextAsync(_onboardingState.Name);
            }
            else
            {
                await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.TOUAcceptedPrompt);

                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, OnboardingResponses.ResponseIds.NamePrompt),
                });
            }
        }

        public async Task<DialogTurnResult> AskForReadingInterests(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {
                string none = "None";
                // Process user's new name
                if (!none.Equals((string)sc.Result, StringComparison.OrdinalIgnoreCase))
                {
                    _onboardingState.Name = (string)sc.Result;
                    await SaveOnboardingState(sc.Context);
                }

                // Display existing reading interests
                var readingInterestsString = String.Join(", ", _onboardingState.ReadingInterests);

                await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.DisplayReadingInterests,
                    new { readingInterests = readingInterestsString });

                // Prompt the user for additional reading interests
                return await sc.PromptAsync(DialogIds.UpdateReadingInterests, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale,
                    OnboardingResponses.ResponseIds.UpdateReadingInterests),
                });
            }

            if (_onboardingState.ReadingInterests.Count > 0)
            {
                return await sc.NextAsync(_onboardingState.ReadingInterests);
            }

            var name = _onboardingState.Name = (string)sc.Result;
            await SaveOnboardingState(sc.Context);

            string greeting = GetGreeting(sc);
            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveNameMessage,
                new { greeting, name = $"{name} \U0001F600" });

            return await sc.PromptAsync(DialogIds.ReadingInterestsPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale,
                    OnboardingResponses.ResponseIds.ReadingInterestsPrompt),
            });
        }

        private string GetGreeting(WaterfallStepContext sc)
        {
            var localTimestamp = sc.Context.Activity?.Timestamp;
            if (localTimestamp.HasValue)
            {
                if (localTimestamp.Value.Hour < 5 || localTimestamp.Value.Hour > 17)
                {
                    return "Good evening";
                }

                if (localTimestamp.Value.Hour >= 5 && localTimestamp.Value.Hour <= 12)
                {
                    return "Good morning";
                }

                if (localTimestamp.Value.Hour > 12 && localTimestamp.Value.Hour <= 17)
                {
                    return "Good afternoon";
                }
            }

            return "Hi";
        }

        public async Task<DialogTurnResult> AskForMusicInterests(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {

                string none = "None";
                // Process user's reading interests
                if (!none.Equals((string)sc.Result, StringComparison.OrdinalIgnoreCase))
                {
                    var newReadingInterests = ((string)sc.Result)
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    // _onboardingState.ReadingInterests.Clear();

                    _onboardingState.ReadingInterests.AddRange(newReadingInterests);

                    await SaveOnboardingState(sc.Context);
                }

                // Display user's current music interests

                var musicInterestsString = String.Join(", ", _onboardingState.MusicInterests);

                await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.DisplayMusicInterests,
                    new { musicInterests = musicInterestsString });

                // Prompt the user for additional reading interests
                return await sc.PromptAsync(DialogIds.UpdateMusicInterests, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale,
                    OnboardingResponses.ResponseIds.UpdateMusicInterests),
                });
            }

            if (_onboardingState.MusicInterests.Count > 0)
            {
                return await sc.NextAsync(_onboardingState.MusicInterests);
            }

            var readingInterests = _onboardingState.ReadingInterests = ((string)sc.Result)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            await SaveOnboardingState(sc.Context);

            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveReadingInterests,
                new { name = $"{_onboardingState.Name} \U0001F44D", robots = "\U0001F916", interest = readingInterests[0] });

            return await sc.PromptAsync(DialogIds.MusicInterestsPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale,
                    OnboardingResponses.ResponseIds.MusicInterestsPrompt),
            });
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {
                string none = "None";
                // Process user's music interests
                if (!none.Equals((string)sc.Result, StringComparison.OrdinalIgnoreCase))
                {
                    var newMusicInterests = ((string)sc.Result)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    _onboardingState.MusicInterests.AddRange(newMusicInterests);

                    await SaveOnboardingState(sc.Context);
                }

                await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.InformationUpdated);
            }

            else
            {
                _onboardingState.MusicInterests = ((string)sc.Result)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                await SaveOnboardingState(sc.Context);

                await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveMusicInterests,
                    new { groovy = "\U0001F57A" });
            }

            _onboardingState.NewUser = (bool)false;

            await SaveOnboardingState(sc.Context);

            return await sc.EndDialogAsync(_onboardingState);
        }

        public async Task<DialogTurnResult> PromptPrivacy(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            // respond to user

            //_state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            /*if (_onboardingState.PrivacyAccepted)
            {
                return await sc.EndDialogAsync(_onboardingState);
            }*/

            if (!_onboardingState.NewUser)
            {
                return await sc.NextAsync(_onboardingState);
            }

            // ask user about privacy with confirm prompt
            return await sc.PromptAsync(DialogIds.ConfirmPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you accept our privacy policy?"),
                    Style = ListStyle.HeroCard
                }
                );
        }

        public async Task<DialogTurnResult> ValidatePrivacy(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {
                return await sc.NextAsync(_onboardingState);
            }

            bool accepted = (bool)sc.Result;

            if (!accepted)
            {
                // TODO: Create message to send to the user for not accepting

                // End the dialog
                return await sc.EndDialogAsync();
            }
            _onboardingState.PrivacyAccepted = true;
            await SaveOnboardingState(sc.Context);
            return await sc.NextAsync(accepted);
        }

        public async Task<DialogTurnResult> PromptTOU(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {
                return await sc.NextAsync(_onboardingState);
            }

            // respond to user
            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.PrivacyAcceptedPrompt);

            // ask user about privacy with confirm prompt
            return await sc.PromptAsync(DialogIds.ConfirmPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you accept our terms of use?"),
                    Style = ListStyle.HeroCard
                }
                );
        }
        public async Task<DialogTurnResult> ValidateTOU(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(sc);

            if (!_onboardingState.NewUser)
            {
                return await sc.NextAsync(_onboardingState); // do i even need to add state here?
            }

            bool accepted = (bool)sc.Result;

            if (!accepted)
            {
                // TODO: Create message to send to the user for not accepting

                // End the dialog
                return await sc.EndDialogAsync();
            }
            _onboardingState.TermsAccepted = true;
            await SaveOnboardingState(sc.Context);
            return await sc.NextAsync();
        }
        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
            public const string ReadingInterestsPrompt = "readingInterestsPrompt";
            public const string MusicInterestsPrompt = "musicInterestsPrompt";
            public const string ConfirmPrompt = "confirmPrompt";
            public const string UpdateReadingInterests = "updateReadingInterests";
            public const string UpdateMusicInterests = "updateMusicInterests";
            public const string UpdateName = "updateName";
        }
    }

}
