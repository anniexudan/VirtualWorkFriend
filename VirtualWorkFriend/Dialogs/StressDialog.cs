// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MyAssistant_1.Services;

namespace MyAssistant_1.Dialogs
{
    using Helpers;
    using Luis;

    public class StressDialog : ComponentDialog
    {
        private BotServices _services;

        public StressDialog(BotServices botServices, EntertainDialog entertainDialog, IBotTelemetryClient telemetryClient)
            : base(nameof(StressDialog))
        {
            _services = botServices;
            InitialDialogId = nameof(StressDialog);

            var steps = new WaterfallStep[]
            {
                Initiate,
                ProposeTips
            };

            AddDialog(entertainDialog);

            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new TextPrompt(DialogIds.TipsPrompt));
        }

        private async Task<DialogTurnResult> Initiate(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(
                    "I have some tips for you to help reduce the stress. Are you curious? \U0001F917")
            };

            return await sc.PromptAsync(DialogIds.TipsPrompt, promptOptions);
        }

        private async Task<DialogTurnResult> ProposeTips(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var intent = await LuisHelper.GetIntent(_services, sc, cancellationToken);

            if (intent == GeneralLuis.Intent.Confirm)
            {
                return await sc.BeginDialogAsync(nameof(EntertainDialog));
            }
            return await sc.EndDialogAsync();
        }
       
        private class DialogIds
        {
            public const string TipsPrompt = "tipsPrompt";
        }
    }
}
