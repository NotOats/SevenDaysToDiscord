using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SevenDaysToDiscord.Settings
{
    internal interface ISettings<T> where T : class
    {
        T Value { get; }

        void Save();
    }
}
