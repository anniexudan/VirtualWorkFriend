// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace MyAssistant_1.Responses.Onboarding
{
    public class OnboardingResponses : TemplateManager
    {
        private static LanguageTemplateDictionary _responseTemplates = new LanguageTemplateDictionary
        {
            ["default"] = new TemplateIdMap
            {
                {
                    ResponseIds.EmailPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.EMAIL_PROMPT,
                            ssml: OnboardingStrings.EMAIL_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.HaveEmailMessage,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.HAVE_EMAIL, data.email),
                            ssml: string.Format(OnboardingStrings.HAVE_EMAIL, data.email),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.HaveLocationMessage,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.HAVE_LOCATION, data.Name, data.Location),
                            ssml: string.Format(OnboardingStrings.HAVE_LOCATION, data.Name, data.Location),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.HaveNameMessage,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.HAVE_NAME, data.greeting, data.name),
                            ssml: string.Format(OnboardingStrings.HAVE_NAME, data.greeting, data.name),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.HaveReadingInterests,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.HAVE_READING_INTERESTS, data.name, data.robots,
                                data.interest),
                            ssml: string.Format(OnboardingStrings.HAVE_READING_INTERESTS, data.name, data.robots,
                                data.interest),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.HaveMusicInterests,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.HAVE_MUSIC_INTERESTS, data.groovy),
                            ssml: string.Format(OnboardingStrings.HAVE_MUSIC_INTERESTS, data.groovy),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.NamePrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.NAME_PROMPT,
                            ssml: OnboardingStrings.NAME_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.LocationPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.LOCATION_PROMPT,
                            ssml: OnboardingStrings.LOCATION_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.ReadingInterestsPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.READING_INTERESTS_PROMPT,
                            ssml: OnboardingStrings.READING_INTERESTS_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.MusicInterestsPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.MUSIC_INTERESTS_PROMPT,
                            ssml: OnboardingStrings.MUSIC_INTERESTS_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
            }
        };

        public OnboardingResponses()
        {
            Register(new DictionaryRenderer(_responseTemplates));
        }

        public class ResponseIds
        {
            public const string EmailPrompt = "emailPrompt";
            public const string HaveEmailMessage = "haveEmail";
            public const string HaveNameMessage = "haveName";
            public const string HaveReadingInterests = "haveReadingInterests";
            public const string HaveMusicInterests = "haveMusicInterests";
            public const string HaveLocationMessage = "haveLocation";
            public const string LocationPrompt = "locationPrompt";
            public const string NamePrompt = "namePrompt";
            public const string ReadingInterestsPrompt = "readingInterestsPrompt";
            public const string MusicInterestsPrompt = "musicInterestsPrompt";
        }
    }
}
