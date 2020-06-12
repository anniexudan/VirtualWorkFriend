// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
// using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualWorkFriendBot.Responses.Escalate;
using VirtualWorkFriendBot.Services;
using VirtualWorkFriendBot.Models;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
// Models namespace has onboardingState
using System.Globalization;
using System.Linq;
using Luis;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using VirtualWorkFriendBot.Helpers;
using VirtualWorkFriendBot.Responses.Onboarding;
using Newtonsoft.Json.Linq;
using AdaptiveCards.Templating;
using Microsoft.Bot.Schema;
using Jurassic;
using AdaptiveCards;

namespace VirtualWorkFriendBot.Dialogs
{
    public class EscalateDialog : VirtualFriendDialog
    {
        private EscalateResponses _responder = new EscalateResponses();

        public EscalateDialog(IServiceProvider botServices, IBotTelemetryClient telemetryClient)
            : base(nameof(EscalateDialog), botServices, telemetryClient)
        {
            InitialDialogId = nameof(EscalateDialog);

            var escalate = new WaterfallStep[]
            {
                // SendPhone,

                /*PromptTherapistGender,
                ValidateTherapistGender,
                PromptTherapistEthnicity,
                ValidateTherapistEthnicity,
                PromptTherapistSpecialty,
                ValidateTherapistSpecialty,*/
                PromptTherapistOnline,
                ValidateTherapistOnline,
                PromptUserLocation,
                ValidateUserLocation,
                PromptTherapistRadius,
                ValidateTherapistRadius,
                PromptDataConsent,
                ValidateDataConsent,
                BeginSearch,
                TherapistSearch
                // ScheduleAppointment,

            };

            AddDialog(new TextPrompt(DialogIds.TherapistOnlinePrompt));
            AddDialog(new TextPrompt(DialogIds.UserLocationPrompt));
            AddDialog(new TextPrompt(DialogIds.TherapistRadiusPrompt));
            AddDialog(new TextPrompt(DialogIds.DataConsentPrompt));
            AddDialog(new TextPrompt(DialogIds.BeginSearch));
            AddDialog(new TextPrompt(DialogIds.NoneFound));
            // AddDialog(new TextPrompt(DialogIds.))
            AddDialog(new WaterfallDialog(InitialDialogId, escalate));
        }

        private async Task<DialogTurnResult> SendPhone(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // await PopulateStateObjects(sc);

            /*await _responder.ReplyWith(sc.Context, OnboardingResponses.ResponseIds.HaveNameMessage,
             new { greeting, name = $"{name} \U0001F600" });

            return await sc.PromptAsync(DialogIds.ReadingInterestsPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, sc.Context.Activity.Locale,
                    OnboardingResponses.ResponseIds.ReadingInterestsPrompt),
            });*/

            await _responder.ReplyWith(sc.Context, EscalateResponses.ResponseIds.SendPhoneMessage);
            return await sc.EndDialogAsync();
        }

        /*private async Task<DialogTurnResult> PromptTherapistGender(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<DialogTurnResult> ValidateTherapistGender(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<DialogTurnResult> PromptTherapistEthnicity(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<DialogTurnResult> ValidateTherapistEthnicity(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }*/

        /*private async Task<DialogTurnResult> PromptTherapistSpecialty(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<DialogTurnResult> ValidateTherapistSpecialty(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }*/

        private async Task<DialogTurnResult> PromptTherapistOnline(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Ask whether they prefer in-person or online therapy sessions
            return await stepContext.PromptAsync(DialogIds.TherapistOnlinePrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale,
                    EscalateResponses.ResponseIds.TherapistOnlinePrompt),
            });
        }

        private async Task<DialogTurnResult> ValidateTherapistOnline(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Save user preference according to their response
            if (Equals((string)stepContext.Result, (string)"online"))
            {
                _onboardingState.TherapistOnline = true;
            }
            else
            {
                _onboardingState.TherapistOnline = false;
            }

            await SaveOnboardingState(stepContext.Context);

            return await stepContext.NextAsync(_onboardingState);
        }

        private async Task<DialogTurnResult> PromptUserLocation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if user location is already known
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Skip if user prefers online therapy
            if (_onboardingState.TherapistOnline)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Ask user for the city to search for therapists
            return await stepContext.PromptAsync(DialogIds.UserLocationPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale,
                    EscalateResponses.ResponseIds.UserLocationPrompt),
            });
        }

        private async Task<DialogTurnResult> ValidateUserLocation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Skip if user prefers online therapy
            if (_onboardingState.TherapistOnline)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            _onboardingState.UserLocation = (string)stepContext.Result;

            await SaveOnboardingState(stepContext.Context);

            return await stepContext.NextAsync(_onboardingState);
        }

        private async Task<DialogTurnResult> PromptTherapistRadius(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Skip if user prefers online therapy
            if (_onboardingState.TherapistOnline)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            return await stepContext.PromptAsync(DialogIds.TherapistRadiusPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale,
                    EscalateResponses.ResponseIds.TherapistRadiusPrompt),
            });
        }

        private async Task<DialogTurnResult> ValidateTherapistRadius(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Skip if user prefers online therapy
            if (_onboardingState.TherapistOnline)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Save radius as integer
            _onboardingState.TherapistRadius = Int32.Parse((string)stepContext.Result);

            await SaveOnboardingState(stepContext.Context);

            return await stepContext.NextAsync(_onboardingState);
        }

        private async Task<DialogTurnResult> PromptDataConsent(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Ask if the user would like us to share their information with the therapist
            return await stepContext.PromptAsync(DialogIds.DataConsentPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale,
                    EscalateResponses.ResponseIds.DataConsentPrompt),
            });
        }

        private async Task<DialogTurnResult> ValidateDataConsent(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await PopulateStateObjects(stepContext);

            // Skip if we already have preferences
            if (_onboardingState.TherapistPreferencesComplete)
            {
                return await stepContext.NextAsync(_onboardingState);
            }

            // Save data sharing preference
            if (Equals((string)stepContext.Result, "yes"))
            {
                _onboardingState.ShareData = true;
            }
            else
            {
                _onboardingState.ShareData = false;
            }

            await SaveOnboardingState(stepContext.Context);

            return await stepContext.NextAsync(_onboardingState);
        }

        private async Task<List<JToken>> FilterTherapists(WaterfallStepContext stepContext)
        {
            await PopulateStateObjects(stepContext);

            const string URL = "https://api.yelp.com/v3/businesses/search?";
            // Figure out syntax here
            //string location = String.Join("=", string["location", (string)_onboardingState.UserLocation]);
            string url = QueryHelpers.AddQueryString(URL, "location", _onboardingState.UserLocation);
            // Have to convert radius to string
            url = QueryHelpers.AddQueryString(url, "radius", _onboardingState.TherapistRadius.ToString());
            //string radius = String.Join("=", string["radius", (string)_onboardingState.TherapistRadius]) ;
            // string urlParameters = "?location=SanDiego&term=food";
            url = QueryHelpers.AddQueryString(url, "limit", "3");

            url = QueryHelpers.AddQueryString(url, "term", "Mental Health");

            url = QueryHelpers.AddQueryString(url, "category", "c_and_mh");

            //string limit = "limit=3";


            // string urlParameters = String.Join("&", string[location, radius, limit]);

            HttpClient client = new HttpClient {};

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add(
                "Authorization", "Bearer TjhCXJXnJBo43hSVc4poqwjXUTLcdRojYmkvhI26pNhu2JFZHecF4HxcGNWsVU_X2vwFQb5MHwrIVCzVFnP5HYROe-5dTXKBKMGLUqxDwKVFuJtMZgGwTBxcAmygXnYx");

            HttpResponseMessage response = client.GetAsync(url).Result;

            List<JToken> output = new List<JToken>();

            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;

                var jsonContent = JObject.Parse(responseContent);

                /*foreach (KeyValuePair<string, JToken> property in jsonContent)
                {
                    if (String.Equals(property.Key, "businesses"){
                        return property.Value;
                    }
                    // Console.WriteLine(property.Key + " - " + property.Value);
                }*/

                output = jsonContent["businesses"].ToList<JToken>();

                // jsonContent.TryGetValue("businesses");

                // List<string> output = jsonContent.businesses;
            }

            client.Dispose();

            return output;
        }

        private async Task<DialogTurnResult> BeginSearch(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                            text: EscalateStrings.THERAPIST_SEARCH,
                            ssml: EscalateStrings.THERAPIST_SEARCH,
                            inputHint: InputHints.IgnoringInput));

            return await stepContext.NextAsync(null, cancellationToken);
           /* {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale,
                    EscalateResponses.ResponseIds.BeginSearch),
            });*/
        }

        private async Task<DialogTurnResult> TherapistSearch(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            List<JToken> therapists = await FilterTherapists(stepContext);

            if (therapists.Capacity == 0)
            {
                return await stepContext.PromptAsync(DialogIds.NoneFound, new PromptOptions()
                {
                    Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale,
                    EscalateResponses.ResponseIds.NoneFound),
                });
            }

            // Fix ToString if it doesn't work
            var templateJson = _templateEngine.GenerateActivityForLocale("TherapistRecommendationCard");

            var templateString = templateJson.Attachments.Single().Content.ToString();

            var cardList = new List<string>();

            foreach (var t in therapists)
            {
                // Convert to string

                var dataJson = t.ToString();

                var transformer = new AdaptiveTransformer();
                var cardJson = transformer.Transform(templateString, dataJson);

                // Create attachment
                cardList.Add(cardJson);
            }

            var carousel = MessageFactory.Carousel(cardList.Select(c => { return jsonToCard(c); }).ToList());

            // Return card
            await stepContext.Context.SendActivityAsync(carousel);

            return await EndDialogAndProcessing(stepContext, cancellationToken);
        }

        private Attachment jsonToCard(string json)
        {
            var result = AdaptiveCard.FromJson(json);

            var attachment = new Attachment(contentType: AdaptiveCard.ContentType,
                content: JObject.FromObject(result.Card));

            return attachment;
        }

        /*new Attachment
	 {
	 ContentType = AdaptiveCard.ContentType,
	  // Convert the AdaptiveCard to a JObject
	 Content = JObject.FromObject(card),*/

        /*private IMessageActivity GetEntertainCard()
        {

            var attachment = new Attachment(contentType: "adaptivecard.",
                name: "Therapist Recommendation",
                thumbnailUrl: "https://www.yogajournal.com/.image/t_share/MTQ2MTgwNzM5MDQ5OTg5NjY0/sunset-meditation-mudra.jpg",
                content: json;

            return MessageFactory.Attachment(attachment);
        }*/

        /*private async Task<DialogTurnResult> ScheduleAppointment(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }*/

        private class DialogIds
        {
            public const string NamePrompt = "namePrompt";
            public const string TherapistOnlinePrompt = "therapistOnlinePrompt";
            public const string UserLocationPrompt = "userLocationPrompt";
            public const string TherapistRadiusPrompt = "therapistRadiusPrompt";
            public const string DataConsentPrompt = "dataConsentPrompt";
            public const string BeginSearch = "beginSearch";
            public const string NoneFound = "noneFound";

        }
    }
}
