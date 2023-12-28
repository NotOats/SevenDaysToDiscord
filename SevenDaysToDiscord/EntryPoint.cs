using SevenDaysToDiscord.Hosting;
using SevenDaysToDiscord.Modules;
using SevenDaysToDiscord.Settings;
using SimpleInjector;
using System.IO;

namespace SevenDaysToDiscord
{
    internal class EntryPoint : IModApi
    {
        private readonly Container _container = new Container();
        private Host _host;

        public void InitMod(Mod _modInstance)
        {
            _container.Options.DefaultLifestyle = Lifestyle.Singleton;
            
            _container.Register<ISettingsReader>(() =>
            {
                var file = Path.Combine(_modInstance.Path, "appsettings.json");
                return new JsonSettingsReader(file);
            });

            _container.RegisterSettings<BloodMoonNotifierSettings>(BloodMoonNotifierSettings.SectionName);
            _container.RegisterSettings<ServerInfoSettings>(ServerInfoSettings.SectionName);

            _container.Register(typeof(WebhookClient<>));
            _container.Collection.Register<IModule>(
                typeof(BloodMoonNotifier), 
                typeof(ServerInfo));

            _container.Register<Host>();
            _container.Verify();

            _host = _container.GetInstance<Host>();

            ModEvents.GameStartDone.RegisterHandler(() => _host?.Start());
            ModEvents.GameShutdown.RegisterHandler(() => _host?.Stop());
        }
    }
}
