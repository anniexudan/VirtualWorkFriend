// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Protocol.StreamingExtensions.NetCore;

namespace MyAssistant_1.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly WebSocketEnabledHttpAdapter _webSocketEnabledHttpAdapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter httpAdapter, WebSocketEnabledHttpAdapter webSocketEnabledHttpAdapter, IBot bot)
        {
            _adapter = httpAdapter;
            _webSocketEnabledHttpAdapter = webSocketEnabledHttpAdapter;
            _bot = bot;
        }

        //updateing according to https://docs.microsoft.com/en-us/azure/bot-service/directline-speech-bot?view=azure-bot-service-4.0
        [HttpPost, HttpGet]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _adapter.ProcessAsync(Request, Response, _bot);
        }

        [HttpGet]
        public async Task StartWebSocketAsync()
        {
            // Delegate the processing of the Websocket Get request to the adapter.
            // The adapter will invoke the bot.
            await _webSocketEnabledHttpAdapter.ProcessAsync(Request, Response, _bot);
        }
    }
}