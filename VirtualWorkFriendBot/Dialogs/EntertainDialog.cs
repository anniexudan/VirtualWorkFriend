// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using VirtualWorkFriendBot.Services;

namespace VirtualWorkFriendBot.Dialogs
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Helpers;
    using Luis;
    using Microsoft.Bot.Schema;
    using Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class EntertainDialog : ComponentDialog
    {
        private IStatePropertyAccessor<OnboardingState> _accessor;

        private BotServices _services;

        private const string CountRefusal = "CountRefusal";
        private const string CountShowing = "CountShowing";
        private const string ContentIndex = "ContentIndex";

        private const int MaxContentShowing = 4;

        private const int LimitRefusal = 2;

        private const string JokeUrl = "https://icanhazdadjoke.com/";

        private const string YouTubeUrl =
            "https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=25&q={0}&key=AIzaSyAx0tTeO8zRVBsFty9h8fl6CEDGNbZBXnY";

        private const string BingSearchUrl =
            "https://api.cognitive.microsoft.com/bing/v7.0/news/search?oauth_signature_method=HMAC-SHA1&oauth_timestamp=1563897395&oauth_nonce=buJSx9&oauth_version=1.0&oauth_signature=98STFSKGsYW7ApE6o3BtW+KdZEo=&q=architecture&mkt=en-us&q={0}";

        private const string BingAuthorizationHeader = "Ocp-Apim-Subscription-Key";

        private HttpClient _httpClient = new HttpClient();

        public EntertainDialog(
            BotServices botServices, 
            UserState userState)
            : base(nameof(EntertainDialog))
        {
            _services = botServices;

            _accessor = userState.CreateProperty<OnboardingState>(nameof(OnboardingState));

            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            InitialDialogId = nameof(EntertainDialog);

            var steps = new WaterfallStep[]
            {
                SomeTips,
                ProcessConfirmation,
                DidYouLikeIt,
                ProcessConfirmation
   
            };

            AddDialog(new WaterfallDialog(InitialDialogId, steps));
            AddDialog(new TextPrompt(DialogIds.AskForConfirmationPrompt));
            AddDialog(new TextPrompt(DialogIds.EmptyPrompt));
        }

        private async Task<DialogTurnResult> SomeTips(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // entry point, get options from previous steps
            var options = sc.Options as dynamic;
            sc.Values[CountShowing] = options != null && options.showing != null ? options.showing : (long)0;
            sc.Values[CountRefusal] = options != null && options.refusals != null ? options.refusals : (long)0;
            sc.Values[ContentIndex] = options != null && options.contentIndex != null ? options.contentIndex : (long)0;

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(GetEntertainmentMessage(sc))
            };

            return await sc.PromptAsync(DialogIds.AskForConfirmationPrompt, promptOptions);
        }

        private async Task<DialogTurnResult> DidYouLikeIt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await _accessor.GetAsync(sc.Context, () => new OnboardingState());
            var name = state != null && !string.IsNullOrEmpty(state.Name) ? state.Name : "buddy";
            await sc.Context.SendActivityAsync(MessageFactory.Text(string.Format(AskForConfirmation(), name)));
            
            // loop back
            return await sc.ReplaceDialogAsync(nameof(EntertainDialog), GetOptionsFromStepContext(sc));
        }

        private dynamic GetOptionsFromStepContext(WaterfallStepContext wsc)
        {
            return new
            {
                refusals = GetCountRefusal(wsc), showing = GetShowingCount(wsc), contentIndex = GetContentIndex(wsc)
            };
        }

        private long GetContentIndex(WaterfallStepContext wsc)
        {
            return wsc.Values.ContainsKey(ContentIndex) ? (long)wsc.Values[ContentIndex] : 0;
        }

        private void IncrementContentIndex(WaterfallStepContext wsc)
        {
            if (!wsc.Values.ContainsKey(ContentIndex))
            {
                wsc.Values[ContentIndex] = 1;
            }
            else
            {
                var value = (long)wsc.Values[ContentIndex];
                value = value == 2 ? 0 : value + 1;
                wsc.Values[ContentIndex] = value;
            }
        }

        private async Task<DialogTurnResult> ProcessConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var intent = await LuisHelper.GetIntent(_services, sc, cancellationToken);

            if (intent == GeneralLuis.Intent.Confirm)
            {
                // show entertainment
                DecrementedCountRefusal(sc);
                var card = await GetEntertainCard(sc);
                await sc.Context.SendActivityAsync(card, cancellationToken);

                IncrementShowing(sc);
                if (ShowingLimitReached(sc))
                {
                    return await sc.EndDialogAsync();
                }

                return await sc.PromptAsync(DialogIds.EmptyPrompt, new PromptOptions { }, cancellationToken);
            }

            IncrementCountRefusal(sc);
            if (RefusalLimitReached(sc))
            {
                // talk to human
                await sc.EndDialogAsync(cancellationToken: cancellationToken);
                return await sc.BeginDialogAsync(nameof(EscalateDialog));
            }

            IncrementContentIndex(sc);
            // loop back
            return await sc.ReplaceDialogAsync(nameof(EntertainDialog), GetOptionsFromStepContext(sc));
        }

        private long GetShowingCount(WaterfallStepContext wsc)
        {
            return wsc.Values.ContainsKey(CountShowing) ? (long)wsc.Values[CountShowing] : 0;
        }

        private bool ShowingLimitReached(WaterfallStepContext wsc)
        {
            return GetShowingCount(wsc) >= MaxContentShowing;
        }

        private void IncrementShowing(WaterfallStepContext wsc)
        {
            if (!wsc.Values.ContainsKey(CountShowing))
            {
                wsc.Values[CountShowing] = 1;
            }
            else
            {
                var previous = (long)wsc.Values[CountShowing];
                wsc.Values[CountShowing] = previous + 1;
            }
        }

        private async Task<IMessageActivity> GetEntertainCard(WaterfallStepContext wsc)
        {
            var state = await _accessor.GetAsync(wsc.Context, () => new OnboardingState());

            var option = GetContentIndex(wsc);
            IncrementContentIndex(wsc);

            switch (option)
            {
                case 0:
                    {
                        var video = GetYouTubeVideos(state.MusicInterests);
                        var attachment = new Attachment(contentType: "video/mp4",
                            name: video.title,
                            thumbnailUrl: video.thumbnail,
                            contentUrl: $"https://www.youtube.com/watch?v={video.url}");

                        return MessageFactory.Attachment(attachment);
                    }
                case 1:                
                    {
                        var attachment = new HeroCard
                        {
                            Text = GetJoke()
                        }.ToAttachment();

                        return MessageFactory.Attachment(attachment, null);
                    }
                default:
                {
                    var articles = GetNewsArticles(state.ReadingInterests).ToList();

                    var cards = articles.Select(x => new ThumbnailCard
                    {
                        Title = x.title,
                        Buttons = new[] {new CardAction(ActionTypes.OpenUrl, title: "Open", value: x.url)},
                        Text = x.description
                    });

                    return MessageFactory.Carousel(cards.Select(c => c.ToAttachment()).ToList(),
                        $"I think you might like these articles based on your preferences \U0001F600");
                }
            }
        }

        private string GetJoke()
        {            
            var reply = _httpClient.GetStringAsync(JokeUrl).Result;
            var joke = JsonConvert.DeserializeObject<dynamic>(reply);
            return joke.joke;
        }

        private dynamic GetYouTubeVideos(List<string> interests)
        {
            var q = interests != null && interests.Count > 0
                ? interests[RandomHelper.GetRandom(0, interests.Count)]
                : "jazz+music";
            var result = _httpClient.GetStringAsync(string.Format(YouTubeUrl, q)).Result;
            JObject json = JObject.Parse(result);
            var itemsJson = json["items"].Value<JArray>();
            var items = itemsJson.ToObject<List<JObject>>();
            var itemJson = items[RandomHelper.GetRandom(0, items.Count)];
            var item = JsonConvert.DeserializeObject<dynamic>(itemJson.ToString());
            string title = item.snippet.title;
            string thumbnail = item.snippet.thumbnails["default"].url;
            string url = item.id.videoId;
            return new {title, thumbnail, url};
        }

        private IEnumerable<dynamic> GetNewsArticles(List<string> interests)
        {
            var q = interests != null && interests.Count > 0
                ? interests[RandomHelper.GetRandom(0, interests.Count)]
                : "hackathon+redmond";
            q = q.Replace(' ', '+');
            try
            {
                _httpClient.DefaultRequestHeaders.Add(BingAuthorizationHeader, "e990e767975144a2a0d37dda7906a382");
                var result = _httpClient.GetStringAsync(string.Format(BingSearchUrl, q)).Result;
                JObject json = JObject.Parse(result);
                var itemsJson = json["value"].Value<JArray>();
                var items = itemsJson.ToObject<List<JObject>>();
                var selected = RandomHelper.GetRandomSubset(items, 3);

                return selected.Select(itemJson =>
                {
                    var item = JsonConvert.DeserializeObject<dynamic>(itemJson.ToString());
                    string title = item.name;
                    string url = item.url;
                    string description = item.description;
                    return new {title, url, description};
                });
            }
            finally
            {
                _httpClient.DefaultRequestHeaders.Remove(BingAuthorizationHeader);
            }
        }

        private string GetEntertainmentMessage(WaterfallStepContext wsc)
        {
            var option = GetContentIndex(wsc);
            switch (option)
            {
                case 0:
                    return "Would you like to listen to some music? \U0001F3BC";
                case 1:
                    return "Would you like to hear a joke? \U0001F609";
                default:
                    return "Would you like to read an article?";
            }
        }

        private static string AskForConfirmation()
        {
            var options = new[] {"Did you enjoy it {0}?", "Did you like it {0}?"};
            return options[RandomHelper.GetRandom(0, options.Length)];
        }

        private static long GetCountRefusal(WaterfallStepContext wsc)
        {
            return wsc.Values.ContainsKey(CountRefusal) ? (long)wsc.Values[CountRefusal] : 0;
        }

        private static void IncrementCountRefusal(WaterfallStepContext wsc)
        {
            if (!wsc.Values.ContainsKey(CountRefusal))
            {
                wsc.Values[CountRefusal] = 1;
            }
            else
            {
                var previous = (long) wsc.Values[CountRefusal];
                wsc.Values[CountRefusal] = previous + 1;
            }
        }

        private static void DecrementedCountRefusal(WaterfallStepContext wsc)
        {
            if (wsc.Values.ContainsKey(CountRefusal))
            {
                var previous = (long)wsc.Values[CountRefusal];
                if (previous > 0)
                {
                    wsc.Values[CountRefusal] = previous - 1;
                }
            }
        }

        private static bool RefusalLimitReached(WaterfallStepContext wsc)
        {
            return GetCountRefusal(wsc) >= LimitRefusal;
        }

        private class DialogIds
        {
            public const string AskForConfirmationPrompt = "confirmationPrompt";
            public const string EmptyPrompt = "emptyPrompt";
        }
    }
}
