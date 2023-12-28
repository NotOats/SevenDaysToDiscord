using Newtonsoft.Json.Linq;
using SevenDaysToDiscord.Settings;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SevenDaysToDiscord
{
    internal interface IDiscordSettings
    {
        string WebHookUrl { get; }
        ulong ThreadId { get; }
    }

    internal class WebhookClient<T> where T : class, IDiscordSettings
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ISettings<T> _settings;

        public WebhookClient(ISettings<T> settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            var userAgent = _httpClient.DefaultRequestHeaders.UserAgent;
            userAgent.Clear();
            userAgent.ParseAdd("DiscordBot (https://github.com/NotOats/SevenDaysToDiscord, 1.0)");
        }

        public async Task<ulong?> SendMessage(string escapedJson)
        {
            var uri = CreateUri();
            var contents = new StringContent(escapedJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uri, contents);

#if DEBUG
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();

                Log.Error($"SevenDaysToDiscord: Error creating webhook message - Code: {response.StatusCode}, Request: {escapedJson}");
                Log.Error($"SevenDaysToDiscord: Response: {msg}");
            }
#endif

            // Ok, result with wait = false
            if (response.StatusCode == HttpStatusCode.NoContent)
                return 0;

            // Ok, read Id from message
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var rawMessage = await response.Content.ReadAsStringAsync();
                var message = JObject.Parse(rawMessage);
                
                var rawId = message["id"];
                var id = rawId.ToObject<ulong>();

                return id;
            }

            // Error
            return null;
        }

        public async Task<bool> UpdateMessage(ulong messageId, string escapedJson)
        {
            var uri = CreateUri($"/messages/{messageId}");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), uri)
            {
                Content = new StringContent(escapedJson, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);

#if DEBUG
            // This should only return Ok with message content
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var msg = await response.Content.ReadAsStringAsync();

                Log.Error($"SevenDaysToDiscord: Error updating webhook message - Code: {response.StatusCode}, Request: {escapedJson}");
                Log.Error($"SevenDaysToDiscord: Response: {msg}");
            }
#endif

            return response.StatusCode == HttpStatusCode.OK;
        }

        private string CreateUri(string urlModifier = null)
        {
            var queryParameters = new Dictionary<string, string>
            {
                // Force wait, maybe configurable later? Only used on SingleUpdateMessage for message tracking.
                { "wait", "true" }
            };

            // Add Thread Id
            if (_settings.Value.ThreadId != 0)
                queryParameters.Add("thread_id", _settings.Value.ThreadId.ToString());

            var url = _settings.Value.WebHookUrl.TrimEnd('/');

            if (urlModifier != null)
                url += urlModifier;

            return AddQueryString(url, queryParameters);
        }

        private static string AddQueryString(string url, IEnumerable<KeyValuePair<string, string>> queryString)
        {
            var queryIndex = url.IndexOf('?');
            var hasQuery = queryIndex != -1;

            var sb = new StringBuilder();
            sb.Append(url);

            foreach (var parameter in queryString)
            {
                sb.Append(hasQuery ? '&' : '?');
                sb.Append(WebUtility.UrlEncode(parameter.Key));
                sb.Append('=');
                sb.Append(WebUtility.UrlEncode(parameter.Value));
                hasQuery = true;
            }

            return sb.ToString();
        }
    }
}
