namespace VirtualWorkFriendBot.Helpers
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Luis;
    using Microsoft.Bot.Builder.Dialogs;
    using Services;

    public static class LuisHelper
    {
        public static async Task<GeneralLuis.Intent> GetIntent(BotServices services, DialogContext dc, CancellationToken cancellationToken)
        {
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cognitiveModels = services.CognitiveModelSets[locale];

            // check luis intent
            cognitiveModels.LuisServices.TryGetValue("General", out var luisService);
            if (luisService == null)
            {
                throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
            }

            var luisResult = await luisService.RecognizeAsync<GeneralLuis>(dc.Context, cancellationToken);
            return luisResult.TopIntent().intent;
        }
    }
}
