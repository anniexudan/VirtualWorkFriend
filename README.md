# VirtualWorkFriend
A Microsoft Azure based bot application helping people in workforce to reduce stress.
[![BotImage](https://virtualworkfriendbotz7sw.blob.core.windows.net/images/Bot.png)](https://virtualworkfriend.azurewebsites.net)


# Project Background
There are many benefits of talking to someone because you can:

1. Sort through your feelings

2. Put things in perspective

3. Release tension.

https://ie.reachout.com/getting-help-2/face-to-face-help/things-you-need-to-know/benefits-of-talking-to-someone/

In modern work life, however, you may work remotely or work at office needing to take to someone available whenever emotions arise.

Virtual Work Friend is the a AI based product which can:

1. Build conversation with you like your work friend and even better it is a friend who is outside of your situation so that you don't have to worry about it being judgmental.
    
2. Construct conversation through IM, voice, and VR talking image.

3. Access your stress level and according it provide suitable customized stress handling methods.

4. Escalate you issue to an expert by leveraging our recommending system.

5. Text Analytics included Journaling capability

In this repository, you will find the architectual diagram, the bot conversation composer files for conversation flow design and the codebase of this solution. 

# Full Demo: 

https://www.youtube.com/watch?v=hgz7K2L-W1c&t

# Feature Demos:
### 1. Login 
The login in process included the privacy and term of use consent. It uses OAUTH for user information. 

![Bot Gif](/GIF/Login.gif)

### 2. Onboarding Process
After log in it will trigger onboarding process if the use is first time login to the application. It will record user's preference on reading and music topics. User can later update their perferences as needed.

![Bot Gif](/GIF/Onboarding.gif)

### 3. Conversation flow
The daily conversation flow leveraged the stress scaling process. It has low, medium and high stress level status. And according to the stress level it will trigger different stress handling process including meditation, talk to the bot, entertainment tips for music, jokes or article reading, and back up with knowledge or chitchat.

![Bot Gif](/GIF/DailyConversation.gif)

### 4. Journaling Capability
If user choose Journaling option, it will create an new OneNote page for user to write down their thoughts. The application can do text analytics and send the overall sentiment and high level statistics back for user to have a high level understanding of their overall sentiment change over the time. 

![Bot Gif](/GIF/Journaling.gif)

### 5. Escalation Process
By triggering "Talk to a person" process, the bot will send the recommended list of professionals in the user's state and preferred distance. It will provide theorpists name, address, rating, etc. from Yelp.

![Bot Gif](/GIF/Escalation.gif)

### 6. Sentiment Analysis Dashboard
User can go to their Power BI tenant and view their sentiment trend based on the sentiment summary on their journal

![Bot Gif](/GIF/SentimentDashboard.gif)

# Resources used:
Azure Virtual Assistant: https://microsoft.github.io/botframework-solutions/overview/virtual-assistant-solution/
Bot Conversation Composer:https://docs.microsoft.com/en-us/composer/introduction
