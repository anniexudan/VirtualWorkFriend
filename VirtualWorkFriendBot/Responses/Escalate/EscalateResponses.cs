// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using Microsoft.Graph;

namespace VirtualWorkFriendBot.Responses.Escalate
{
    public class EscalateResponses : TemplateManager
    {
        private LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.SendPhoneMessage, (context, data) => BuildEscalateCard(context, data)
                },

                {
                    ResponseIds.TherapistOnlinePrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: EscalateStrings.THERAPIST_ONLINE,
                            ssml: EscalateStrings.THERAPIST_ONLINE,
                            inputHint: InputHints.ExpectingInput)
                },

                {
                    ResponseIds.UserLocationPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: EscalateStrings.USER_LOCATION,
                            ssml: EscalateStrings.USER_LOCATION,
                            inputHint: InputHints.ExpectingInput)
                },

                {
                    ResponseIds.TherapistRadiusPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: EscalateStrings.THERAPIST_RADIUS,
                            ssml: EscalateStrings.THERAPIST_RADIUS,
                            inputHint: InputHints.ExpectingInput)
                },

                {
                    ResponseIds.DataConsentPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: EscalateStrings.DATA_CONSENT,
                            ssml: EscalateStrings.DATA_CONSENT,
                            inputHint: InputHints.ExpectingInput)
                },

                {
                    ResponseIds.BeginSearch,
                    (context, data) =>
                        MessageFactory.Text(
                            text: EscalateStrings.THERAPIST_SEARCH,
                            ssml: EscalateStrings.THERAPIST_SEARCH,
                            inputHint: InputHints.IgnoringInput)
                },

                {
                    ResponseIds.NoneFound,
                    (context, data) =>
                        MessageFactory.Text(
                            text: EscalateStrings.NONE_FOUND,
                            ssml: EscalateStrings.NONE_FOUND,
                            inputHint: InputHints.IgnoringInput)
                }
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
            public const string TherapistOnlinePrompt = "therapistOnlinePrompt";
            public const string UserLocationPrompt = "userLocationPrompt";
            public const string TherapistRadiusPrompt = "therapistRadiusPrompt";
            public const string DataConsentPrompt = "dataConsentPrompt";
            public const string BeginSearch = "beginSearch";
            public const string NoneFound = "noneFound";
        }
    }
}
