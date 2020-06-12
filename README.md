# VirtualWorkFriend
Virtual Work Friend is a Microsoft Azure-based bot application helping people in the workforce to reduce stress.
[![BotImage](https://virtualworkfriendbotz7sw.blob.core.windows.net/images/Bot.png)](https://virtualworkfriend.azurewebsites.net)

# Project Background
There are many benefits of talking to someone because you can:

1. Sort through your feelings

2. Put things in perspective

3. Release tension

https://ie.reachout.com/getting-help-2/face-to-face-help/things-you-need-to-know/benefits-of-talking-to-someone/

In modern work life, whether you may work remotely or work at an office needing to talk to someone available whenever emotions arise.

Virtual Work Friend is an AI-based product which can:

1. Build a conversation with you like your work friend, and even better, it is a friend who is outside of your situation so that you don't have to worry about it being judgmental.
    
2. Construct conversation through IM, Voice, and VR talking image.

3. Access your stress level and provide suitable customized stress handling methods accordingly.

4. Escalate your issue to an expert by leveraging our recommending system.

5. Text Analytics included Journaling capability

In this repository, you will find the architectural diagram, the bot conversation composer files for conversation flow design, and the codebase of this solution. 

# Feature Demos:
### 1. Login 
The login process included the privacy and term of use consent. It uses OAUTH for user information. 

![Bot Gif](/GIF/Login.gif)

### 2. Onboarding Process
After login, it will trigger the onboarding process if the user is the first-time login to the application. It will record the user's preference for reading and music topics. User can later update their preferences as needed.

![Bot Gif](/GIF/Onboarding.gif)

### 3. Conversation flow
The daily conversation flow leveraged the stress scaling process. It has low, medium, and high-stress level status. And according to the stress level, it will trigger different stress handling processes including meditation, talk to the bot, entertainment tips for music, jokes or article reading, and back up with knowledge or chitchat.

![Bot Gif](/GIF/DailyConversation.gif)

### 4. Journaling Capability
If the user choose Journaling option, it will create a new OneNote page for the user to write down their thoughts. The application can do text analytics and send the overall sentiment and high-level statistics back for users to have a high-level understanding of their overall sentiment change over time. 

![Bot Gif](/GIF/Journaling.gif)

### 5. Escalation Process
By triggering the "Talk to a person" process, the bot will send the recommended list of professionals in the user's state and preferred distance. It will provide the therapist's name, address, rating, etc. from Yelp.

![Bot Gif](/GIF/Escalation.gif)

### 6. Sentiment Analysis Dashboard
Users can go to their Power BI tenant and view their sentiment trends based on the sentiment summary in their journal.


![Bot Gif](/GIF/SentimentDashboard.gif)

# Resources used:
Azure Virtual Assistant: https://microsoft.github.io/botframework-solutions/overview/virtual-assistant-solution/
Bot Conversation Composer:https://docs.microsoft.com/en-us/composer/introduction
