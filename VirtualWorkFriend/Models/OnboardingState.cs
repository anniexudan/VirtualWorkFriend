// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MyAssistant_1.Models
{
    using System.Collections.Generic;

    public class OnboardingState
    {
        public OnboardingState()
        {
            ReadingInterests = new List<string>();
            MusicInterests = new List<string>();
        }

        public string Name { get; set; }

        public List<string> ReadingInterests { get; set; }

        public List<string> MusicInterests { get; set; }
        
        public string Location { get; set; }
    }
}
