﻿using SevenDaysToDiscord.Settings;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SevenDaysToDiscord
{
    internal class DiscordSettings
    {
        public static string SectionName = "Discord";

        public string WebHookUrl { get; set; }
    }

    internal class WebhookClient
    {
        private readonly string _webhookUrl;
        private readonly HttpClient _httpClient = new HttpClient();

        public WebhookClient(ISettings<DiscordSettings> settings)
        {
            _webhookUrl = settings.Value?.WebHookUrl ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<bool> SendMessage(string escapedJson)
        {
            var contents = new StringContent(escapedJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_webhookUrl, contents);

#if DEBUG
            if(!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();

                Log.Error($"SevenDaysToDiscord: Error sending webhook, {response.StatusCode}");
                Log.Error($"SevenDaysToDiscord: {msg}");
            }
#endif

            return response.IsSuccessStatusCode;
        }
    }
}
