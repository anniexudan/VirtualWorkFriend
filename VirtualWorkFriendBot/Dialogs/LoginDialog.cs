using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VirtualWorkFriendBot.Models;
using VirtualWorkFriendBot.Helpers;
using Microsoft.Graph;
using System.Security.Cryptography;
using System.Text;

namespace VirtualWorkFriendBot.Dialogs
{
    public class LoginDialog : ComponentDialog
    {
        private IServiceProvider _serviceProvider;
        private OAuthPrompt _oauthPrompt;

        public LoginDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(LoginDialog))
        {
            _serviceProvider = serviceProvider;

            _oauthPrompt = serviceProvider.GetService<OAuthPrompt>();
            AddDialog(_oauthPrompt);

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync
             }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {

                OnboardingState onboardingState = null;
                var virtualFriendState = await StateHelper
                    .RetrieveFromStateAsync<ConversationState, DiscussionState>(_serviceProvider, stepContext.Context);
                virtualFriendState.UserToken = tokenResponse;


 
                try
                {
                    var gh = new GraphHelper(tokenResponse.Token);
                    var currentUser = await gh.GetMeAsync();
                    virtualFriendState.SignedInUserId = GenerateAuthenticatedUserId(currentUser);
                    virtualFriendState.UpdateActivityFromId(stepContext.Context);

                    onboardingState = await StateHelper
                        .RetrieveFromStateAsync<UserState, OnboardingState>(_serviceProvider, stepContext.Context, true);
                    if (String.IsNullOrEmpty(onboardingState.Name))
                    {
                        onboardingState.Name = currentUser.GivenName;
                    }
                    onboardingState.SignedInUserId = virtualFriendState.SignedInUserId;

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(
                        $"Hi {onboardingState.Name}! You are now logged in."), cancellationToken);
                }
                catch (Exception ex)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("You are now logged in."), cancellationToken);
                }

                await StateHelper.PersistInStateAsync<UserState, OnboardingState>(
                        _serviceProvider, stepContext.Context, onboardingState, true);
                await StateHelper.PersistInStateAsync<ConversationState, DiscussionState>(
                    _serviceProvider, stepContext.Context, virtualFriendState);
                return await stepContext.EndDialogAsync(onboardingState);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
            return await stepContext.EndDialogAsync();
        }

        private string GenerateAuthenticatedUserId(User currentUser)
        {
            try { 
                using (SHA256 crypto = SHA256.Create())
                {
                    var input = String.Join("", currentUser.UserPrincipalName.ToLower().Reverse());
                    byte[] hash = crypto.ComputeHash(Encoding.Default.GetBytes(input));
                    for (int i = 0; i < 16; i++)
                    {
                        hash[i] = (byte)(hash[i] ^ hash[i + 16]);
                    }
                    Guid result = new Guid(hash.Take(16).ToArray());
                    return result.ToString().ToLower();
                }
            }
            catch(Exception ex)
            {

            }
            return Guid.NewGuid().ToString().ToLower();

        }
 }
}
