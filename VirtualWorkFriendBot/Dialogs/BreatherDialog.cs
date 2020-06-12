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

    public class BreatherDialog : ComponentDialog
    {
        private BotServices _services;

        private CancelDialog _cancelDialog;
        private StressHandlingDialog _stressHandlingDialog;

        public BreatherDialog(
        BotServices botServices,
        IServiceProvider serviceProvider,
        IBotTelemetryClient telemetryClient)
            : base(nameof(BreatherDialog))
        {
            _services = botServices;

            InitialDialogId = nameof(BreatherDialog);


var steps = new WaterfallStep[]
          {
                Initiate,
                Feedback,
                FeedbackProcessAsync,
                Complete
          };

            _cancelDialog = serviceProvider.GetService<CancelDialog>();
            _stressHandlingDialog = serviceProvider.GetService<StressHandlingDialog>();
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)) { Style = ListStyle.HeroCard });
            AddDialog(_cancelDialog);
            AddDialog(_stressHandlingDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, steps));

        }
private  IMessageActivity GetEntertainCard()
        {

            var attachment = new Microsoft.Bot.Schema.Attachment(contentType: "video/mp4",
                name: "5 minutes Meditation",
                thumbnailUrl: "https://www.yogajournal.com/.image/t_share/MTQ2MTgwNzM5MDQ5OTg5NjY0/sunset-meditation-mudra.jpg",
                contentUrl: "https://www.youtube.com/watch?v=inpok4MKVLM&t=14s");

            return MessageFactory.Attachment(attachment);
        }

private async Task<DialogTurnResult> Initiate(WaterfallStepContext sc, CancellationToken cancellationToken)
{
    var card =  GetEntertainCard();
   int  milliseconds = 1000;
    Thread.Sleep(milliseconds);
    await sc.Context.SendActivityAsync(card, cancellationToken);
   return await sc.NextAsync();
        }



private async Task<DialogTurnResult> Feedback(WaterfallStepContext stepContext, CancellationToken cancellationToken)
{
    var newFeedback = new List<string> { "I am good now", "I still need to talk to you" };
            int milliseconds = 8000;
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
    if (choice.Value == "I am good now")
    {
        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Glad to hear it. See you next time!"));
        return await stepContext.EndDialogAsync();
    }
    else
    {

        return await stepContext.BeginDialogAsync(_stressHandlingDialog.Id);

    }

}

        private async Task<DialogTurnResult> Complete(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync();
        }
    }
}
