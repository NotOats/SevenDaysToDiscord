namespace SevenDaysToDiscord
{
    internal class EntryPoint : IModApi
    {
        private BloodMoonNotifier _notifier;

        public void InitMod(Mod _modInstance)
        {
            // TODO: Load settings file
            var settings = new ModSettings
            {
                //BloodMoonCheckDelaySeconds = 10,
                //BloodMoonNotificationSeconds = 23400, // 6.5hrs before (default BM cycle = every 7 hours)
                DiscordWebHook = @"https://discord.com/api/webhooks/1181558860725637181/6W7W2-Jh3EHIprpxpPT2KS595L89BdHmZeGXH68wLVQHadiiw6dE8AcFQW-3R7huuG7y"
            };

            var webhookClient = new WebhookClient(settings.DiscordWebHook);

            _notifier = new BloodMoonNotifier(settings, webhookClient);

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameShutdown.RegisterHandler(GameShutdown);
        }

        private void GameStartDone()
        {
            _notifier.Start();
        }

        private void GameShutdown()
        {
            _notifier.Stop();
        }
    }
}
