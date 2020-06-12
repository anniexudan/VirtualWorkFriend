// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;

namespace VirtualWorkFriendBot.Responses.Onboarding
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
                {
                    ResponseIds.PrivacyAcceptedPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.PRIVACY_ACCEPTED_PROMPT,
                            ssml: OnboardingStrings.PRIVACY_ACCEPTED_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.TOUAcceptedPrompt,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.TOU_ACCEPTED_PROMPT,
                            ssml: OnboardingStrings.TOU_ACCEPTED_PROMPT,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.DisplayReadingInterests,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.DISPLAY_READING_INTERESTS, data.readingInterests),
                            ssml: string.Format(OnboardingStrings.DISPLAY_READING_INTERESTS, data.readingInterests),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.UpdateReadingInterests,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.UPDATE_READING_INTERESTS,
                            ssml: OnboardingStrings.UPDATE_READING_INTERESTS,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.DisplayMusicInterests,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.DISPLAY_MUSIC_INTERESTS, data.musicInterests),
                            ssml: string.Format(OnboardingStrings.DISPLAY_MUSIC_INTERESTS, data.musicInterests),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.UpdateMusicInterests,
                    (context, data) =>
                        MessageFactory.Text(
                            text: OnboardingStrings.UPDATE_MUSIC_INTERESTS,
                            ssml: OnboardingStrings.UPDATE_MUSIC_INTERESTS,
                            inputHint: InputHints.ExpectingInput)
                },
                {
                    ResponseIds.InformationUpdated,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.INFO_UPDATED),
                            ssml: string.Format(OnboardingStrings.INFO_UPDATED),
                            inputHint: InputHints.IgnoringInput)
                },
                {
                    ResponseIds.UpdateName,
                    (context, data) =>
                        MessageFactory.Text(
                            text: string.Format(OnboardingStrings.UPDATE_NAME, data.name),
                            ssml: string.Format(OnboardingStrings.UPDATE_NAME, data.name),
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
            public const string PrivacyAcceptedPrompt = "privacyAcceptedPrompt";
            public const string TOUAcceptedPrompt = "touAcceptedPrompt";
            public const string DisplayReadingInterests = "displayReadingInterests";
            public const string DisplayMusicInterests = "displayMusicInterests";
            public const string UpdateReadingInterests = "updateReadingInterests";
            public const string UpdateMusicInterests = "updateMusicInterests";
            public const string InformationUpdated = "informationUpdated";
            public const string UpdateName = "updateName";

        }
    }
}
