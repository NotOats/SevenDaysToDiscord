using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord
{
    internal class BloodMoonNotifier
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly ModSettings _settings;
        private readonly WebhookClient _webhookClient;

        private readonly float _noticeDays;

        private float _lastNoticeTime = 0;
        private int _lastNoticeBloodMoonDay = 0;

        public BloodMoonNotifier(ModSettings settings, WebhookClient webhookClient)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _webhookClient = webhookClient ?? throw new ArgumentNullException(nameof(webhookClient));

            _noticeDays = (float)(_settings.BloodMoonNotificationSeconds / 60) / GameApi.DayNightLength;
        }

        public void Start()
        {
            var interval = TimeSpan.FromSeconds(_settings.BloodMoonCheckDelaySeconds);
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await CheckForBloodMoon();

                    await Task.Delay(interval, token);
                }
            }, token);

            Log.Out($"SevenDaysToDiscord: Started BM Notifier  ({interval.TotalSeconds}s interval, {_noticeDays} days notice)");
        }

        public void Stop()
        {
            Log.Out("SevenDaysToDiscord: Stopping BM Task");

            _cts.Cancel();
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

#if DEBUG
            Log.Out($"SevenDaysToDiscord: Worldtime {GameManager.Instance.World.GetWorldTime()}");
            Log.Out($"SevenDaysToDiscord: bloodMoonDay {bloodMoonDay}");
            Log.Out($"SevenDaysToDiscord: daysUntilBloodMoon {daysUntilBloodMoon}");
            Log.Out($"SevenDaysToDiscord: worldTimeDays {worldTimeDays}");
            Log.Out($"SevenDaysToDiscord: _lastNoticeTime {_lastNoticeTime}");
            Log.Out($"SevenDaysToDiscord: _lastNoticeBloodMoonDay {_lastNoticeBloodMoonDay}");
#endif

            if (_noticeDays < daysUntilBloodMoon)
                return;

            if (worldTimeDays >= _lastNoticeTime && bloodMoonDay == _lastNoticeBloodMoonDay)
                return;

            var message = CreateDiscordNotification(daysUntilBloodMoon, bloodMoonDay);
            if (!await _webhookClient.SendMessage(message))
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

            var message = $"{{ \"content\": \"Next Blood Moon (Day {bloodMoonDay}) in {realTimeUntilBloodMoon.TotalMinutes} minutes! <t:{timestamp}:F> <t:{timestamp}:R> \" }}";

#if DEBUG
            Log.Out($"SevenDaysToDiscord: message: {message}");
#endif
            return message;
        }
    }
}
