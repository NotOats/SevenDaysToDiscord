using SevenDaysToDiscord.Hosting;
using SevenDaysToDiscord.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace SevenDaysToDiscord
{
    internal class EntryPoint : IModApi
    {
        private Host _host;

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

            // Initialize modules & host
            var modules = new List<IModule>
            {
                new BloodMoonNotifier(settings, webhookClient)
            };

            _host = new Host(modules);

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameShutdown.RegisterHandler(GameShutdown);
        }

        private void GameStartDone()
        {
            _host?.Start();
        }

        private void GameShutdown()
        {
            _host?.Stop();
        }
    }
}
