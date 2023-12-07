using System;

namespace SevenDaysToDiscord
{
    internal static class GameApi
    {
        public static int DayNightLength 
            => GamePrefs.GetInt(EnumUtils.Parse<EnumGamePrefs>("DayNightLength"));

        public static (int duskHours, int dawnHours) DuskDawnHours
            => GameUtils.CalcDuskDawnHours(GamePrefs.GetInt(EnumUtils.Parse<EnumGamePrefs>("DayLightLength")));

        public static int BloodMoonDay
            => GameStats.GetInt(EnumUtils.Parse<EnumGameStats>("BloodMoonDay"));

        public static bool IsBloodMoonActive 
            => GameUtils.IsBloodMoonTime(GameManager.Instance.World.GetWorldTime(), DuskDawnHours, BloodMoonDay);

        public static (int day, int hours, int minutes) WorldTime 
            => GameUtils.WorldTimeToElements(GameManager.Instance.World.GetWorldTime());

        public static float WorldTimeTotalDays
        {
            get
            {
                (int days, int hours, int minutes) = WorldTime;

                var totalHours = hours + ((float)minutes / 60);
                var totalDays = days + ((float)totalHours / 24);

                return totalDays;
            }
        }

        public static float DaysUntilBloodMoon(int bloodMoonDay)
        {
            var totalDays = WorldTimeTotalDays;

            // When the next blood moon starts at (day + dusk hours)
            (int duskHour, _) = DuskDawnHours;
            var bloodMoonDays = bloodMoonDay + ((float)duskHour / 24);

            return bloodMoonDays - totalDays;
        }

        public static TimeSpan NextBloodMoonInRealTimeSpan(int bloodMoonDay)
        {
            // Get the total amount of days passed
            var totalDays = WorldTimeTotalDays;

            // Get next blood moon time in days
            (int duskHour, _) = DuskDawnHours;
            var bloodMoonDays = bloodMoonDay + ((float)duskHour / 24);

            // Difference in real time
            var difference = bloodMoonDays - totalDays;
            var minutesRealTimeDiff = difference * DayNightLength;

            return TimeSpan.FromMinutes(minutesRealTimeDiff);
        }
    }
}
