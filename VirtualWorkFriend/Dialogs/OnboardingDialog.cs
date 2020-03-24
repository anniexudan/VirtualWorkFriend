// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MyAssistant_1.Models;
using MyAssistant_1.Responses.Onboarding;
using MyAssistant_1.Services;

namespace MyAssistant_1.Dialogs
{
    using System;
    using System.Linq;
    using Microsoft.EntityFrameworkCore.ValueGeneration;

    public class OnboardingDialog : ComponentDialog
    {
        private static OnboardingResponses _responder = new OnboardingResponses();
        private IStatePropertyAccessor<OnboardingState> _accessor;
        private OnboardingState _state;

        public OnboardingDialog(
            BotServices botServices,
            UserState userState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(OnboardingDialog))
        {
            _accessor = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            InitialDialogId = nameof(OnboardingDialog);

            var onboarding = new WaterfallStep[]
            {
                AskForName,
                AskForReadingInterests,
                AskForMusicInterests,
                FinishOnboardingDialog,
            };

            // To capture built-in waterfall dialog telemetry, set the telemetry client
            // to the new waterfall dialog and add it to the component dialog
            TelemetryClient = telemetryClient;
            AddDialog(new WaterfallDialog(InitialDialogId, onboarding) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(DialogIds.NamePrompt));
            AddDialog(new TextPrompt(DialogIds.ReadingInterestsPrompt));
            AddDialog(new TextPrompt(DialogIds.MusicInterestsPrompt));
        }

        public async Task<DialogTurnResult> AskForName(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());

            if (!string.IsNullOrEmpty(_state.Name))
            {
                return await sc.NextAsync(_state.Name);
            }
            else
            {
                return await sc.PromptAsync(DialogIds.NamePrompt, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale, OnboardingResponses.ResponseIds.NamePrompt),
                });
            }
        }

        public async Task<DialogTurnResult> AskForReadingInterests(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());

            if (_state.ReadingInterests.Count > 0)
            {
                return await sc.NextAsync(_state.ReadingInterests);
            }

            var name = _state.Name = (string)sc.Result;
            await _accessor.SetAsync(sc.Context, _state, cancellationToken);

            string greeting = GetGreeting(sc);
            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveNameMessage, 
                new {greeting, name = $"{name} \U0001F600"});

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
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());

            if (_state.MusicInterests.Count > 0)
            {
                return await sc.NextAsync(_state.MusicInterests);
            }

            var readingInterests = _state.ReadingInterests = ((string)sc.Result)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            await _accessor.SetAsync(sc.Context, _state, cancellationToken);

            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveReadingInterests,
                new { name = $"{_state.Name} \U0001F44D", robots = "\U0001F916", interest = readingInterests[0] });

            return await sc.PromptAsync(DialogIds.MusicInterestsPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale,
                    OnboardingResponses.ResponseIds.MusicInterestsPrompt),
            });
        }

        public async Task<DialogTurnResult> FinishOnboardingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            _state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            var musicInterests = _state.MusicInterests = ((string) sc.Result)
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
            await _accessor.SetAsync(sc.Context, _state, cancellationToken);

            await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveMusicInterests,
                new {groovy = "\U0001F57A" });
            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
            public const string ReadingInterestsPrompt = "readingInterestsPrompt";
            public const string MusicInterestsPrompt = "musicInterestsPrompt";
        }
    }
}
