using SevenDaysToDiscord.Hosting;
using SevenDaysToDiscord.Settings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Modules
{
    internal class BloodMoonNotifierSettings : IDiscordSettings
    {
        public static string SectionName = "BloodMoonNotifier";

        public bool Enabled { get; set; } = false;

        public uint BloodMoonCheckDelaySeconds { get; set; } = 30;

        public uint BloodMoonNotificationSeconds { get; set; } = 3600; // Default to 1 hours before BM

        public string WebHookUrl { get; set; }
        public ulong ThreadId { get; set; } = 0;
    }

    internal class BloodMoonNotifier : BackgroundModule
    {
        private readonly ISettings<BloodMoonNotifierSettings> _settings;
        private readonly WebhookClient<BloodMoonNotifierSettings> _webhookClient;

        private readonly float _noticeDays;

        private float _lastNoticeTime = 0;
        private int _lastNoticeBloodMoonDay = 0;

        public BloodMoonNotifier(ISettings<BloodMoonNotifierSettings> settings, WebhookClient<BloodMoonNotifierSettings> webhookClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _webhookClient = webhookClient ?? throw new ArgumentNullException(nameof(webhookClient));

            _noticeDays = (float)(_settings.Value.BloodMoonNotificationSeconds / 60) / GameApi.DayNightLength;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(_settings.Value.BloodMoonCheckDelaySeconds);

            Log.Out($"SevenDaysToDiscord: Blood Moon Notifier ({interval.TotalSeconds}s interval, {_noticeDays} days notice) - {(_settings.Value.Enabled ? "Enabled" : "Disabled")}");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_settings.Value.Enabled)
                    await CheckForBloodMoon();

                await Task.Delay(interval, cancellationToken);
            }
        }

        private async Task CheckForBloodMoon()
        {
            // Skip until next blood moon
            if (GameApi.IsBloodMoonActive)
                return;

            var bloodMoonDay = GameApi.BloodMoonDay;
            var daysUntilBloodMoon = GameApi.DaysUntilBloodMoon(bloodMoonDay);
            var worldTimeDays = GameApi.WorldTimeTotalDays;

            // Detect time rollbacks
            if (worldTimeDays < _lastNoticeTime)
            {
                Log.Out("SevenDaysToDiscord: World time is less than last notice time, resetting notice tracker. Was someone messing with SetTime?");
                _lastNoticeTime = 0;
                _lastNoticeBloodMoonDay = 0;
            }

            if (_noticeDays < daysUntilBloodMoon)
                return;

            if (worldTimeDays >= _lastNoticeTime && bloodMoonDay == _lastNoticeBloodMoonDay)
                return;

            var message = CreateDiscordNotification(daysUntilBloodMoon, bloodMoonDay);
            if (await _webhookClient.SendMessage(message) == null)
            {
                Log.Error("SevenDaysToDiscord: Failed to send blood moon notification to discord");
                return;
            }

            _lastNoticeTime = worldTimeDays;
            _lastNoticeBloodMoonDay = bloodMoonDay;
        }

        private string CreateDiscordNotification(float daysUntilBloodMoon, int bloodMoonDay)
        {
            var realTimeUntilBloodMoon = TimeSpan.FromMinutes(daysUntilBloodMoon * GameApi.DayNightLength);
            var offset = new DateTimeOffset(DateTime.Now + realTimeUntilBloodMoon);
            var timestamp = offset.ToUnixTimeSeconds();

            return $"{{ \"content\": \"Next Blood Moon (Day {bloodMoonDay}) <t:{timestamp}:R>\" }}";
        }
    }
}
