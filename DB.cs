using System.Data.SQLite;

namespace Timetracking_HSE_Bot
{
    public class DB
    {
        private static readonly string fileName = "DB.db";
        private static SQLiteConnection DBConection = new($"Data Source={fileName};");

        public static string fullPath = Path.GetFullPath($"{fileName}");

        /// <summary>
        /// Занесение id и username пользователя в базу данных c активностями
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="username">Юзернэйм пользователя</param>
        public static void Registration(long chatId, string username)
        {
            try
            {
                DBConection.Open();

                using SQLiteCommand regcmd = DBConection.CreateCommand();
                {
                    regcmd.CommandText = $"SELECT EXISTS(SELECT 1 FROM RegUsers WHERE ChatId = @chatId)";
                    regcmd.Parameters.AddWithValue("@chatId", chatId);

                    //Если юзер уже есть в базе данных - скипаем
                    if (Convert.ToBoolean(regcmd.ExecuteScalar()) == false)
                    {
                        // Добавляем пользователя в таблицу RegUsers
                        regcmd.CommandText = "INSERT INTO RegUsers (ChatId, Username) VALUES (@chatId, @Username)";
                        regcmd.Parameters.AddWithValue("@Username", username);
                        regcmd.ExecuteNonQuery();

                        Console.WriteLine($"{chatId}: @{username} зарегестрирован");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
        }

        /// <summary>
        /// Прочитать строку в БД
        /// </summary>
        /// <param name="findColumn">Искомый столбец</param>
        /// <param name="chatId">id пользователя</param>
        /// <param name="table">Название таблицы</param>
        /// <returns></returns>
        public static string Read(string table, string findColumn, long chatId)
        {
            string result = "";
            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.CommandText = $"SELECT {findColumn} FROM {table} WHERE ChatId = @chatId";
                    cmd.Parameters.AddWithValue("@chatId", chatId.ToString());

                    using SQLiteDataReader reader = cmd.ExecuteReader();
                    {
                        if (reader.Read())
                        {
                            result = reader[findColumn]?.ToString() ?? string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
            return result;
        }

        /// <summary>
        /// Обновляем названия активностей в базе данных по id юзера
        /// </summary>
        /// <param name="numberAct">Номер изменяемой активности</param>
        /// <param name="newName">Новое название активности</param>
        /// <param name="chatId">id пользователя</param>
        public static void UpdateActivityName(int numberAct, string newName, long chatId)
        {
            try
            {
                DBConection.Open();

                using (SQLiteCommand cmd = DBConection.CreateCommand())
                {
                    cmd.CommandText = $"UPDATE Activities SET Name = @newValue WHERE ChatId = @chatId AND Number = @numberAct";
                    cmd.Parameters.AddWithValue("@newValue", newName);
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@numberAct", numberAct);
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"{chatId}: Активность #{numberAct} новое название: {newName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
        }

        /////<summary>
        /////Возвращает количество активностей пользователя
        /////</summary>
        //public static int GetAllActivitiesCount(long chatId)
        //{
        //    int result = 0;
        //    try
        //    {
        //        DBConection.Open();
        //        using (SQLiteCommand cmd = DBConection.CreateCommand())
        //        {
        //            cmd.CommandText = $"SELECT Number FROM Activities WHERE ChatId = @chatId ORDER BY Number DESC LIMIT 1";
        //            cmd.Parameters.AddWithValue("@chatId", chatId);

        //            using var reader = cmd.ExecuteReader();
        //            {
        //                if (reader.Read())
        //                {
        //                    result = Convert.ToInt32(reader["Number"]);
        //                }
        //                reader.Close();
        //            }
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Ошибка: " + ex);
        //    }
        //    finally
        //    {
        //        DBConection?.Close();
        //    }
        //    return result;
        //}

        /// <summary>
        /// Добавление активности в таблицу RegUsers
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="newValue">Название добавляемой активности</param>
        public static void AddActivity(long chatId, string newValue)
        {
            List<Activity> allActivities = DB.GetAllActivities(chatId);
            int actCount = allActivities.Count + 1;

            try
            {
                DBConection.Open();
                DateTime dateStart = DateTime.Now;

                using (SQLiteCommand cmd = DBConection.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Activities (ChatId, Number, Name, IsTracking, DateStart) VALUES (@chatId, @number, @name, @isTracking, @dateStart)";
                    cmd.Parameters.AddWithValue("@name", newValue);
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@number", actCount);
                    cmd.Parameters.AddWithValue("@isTracking", 0);
                    cmd.Parameters.AddWithValue("@dateStart", dateStart.ToString("yyyy-MM-dd"));
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine($"{chatId}: Активность #{actCount} - {newValue} добавлена");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
        }

        /// <summary>
        /// Завершить активность в таблице Activities
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static void EndActivity(long chatId, int actNumber)
        {
            try
            {
                DBConection.Open();
                DateTime dateEnd = DateTime.Now;

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    //Завершение активности в Activities
                    cmd.CommandText = $"UPDATE Activities SET DateEnd = @dateEnd WHERE ChatId = @chatId AND Number = @actNumber";
                    cmd.Parameters.AddWithValue("@dateEnd", dateEnd.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@actNumber", actNumber);
                    cmd.ExecuteNonQuery();

                    Console.WriteLine($"{chatId}: Активность #{actNumber} окончена");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
        }

        ///<summary>
        ///Получить лист активностей
        ///</summary>
        public static List<Activity> GetActivityList(long chatId)
        {
            List<Activity> activities = new(10);

            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    // Запрос для получения активностей
                    cmd.CommandText = $"SELECT Number, Name, IsTracking, DateStart, DateEnd FROM Activities WHERE ChatId = @chatId";
                    cmd.Parameters.AddWithValue("@chatId", chatId);

                    using var reader = cmd.ExecuteReader();
                    {
                        while (reader.Read())
                        {
                            int number = Convert.ToInt32(reader["Number"]);
                            string name = reader["Name"].ToString();
                            bool isTracking = Convert.ToBoolean(reader["IsTracking"]);
                            DateTime dateStart = Convert.ToDateTime(reader["DateStart"]);
                            DateTime dateEnd = Convert.ToDateTime(reader["DateEnd"]);
                            activities.Add(new Activity(number, name, isTracking, dateStart, dateEnd));
                        }
                        reader.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }

            return activities;
        }

        /// <summary>
        /// Получить затраченное время на активность из таблицы StartStopAct
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        /// <returns></returns>
        public static double GetStatistic(long chatId, int actNumber)
        {
            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.CommandText = "SELECT SUM(TotalTime) FROM StartStopAct WHERE ChatId = @chatId AND Act = @act";

                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@act", actNumber);

                    object sumTime = cmd.ExecuteScalar();

                    if (sumTime != null && sumTime != DBNull.Value)
                    {
                        return Convert.ToDouble(sumTime);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }

            return 0;
        }

        /// <summary>
        /// Записать время начала активности в БД
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        public static void StartTime(long chatId, int actNumber)
        {
            DateTime startTime = DateTime.Now;

            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.CommandText = "INSERT INTO StartStopAct (ChatId, Act, StartTime, TotalTime) VALUES (@chatId, @act, @startTime, @totalTime)";

                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@act", actNumber);
                    cmd.Parameters.AddWithValue("@totalTime", 0);

                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"{chatId}: Активность #{actNumber} СТАРТ: {startTime} ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
        }

        /// <summary>
        /// Остановить время отслеживания активности в БД
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активност</param>
        public static int StopTime(long chatId, int actNumber)
        {
            TimeSpan result;
            DateTime stopTime = DateTime.Now;
            DateTime startTime = Read("StartTime", chatId, actNumber);

            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@startTime", startTime);
                    cmd.Parameters.AddWithValue("@act", actNumber);
                    cmd.Parameters.AddWithValue("@stoptime", stopTime);

                    cmd.CommandText = $"UPDATE StartStopAct SET StopTime = @stoptime, TotalTime = @totalTime " +
                        $"WHERE ChatId = @chatId AND Act = @act AND StartTime = @startTime";

                    result = stopTime - startTime;
                    int totalTime = result.Seconds + result.Minutes * 60 + result.Hours * 3600;
                    cmd.Parameters.AddWithValue("@totalTime", totalTime);
                    cmd.ExecuteNonQuery();

                    Console.WriteLine($"{chatId}: Активность #{actNumber} СТОП: {stopTime} ВСЕГО: {totalTime}");
                    return totalTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
            return 0;
        }

        /// <summary>
        /// Прочитать время в БД
        /// </summary>
        /// <param name="findColumn">Искомый столбец</param>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        /// <returns>Переменная типа DateTime</returns>
        public static DateTime Read(string findColumn, long chatId, int actNumber, string table = "StartStopAct")
        {
            DateTime result = DateTime.Now;
            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.CommandText = $"SELECT {findColumn} FROM {table} WHERE ChatId = @chatId AND Act = @act ORDER BY {findColumn} DESC LIMIT 1";
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@act", actNumber);

                    using var reader = cmd.ExecuteReader();
                    {
                        if (reader.Read())
                        {
                            result = DateTime.Parse(Convert.ToString(reader[findColumn]) ?? System.String.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }

            return result;
        }

        /// <summary>
        /// Прочитать логическую переменную в БД
        /// </summary>
        /// <param name="findColumn">Искомый столбец</param>
        /// <param name="chatId">id пользователя</param>
        /// <returns>Переменная типа bool</returns>
        public static bool Read(string findColumn, long chatId, string table = "ActivityMonitor")
        {
            bool result = false;
            try
            {
                DBConection.Open();

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {

                    cmd.CommandText = $"SELECT {findColumn} FROM {table} WHERE ChatId = @ChatId";

                    result = (bool)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }

            return result;
        }

        /// <summary>
        /// Установить статус для активности в таблице ActivityMonitor
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">номер активности</param>
        /// <param name="isStarted">статус активности, начата или нет</param>
        public static void SetActivityStatus(long chatId, int actNumber, bool isStarted)
        {
            try
            {
                DBConection.Open();

                using (SQLiteCommand cmd = DBConection.CreateCommand())
                {
                    cmd.CommandText = $"UPDATE ActivityMonitor SET act{actNumber} = @value WHERE ChatId = @chatId";
                    cmd.Parameters.AddWithValue("@value", isStarted);
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@actNumber", actNumber);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
            }
            finally
            {
                DBConection?.Close();
            }
        }
    }
}
