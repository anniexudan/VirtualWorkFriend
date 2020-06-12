"use strict";
class MinimizableWebChat {
    Minimize = null;
    Maximize = null;
    ChatFrame = null;
    Root = null;
    WebChat = null;
    Connected = false;
    Authenticated = false;

    #HtmlTemplate = `<div class="minimizable-web-chat">    
    <button class="maximize">
        <img alt="beanie icon" style="margin-top: -30px;margin-left: -10px;"
            src="https://virtualworkfriendbotz7sw.blob.core.windows.net/images/BotIcon.png" width="50" height="50">
    </button>
    <div class="chat-box right hide" style="background-color:green;">
        <header>
            <div class="filler"></div>
            <button class="minimize"><span class="ms-Icon ms-Icon--ChromeMinimize"></span></button>
        </header>
        <div class="webchat" style="height:95%;"></div>
    </div>
</div>`;
    DefaultStyleOptions = {
        botAvatarImage: 'https://virtualworkfriendbotz7sw.blob.core.windows.net/images/BotInitials.png',
        botAvatarInitials: 'B',
        userAvatarInitials: 'You',
    };
    StyleOptions = null;
    constructor(rootDivId, authenticate, autoConnect, styleOptions) {
        this.Root = $('#' + rootDivId);
        this.Initialize();
        this.StyleOptions = (styleOptions == undefined) ? this.DefaultStyleOptions : styleOptions;
        var callConnect = true;
        if (autoConnect != undefined) {
            callConnect = Boolean(autoConnect);
        }
        if (authenticate != undefined) {
            this.Authenticated = Boolean(authenticate);
        }
        if (callConnect) {
            this.Connect();
        }
    }
    Initialize() {
        this.Root.html(this.#HtmlTemplate);
        this.Maximize = $(this.Root).find("button.maximize");
        this.Minimize = $(this.Root).find("button.minimize");
        this.ChatFrame = $(this.Root).find("div.chat-box");
        this.WebChat = $(this.Root).find("div.webchat");
        this.Maximize.click(e => { this.ShowChat(); });
        this.Minimize.click(e => { this.HideChat(); });
    }
    ShowChat() {
        this.Maximize.addClass("hide");
        this.ChatFrame.removeClass("hide");
        if (!this.Connected) {
            this.Connect();
        }
        this.WebChat.find("input").focus();
    }
    HideChat() {
        this.ChatFrame.addClass("hide");
        this.Maximize.removeClass("hide");
    }
    Connect() {
        if (this.Connected) {
            return;
        }
        var chatControl = this;

        var tokenUrl = "/api/tokens";
        if (this.Authenticated) {
            tokenUrl += "/authenticate";
        }
        $.post(tokenUrl, function (config) {
            // We are using a customized store to add hooks to connect event
            const botStore = window.WebChat.createStore({}, ({ dispatch }) => next => action => {
                if (action.type === 'DIRECT_LINE/CONNECT_FULFILLED') {
                    // When we receive DIRECT_LINE/CONNECT_FULFILLED action, we will send an event activity using WEB_CHAT/SEND_EVENT
                    dispatch({
                        type: 'WEB_CHAT/SEND_EVENT',
                        payload: {
                            name: 'webchat/join',
                            value: { language: window.navigator.language }
                        }
                    });
                }

                return next(action);
            });

            window.WebChat.renderWebChat(
                {
                    directLine: window.WebChat.createDirectLine({
                        token: ((config.Token != undefined) ? config.Token : config.token)
                    }),
                    userID: (config.UserId != undefined) ? config.UserId : config.userId,
                    locale: 'en-US',
                    styleOptions: chatControl.StyleOptions,
                    store: botStore
                },
                chatControl.WebChat[0]);
            chatControl.Connected = true;
        });
    }
}
