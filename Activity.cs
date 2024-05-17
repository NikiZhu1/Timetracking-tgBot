﻿namespace Timetracking_HSE_Bot
{
    public class Activity
    {
        public int Number { get; set; }

        public string Name { get; set; }

        public bool IsTracking { get; set; }

        public Activity(int number, string name, bool isTracking)
        {
            Number = number;
            Name = name;
            IsTracking = isTracking;
        }

        /// <summary>
        /// Есть ли уже у пользователя активность с названием <paramref name="activityName"/>
        /// </summary>
        /// <param name="activityName">Проверяемое название активности</param>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber"></param>
        /// <returns></returns>
        public static bool IsNotRepeatingName(string? activityName, long chatId, int? actNumber = null)
        {
            //int actCount = DB.GetAllActivitiesCount(chatId);
            List<Activity> allActivities = DB.GetAllActivities(chatId);
            bool result = true;


            foreach (Activity activity in allActivities)
            {
                if (activity.Name == activityName)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Запустить активность
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static void Start(long chatId, int actNumber)
        {
            TimeTracker.Start(chatId, actNumber);
            DB.SetActivityStatus(chatId, actNumber, true);
        }

        /// <summary>
        /// Остановить активностm
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static int Stop(long chatId, int actNumber)
        {
            int totalTime = 0;
            try
            {
                totalTime = TimeTracker.Stop(chatId, actNumber);
                DB.SetActivityStatus(chatId, actNumber, false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return totalTime;
        }
    }
}
