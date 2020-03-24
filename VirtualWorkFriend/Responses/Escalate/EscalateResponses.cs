// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace MyAssistant_1.Responses.Escalate
{
    public class EscalateResponses : TemplateManager
    {
        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                { ResponseIds.SendPhoneMessage, (context, data) => BuildEscalateCard(context, data) },
            }
        };

        public EscalateResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public static IMessageActivity BuildEscalateCard(ITurnContext turnContext, dynamic data)
        {
            var attachment = new HeroCard()
            {
                Text = $"{EscalateStrings.PHONE_INFO} \U0001F4DE",
                Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.OpenUrl, title: "Call now", value: "tel:+33622037720"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Open Teams Channel", value: "https://teams.microsoft.com/l/channel/19%3a26b516eeb8b048328ecd4eceb674e506%40thread.skype/Talk%2520to%2520a%2520human?groupId=74e84760-c73f-48cf-b20e-d0ddb8313f90&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47"),
                    new CardAction(ActionTypes.OpenUrl, "Benefits of talking to someone",
                        value:
                        "https://ie.reachout.com/getting-help-2/face-to-face-help/things-you-need-to-know/benefits-of-talking-to-someone/")
                },
            }.ToAttachment();

            return MessageFactory.Attachment(attachment, null, EscalateStrings.PHONE_INFO, InputHints.AcceptingInput);
        }

        public class ResponseIds
        {
            public const string SendPhoneMessage = "sendPhoneMessage";
        }
    }
}
