// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace VirtualWorkFriendBot.Models
{
    using Microsoft.Bot.Builder;
    using System.Collections.Generic;

    public class OnboardingState : VirtualFriendBotState<UserState>
    {
        public OnboardingState()
        {
            ReadingInterests = new List<string>();
            MusicInterests = new List<string>();
            NewUser = true;
            Journal = new JournalInformation();
            TherapistPreferencesComplete = false;
        }

        public string Name { get; set; }

        public List<string> ReadingInterests { get; set; }

        public List<string> MusicInterests { get; set; }
        
        public string Location { get; set; }

        public bool PrivacyAccepted { get; set; }
        public bool TermsAccepted { get; set; }
        public int ExpirationPeriod { get; set; }
        public string SignedInUserId { get; set; }
        public bool NewUser { get; set; }

        // Journal Settings
        public JournalInformation Journal { get; set; }
        public bool TherapistOnline { get; set; }
        public bool TherapistPreferencesComplete { get; set; }
        public string UserLocation { get; set; }
        public int TherapistRadius { get; set; }
        public bool ShareData { get; set; }
    }
}
