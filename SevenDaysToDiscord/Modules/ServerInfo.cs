using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SevenDaysToDiscord.Hosting;
using SevenDaysToDiscord.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Modules
{
    internal class ServerInfoSettings : IDiscordSettings
    {
        public static string SectionName = "ServerInfo";

        public bool Enabled { get; set; } = false;

        public uint UpdateInterval { get; set; } = 60;

        public bool DisplayPlayerList { get; set; } = true;
        public bool DisplayServerSlots { get; set; } = true;
        public bool DisplayServerTime { get; set; } = true;
        public bool DisplayNextBloodMoon { get; set; } = true;

        public string EmbedTitle { get; set; }

        public string WebHookUrl { get; set; }
        public ulong ThreadId { get; set; } = 0;
        public ulong MessageId { get; set; } = 0;
    }

    internal class ServerInfo : BackgroundModule
    {
        private readonly ISettings<ServerInfoSettings> _settings;
        private readonly WebhookClient<ServerInfoSettings> _webhookClient;
        public ServerInfo(ISettings<ServerInfoSettings> settings, WebhookClient<ServerInfoSettings> webhookClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _webhookClient = webhookClient ?? throw new ArgumentNullException(nameof(webhookClient));
        }


        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Check EmbedTitle, this can only be done once the game server has fully started
            if (string.IsNullOrWhiteSpace(_settings.Value.EmbedTitle))
            {
                var serverName = GamePrefs.GetString(EnumGamePrefs.ServerName);

                _settings.Value.EmbedTitle = serverName;
                _settings.Save();
            }

            var interval = TimeSpan.FromSeconds(_settings.Value.UpdateInterval);

            Log.Out($"SevenDaysToDiscord: ServerInfo ({interval.TotalSeconds}s interval) - {(_settings.Value.Enabled ? "Enabled" : "Disabled")}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_settings.Value.Enabled)
                        await UpdateServerInfo();

                    await Task.Delay(interval, cancellationToken);
                }
            }
            finally
            {
                _settings.Save();
            }
        }

        private async Task UpdateServerInfo()
        {
            var embed = new JObject(
                new JProperty("type", "rich"),
                new JProperty("title", _settings.Value.EmbedTitle),
                new JProperty("fields", new JArray()));

            AddPlayerList(embed);

            var fields = embed["fields"] as JArray;
            AddSlotsField(fields);
            AddDateTimeField(fields);
            AddBloodMoonField(fields);
            AddLastUpdatedField(fields);

            var message = new JObject(
                new JProperty("content", ""),
                new JProperty("embeds", new JArray(embed)));

            await SendOrUpdateMessage(message.ToString(Formatting.None));
        }

        private async Task<bool> SendOrUpdateMessage(string jsonPayload)
        {
            // New Message
            var messageId = _settings.Value.MessageId;
            if (messageId == 0)
            {
                var responseId = await _webhookClient.SendMessage(jsonPayload);
                if (responseId == null | responseId.Value == 0)
                {
                    Log.Error("SevenDaysToDiscord: Failed to create server info message.");
                    return false;
                }

                // Save for future use
                _settings.Value.MessageId = responseId.Value;
                _settings.Save();

                return true;
            }

            if (await _webhookClient.UpdateMessage(messageId, jsonPayload))
                return true;

            Log.Error($"SevenDaysToDiscord: Failed to update server info message, id: {messageId}");
            return false;
        }

        private void AddPlayerList(JObject embed)
        {
            if (!_settings.Value.DisplayPlayerList)
                return;

            var sb = new StringBuilder();
            sb.Append("**Player List**");

            var players = GameManager.Instance.World.Players.list;
            foreach (var player in players)
            {
                sb.Append($"\n- {player.EntityName}");
            }

            embed.Add(new JProperty("description", sb.ToString()));
        }

        private void AddSlotsField(JArray fields)
        {
            if (!_settings.Value.DisplayServerSlots)
                return;

            var current = GameManager.Instance.World.Players.Count;
            var max = GamePrefs.GetInt(EnumGamePrefs.ServerMaxPlayerCount);

            fields.Add(CreateField("Slots", $"{current}/{max}"));
        }

        private void AddDateTimeField(JArray fields)
        {
            if (!_settings.Value.DisplayServerTime)
                return;

            var (days, hours, minutes) = GameUtils.WorldTimeToElements(GameManager.Instance.World.GetWorldTime());

            fields.Add(CreateField("Server Time", $"Day {days} - {hours}:{minutes}"));
        }

        private void AddBloodMoonField(JArray fields)
        {
            if (!_settings.Value.DisplayNextBloodMoon)
                return;

            var bloodMoonDay = GameApi.BloodMoonDay;
            var daysUntilBloodMoon = GameApi.DaysUntilBloodMoon(GameApi.BloodMoonDay);

            var realTimeUntilBloodMoon = TimeSpan.FromMinutes(daysUntilBloodMoon * GameApi.DayNightLength);
            var timestamp = new DateTimeOffset(DateTime.Now + realTimeUntilBloodMoon).ToUnixTimeSeconds();

            fields.Add(CreateField($"Next Blood Moon (Day {bloodMoonDay})", $"<t:{timestamp}:f>"));
        }

        private void AddLastUpdatedField(JArray fields)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            fields.Add(CreateField("\u200B", $"Last Updated: <t:{timestamp}:f>", false));
        }

        private JObject CreateField(string name, string value, bool inline = true)
        {
            return new JObject(
                        new JProperty("name", name),
                        new JProperty("value", value),
                        new JProperty("inline", inline));
        }
    }
}
