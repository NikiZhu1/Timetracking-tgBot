namespace Timetracking_HSE_Bot
{
    public class Activity : IComparable<Activity>
    {
        public int Number { get; set; }

        public string Name { get; set; }

        public bool IsTracking { get; set; }

        public bool InArchive
        {
            get
            {
                if (DateEnd is not null)
                    return true;
                else
                    return false;
            }
        }

        public DateTime? DateStart { get; set; }

        public DateTime? DateEnd { get; set; }

        public int TotalTime { get; set; }

        public Activity(int number, string name, bool isTracking, DateTime? dateStart, DateTime? dateEnd)
        {
            Number = number;
            Name = name;
            IsTracking = isTracking;
            DateStart = dateStart;
            DateEnd = dateEnd;
        }

        public Activity(string name, int totalTime)
        {
            Name = name;
            TotalTime = totalTime;
        }

        public string TotalTimeToString()
        {
            if (TotalTime > 0)
            {
                TimeSpan result = TimeSpan.FromSeconds(TotalTime);
                int hour = result.Hours;
                int min = result.Minutes;
                int sec = result.Seconds;

                //Только секунды
                if (min == 0)
                    return $"{sec} сек.";

                //Только минуты с секундами
                else if (hour == 0)
                    return $"{min} мин. {sec} сек.";

                else return $"{hour} ч. {min} мин. {sec} сек.";
            }
            else
                return string.Empty;
        }

        public override string ToString()
        {
            return $"{Name}: {TotalTimeToString()}";
        }

        /// <summary>
        /// Проверка на существование активности с данным названием
        /// </summary>
        /// <param name="activityName"></param>
        /// <param name="chatId"></param>
        /// <param name="actNumber"></param>
        /// <returns></returns>
        public static int IsUniqueName(string? activityName, long chatId, int? actNumber = null)
        {
            List<Activity> allActivities = DB.GetActivityList(chatId, true);
            Activity? changeActivity = allActivities.FirstOrDefault(a => a.Number == actNumber);

            int result = 1;

            foreach (Activity activity in allActivities)
            {
                //Если новое название равно текущему
                if (changeActivity is not null && changeActivity.Name == activityName)
                {
                    result = 1;
                    break;
                }

                //Если совпадает с другими активностями
                if (activity.Name == activityName)
                {
                    if (activity.InArchive) //активность в архиве
                        result = -1;

                    else //активность действующая
                        result = 0;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Получить номер восстанавливаемой активности
        /// </summary>
        /// <param name="activityName"></param>
        /// <param name="chatId"></param>
        /// <param name="actNumber"></param>
        /// <returns></returns>
        public static int GetRecoveringActNumber(string? activityName, long chatId, int? actNumber = null)
        {
            List<Activity> allActivities = DB.GetActivityList(chatId, true);

            foreach (Activity activity in allActivities)
            {
                //Если совпадает с другими активностями
                if (activity.Name == activityName && activity.InArchive)
                {
                    return activity.Number;
                }
            }
            return 0;
        }

        /// <summary>
        /// Запустить активность
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static void Start(long chatId, int actNumber)
        {
            // TimeTracker.Start(chatId, actNumber);
            DB.StartTime(chatId, actNumber);
            DB.SetActivityStatus(chatId, actNumber, true);
        }

        /// <summary>
        /// Остановить активность и получить итоговое время
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static int Stop(long chatId, int actNumber)
        {
            int totaltime = 0;
            try
            {
                //totalTime = TimeTracker.Stop(chatId, actNumber);
                DB.SetActivityStatus(chatId, actNumber, false);
                totaltime = DB.StopTime(chatId, actNumber);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return totaltime;
        }

        public int CompareTo(Activity? other)
        {
            if (other == null)
                return 1;

            return other.TotalTime.CompareTo(TotalTime);
        }
    }
}
