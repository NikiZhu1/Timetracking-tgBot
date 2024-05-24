﻿using System.Data.SqlClient;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace Timetracking_HSE_Bot
{
    // Класс для пользовательской функции REGEXP
    public class RegexpSQLiteFunction : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            string pattern = args[0].ToString();
            string input = args[1].ToString();
            return Regex.IsMatch(input, pattern);
        }
    }

    public class DB
    {
        private static readonly string fileName = "DB.db";
        private static SQLiteConnection DBConection = new($"Data Source={fileName}; Trusted_Connection=True;");

        public static string fullPath = Path.GetFullPath($"{fileName}");

        /// <summary>
        /// Занесение id и username пользователя в бд
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
                throw;
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
        //public static string Read(string table, string findColumn, long chatId)
        //{
        //    string result = "";
        //    try
        //    {
        //        DBConection.Open();

        //        using SQLiteCommand cmd = DBConection.CreateCommand();
        //        {
        //            cmd.CommandText = $"SELECT {findColumn} FROM {table} WHERE ChatId = @chatId";
        //            cmd.Parameters.AddWithValue("@chatId", chatId.ToString());

        //            using SQLiteDataReader reader = cmd.ExecuteReader();
        //            {
        //                if (reader.Read())
        //                {
        //                    result = reader[findColumn]?.ToString() ?? string.Empty;
        //                }
        //            }
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
                throw;
            }
            finally
            {
                DBConection?.Close();
            }
        }

        //функция с транзакцией
        public static async void AddActivity(long chatId, string newValue)
        {
            List<Activity> allActivities = GetActivityList(chatId, true);
            int actCount = allActivities.Count + 1;
            DateTime dateStart = DateTime.Now;
            DBConection.Open();
            SQLiteTransaction transaction = DBConection.BeginTransaction();
            SQLiteCommand cmd = DBConection.CreateCommand();
            cmd.Transaction = transaction;
            try
            {
                cmd.CommandText = "INSERT INTO Activities (ChatId, Number, Name, IsTracking, DateStart) VALUES (@chatId, @number, @name, @isTracking, @dateStart)";
                cmd.Parameters.AddWithValue("@name", newValue);
                cmd.Parameters.AddWithValue("@chatId", chatId);
                cmd.Parameters.AddWithValue("@number", actCount);
                cmd.Parameters.AddWithValue("@isTracking", 0);
                cmd.Parameters.AddWithValue("@dateStart", dateStart.ToString("yyyy-MM-dd"));
                cmd.ExecuteNonQuery();

                await transaction.CommitAsync();
                Console.WriteLine($"{chatId}: Активность #{actCount} - {newValue} добавлена");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
                await transaction.RollbackAsync();
            }
            finally { DBConection?.Close(); }
        }

        /// <summary>
        /// Добавить активность
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="newValue">Название добавляемой активности</param>
        //public static void AddActivity(long chatId, string newValue)
        //{
        //    List<Activity> allActivities = DB.GetActivityList(chatId, true);
        //    int actCount = allActivities.Count + 1;

        //    try
        //    {
        //        DBConection.Open();
        //        DateTime dateStart = DateTime.Now;

        //        using (SQLiteCommand cmd = DBConection.CreateCommand())
        //        {
        //            cmd.CommandText = "INSERT INTO Activities (ChatId, Number, Name, IsTracking, DateStart) VALUES (@chatId, @number, @name, @isTracking, @dateStart)";
        //            cmd.Parameters.AddWithValue("@name", newValue);
        //            cmd.Parameters.AddWithValue("@chatId", chatId);
        //            cmd.Parameters.AddWithValue("@number", actCount);
        //            cmd.Parameters.AddWithValue("@isTracking", 0);
        //            cmd.Parameters.AddWithValue("@dateStart", dateStart.ToString("yyyy-MM-dd"));
        //            cmd.ExecuteNonQuery();
        //        }

        //        Console.WriteLine($"{chatId}: Активность #{actCount} - {newValue} добавлена");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Ошибка: " + ex);
        //        throw;
        //    }
        //    finally
        //    {
        //        DBConection?.Close();
        //    }
        //}

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
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@actNumber", actNumber);
                    cmd.ExecuteNonQuery();

                    Console.WriteLine($"{chatId}: Активность #{actNumber} окончена");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
                throw;
            }
            finally
            {
                DBConection?.Close();
            }
        }

        //функция с транзакцией
        public static List<Activity> GetActivityList(long chatId, bool getFullList = false)
        {
            List<Activity> activities = new(10);
            string command = $"SELECT Number, Name, IsTracking, DateStart, DateEnd FROM Activities WHERE ChatId = @chatId";

            if (!getFullList)
                command += " AND DateEnd IS NULL";

            try
            {
                DBConection.Open();
                using var transaction = DBConection.BeginTransaction();
                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.Transaction = transaction;
                    // Запрос для получения активностей
                    cmd.CommandText = command;
                    cmd.Parameters.AddWithValue("@chatId", chatId);

                    using var reader = cmd.ExecuteReader();
                    {
                        int number;
                        string name;
                        bool isTracking;
                        DateTime? dateStart = null;
                        DateTime? dateEnd = null;

                        while (reader.Read())
                        {
                            number = Convert.ToInt32(reader["Number"]);

                            name = reader["Name"].ToString();

                            isTracking = Convert.ToBoolean(reader["IsTracking"]);

                            if (reader["DateStart"] is not DBNull)
                                dateStart = Convert.ToDateTime(reader["DateStart"]);

                            if (reader["DateEnd"] is not DBNull)
                                dateEnd = Convert.ToDateTime(reader["DateEnd"]);

                            activities.Add(new Activity(number, name, isTracking, dateStart, dateEnd));
                        }
                        reader.Close();
                    }
                    transaction.Commit(); // Фиксация транзакции
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
                throw;
            }
            finally
            {
                DBConection?.Close();
            }
            activities.Sort();

            return activities;
        }


        ///<summary>
        ///Получить лист активностей
        ///</summary>
        //public static List<Activity> GetActivityList(long chatId, bool getFullList = false)
        //{
        //    List<Activity> activities = new(10);
        //    string command = $"SELECT Number, Name, IsTracking, DateStart, DateEnd FROM Activities WHERE ChatId = @chatId";

        //    if (!getFullList)
        //        command += " AND DateEnd IS NULL";

        //    try
        //    {
        //        DBConection.Open();

        //        using SQLiteCommand cmd = DBConection.CreateCommand();
        //        {
        //            // Запрос для получения активностей
        //            cmd.CommandText = command;
        //            cmd.Parameters.AddWithValue("@chatId", chatId);

        //            using var reader = cmd.ExecuteReader();
        //            {
        //                int number;
        //                string name;
        //                bool isTracking;
        //                DateTime? dateStart = null;
        //                DateTime? dateEnd = null;

        //                while (reader.Read())
        //                {
        //                    number = Convert.ToInt32(reader["Number"]);

        //                    name = reader["Name"].ToString();

        //                    isTracking = Convert.ToBoolean(reader["IsTracking"]);

        //                    if (reader["DateStart"] is not DBNull)
        //                        dateStart = Convert.ToDateTime(reader["DateStart"]);

        //                    if (reader["DateEnd"] is not DBNull)
        //                        dateEnd = Convert.ToDateTime(reader["DateEnd"]);

        //                    activities.Add(new Activity(number, name, isTracking, dateStart, dateEnd));
        //                }
        //                reader.Close();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Ошибка: " + ex);
        //        throw;
        //    }
        //    finally
        //    {
        //        DBConection?.Close();
        //    }
        //    activities.Sort();

        //    return activities;
        //}

        /// <summary>
        /// Получить затраченное время на активность из таблицы StartStopAct
        /// </summary>
        /// <param name="chatId">id пользователя</param>
        /// <param name="actNumber">Номер активности</param>
        /// <param name="monthNumber">Номер месяца</param>
        /// <returns></returns>
        public static int GetStatistic(long chatId, int actNumber, int monthNumber = 0, DateTime today = default)
        {
            string command = $"SELECT SUM(TotalTime) FROM StartStopAct WHERE ChatId = @chatId AND Number = @act";

            if (monthNumber != 0)
            {
                string month = monthNumber.ToString("00"); // Преобразует число в строку с ведущим нулем
                string pattern = $@"^\d{{4}}-{month}-\d{{2}} \d{{2}}:\d{{2}}:\d{{2}}$";

                command += $" AND StopTime REGEXP '{pattern}'";
            }

            if (today != default)
            {
                string todayYear = today.Year.ToString();
                string todayMonth = today.Month.ToString("00");
                string todayDay = today.Day.ToString("00");
                string pattern = $@"^{todayYear}-{todayMonth}-{todayDay} \d{{2}}:\d{{2}}:\d{{2}}$";
                command += $" AND StopTime REGEXP '{pattern}'";
            }
            try
            {
                DBConection.Open();
                if (today != null || monthNumber != 0)
                {
                    var attribute = new SQLiteFunctionAttribute
                    {
                        Name = "REGEXP",
                        Arguments = 2,
                        FuncType = FunctionType.Scalar
                    };
                    // Создание экземпляра пользовательской функции
                    var function = new RegexpSQLiteFunction();
                    // Привязка функции к соединению
                    DBConection.BindFunction(attribute, function);
                }

                using SQLiteCommand cmd = DBConection.CreateCommand();
                {
                    cmd.CommandText = command;

                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@act", actNumber);

                    object sumTime = cmd.ExecuteScalar();

                    if (sumTime != null && sumTime != DBNull.Value)
                    {
                        return Convert.ToInt32(sumTime);
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
                throw;
            }
            finally
            {
                DBConection?.Close();
            }
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
                    cmd.CommandText = "INSERT INTO StartStopAct (ChatId, Number, StartTime, TotalTime) VALUES (@chatId, @act, @startTime, @totalTime)";

                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@startTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@act", actNumber);
                    cmd.Parameters.AddWithValue("@totalTime", 0);

                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"{chatId}: Активность #{actNumber} СТАРТ: {startTime} ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
                throw;
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
                    cmd.Parameters.AddWithValue("@act", actNumber);
                    cmd.Parameters.AddWithValue("@stoptime", stopTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@starttime", startTime);

                    cmd.CommandText = $"UPDATE StartStopAct SET StopTime = @stoptime, TotalTime = @totalTime " +
                        $"WHERE ChatId = @chatId AND Number = @act AND StartTime = @starttime";

                    result = stopTime - startTime;
                    int totalTime = (int)result.TotalSeconds;
                    cmd.Parameters.AddWithValue("@totalTime", totalTime);
                    cmd.ExecuteNonQuery();

                    Console.WriteLine($"{chatId}: Активность #{actNumber} СТОП: {stopTime} ВСЕГО: {totalTime}");
                    return totalTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
                throw;
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
                    cmd.CommandText = $"SELECT {findColumn} FROM {table} WHERE ChatId = @chatId AND Number = @act ORDER BY {findColumn} DESC LIMIT 1";
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
                throw;
            }
            finally
            {
                DBConection?.Close();
            }

            return result;
        }

        /// <summary>
        /// Установить статус для активности
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
                    cmd.CommandText = $"UPDATE Activities SET IsTracking = @value WHERE ChatId = @chatId AND Number = @actNumber";
                    cmd.Parameters.AddWithValue("@value", isStarted);
                    cmd.Parameters.AddWithValue("@chatId", chatId);
                    cmd.Parameters.AddWithValue("@actNumber", actNumber);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex);
                throw;
            }
            finally
            {
                DBConection?.Close();
            }
        }
    }
}
