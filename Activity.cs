namespace Timetracking_HSE_Bot
{
    public class Activity : IComparable<Activity>
    {
        public int Number { get; set; }

        public string Name { get; set; }

        public bool IsTracking { get; set; }

        public bool IsEnded
        {
            get
            {
                if (DateEnd is null)
                    return false;
                else
                    return true;
            }
            set { }
        }

        public DateTime? DateStart { get; set; }

        public DateTime? DateEnd { get; set; }

        public int TotalTime
        {
            get
            {
                if (DateEnd != null && DateStart != null)
                {
                    TimeSpan result = (TimeSpan)(DateEnd - DateStart);
                    int totalSeconds = (int)result.TotalSeconds;
                    return totalSeconds;
                }
                else
                    return 0;
            }
        }

        public Activity(int number, string name, bool isTracking, DateTime? dateStart, DateTime? dateEnd)
        {
            Number = number;
            Name = name;
            IsTracking = isTracking;
            DateStart = dateStart;
            DateEnd = dateEnd;
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
            List<Activity> allActivities = DB.GetActivityList(chatId);
            bool result = true;

            foreach (Activity activity in allActivities)
            {
                //Если новое название равно текущему
                if (activity.Number == actNumber)
                {
                    result = true;
                    break;
                }

                //Если совпадает с другими активностями
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

        public int CompareTo(Activity? other)
        {
            return this.TotalTime.CompareTo(other.TotalTime);
        }
    }
}
