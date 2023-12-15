using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Settings
{
    internal interface ISettingsReader
    {
        ISettings<T> Load<T>() where T : class;
        ISettings<T> LoadSection<T>(string sectionName = null) where T : class;
    }
}
