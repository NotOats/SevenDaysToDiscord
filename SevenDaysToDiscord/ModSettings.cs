using System;

namespace SevenDaysToDiscord
{
    internal class ModSettings
    {
        public uint BloodMoonCheckDelaySeconds { get; set; } = 30;

        public uint BloodMoonNotificationSeconds { get; set; } = 3600; // Default to 1 hours before BM

        public string DiscordWebHook { get; set; }
    }
}
