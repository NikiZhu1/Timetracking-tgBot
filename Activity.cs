namespace Timetracking_HSE_Bot
{
    public class Activity
    {
        public string Name { get; set; }

        public bool IsTracking { get; set; }

        /// <summary>
        /// Есть ли уже у пользователя активность с названием <paramref name="activityName"/>
        /// </summary>
        /// <param name="activityName">Проверяемое название активности</param>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber"></param>
        /// <returns></returns>
        public static bool IsNotRepeatingName(string? activityName, long chatId, int? actNumber = null)
        {
            bool result = true;

            for (int i = 1; i < 10; i++)
            {
                result = (DB.Read("RegUsers", $"act{i}", chatId) != activityName);

                //Если уже такая есть, break
                if (!result)
                    break;

                //Если юзер изменяет активность на то же название, что и было - игнорируем
                if (actNumber == i)
                    continue;
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
