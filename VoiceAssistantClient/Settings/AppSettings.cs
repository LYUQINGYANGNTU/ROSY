// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VoiceAssistantClient.Settings
{
    using Jot.DefaultInitializer;

    public class AppSettings
    {
        public AppSettings()
        {
            this.DisplaySettings = new DisplaySettings();
            this.RuntimeSettings = new RuntimeSettings();
        }

        [Trackable]
        public DisplaySettings DisplaySettings { get; set; }

        [Trackable]
        public RuntimeSettings RuntimeSettings { get; set; }
    }
}
