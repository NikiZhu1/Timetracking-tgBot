namespace Timetracking_HSE_Bot
{
    public class TimeTracker
    {
        private static DateTime startTime;
        private static Timer timer;

        /// <summary>
        /// Запуск таймера активности
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static async void Start(long chatId, int actNumber)
        {
            timer = new Timer(TimerCallback, null, 0, 1000);
            DB.StartTime(chatId, actNumber);
        }

        /// <summary>
        /// Остановить таймер активности
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static int Stop(long chatId, int actNumber)
        {
            if (timer != null)
                timer.Dispose();

            int totaltime = DB.StopTime(chatId, actNumber);
            return totaltime;
        }

        private static void TimerCallback(object state)
        {
            TimeSpan elapsedTime = DateTime.Now - startTime;
        }
    }
}
