using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Settings
{
    internal static class ContainerExtensions
    {
        public static void RegisterSettings<T>(this Container container, string sectionName) where T : class
        {
            if (string.IsNullOrEmpty(sectionName))
                throw new ArgumentNullException(nameof(sectionName));

            container.Register<ISettings<T>>(() =>
            {
                var settings = container.GetInstance<ISettingsReader>();

                return settings.LoadSection<T>(sectionName);
            });
        }
    }
}
