using System.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Timetracking_HSE_Bot
{
    internal class Program
    {
        public static int totalActivitiesCount = 10;

        static public TelegramBotClient botClient = new("");

        static async Task Main(string[] args)
        {
            string token = ConfigurationManager.AppSettings["Token"];
            botClient = new TelegramBotClient(token);
            var me = await botClient.GetMeAsync(); //Получаем информацию о боте
            botClient.StartReceiving(Update, Error);
            Console.WriteLine($"Бот {me.FirstName} запущен! id: {me.Id}");
            Console.ReadLine();
        }

        //Асинхронная задача которая реагирует на взаимодействия с ботом
        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;
            var callbackQuery = update.CallbackQuery;

            switch (update.Type)
            {
                //Если прислали сообщение
                case UpdateType.Message:
                    await MessageAsync(message);
                    break;

                //Если нажали на инлайн-кнопку
                case UpdateType.CallbackQuery:
                    await CallbackQueryAsync(botClient, callbackQuery);
                    break;

                default:
                    Console.WriteLine($"Ошибка {update.Type}");
                    break;
            }
        }

        //Обработка: СООБЩЕННИЯ
        static async Task MessageAsync(Message message)
        {
            long chatId = message.Chat.Id;
            (User.State state, int? actNumber) userInfo = User.GetState(chatId);

            //стартовое сообщение
            if (message.Text != null && message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(chatId,
                text: "У вас есть несколько предустановленых активностей: работа, спорт, отдых.\n" +
                "*Старт* — запускается таймер активности,\n" +
                "*Стоп* — таймер активности останавливается.\n" +
                "\n" +
                "📊 Активности могут отслеживатся одновременно\n" +
                "⚠️ Главное не забывайте их останавливать\n" +
                "\n" +
                "Узнать больше о функциях бота — /help",
                parseMode: ParseMode.Markdown);

                //Регистрация пользователя в БД
                try
                {
                    DB.Registration(chatId, message.Chat.Username);

                    //Инициализация инлайн клавиатуры
                    InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                    //Вывод клавиатуры с сообщением
                    Message messageAct = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.",
                        replyMarkup: activityKeyboard);

                    //Удаление прошлой клавиатуры c активностями
                    int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                    InlineKeyboard.RemoveMessageId(chatId);
                    if (tempMessageId != 0)
                    {
                        await botClient.DeleteMessageAsync(chatId, tempMessageId);
                    }

                    InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                    Console.WriteLine(ex.Message);
                }
            }

            else if (message.Text != null && message.Text == "/archive")
            {
                if (!DB.HaveUser(chatId))
                {
                    await botClient.SendTextMessageAsync(chatId,
                        "🤔 Вы не зарегестрированы. Нажмите на команду /start");
                    return;
                }
                List<Activity> archive = DB.GetActivityList(chatId, getOnlyArchived: true);

                if (archive.Count == 0)
                {
                    //Вывод клавиатуры с сообщением
                    await botClient.SendTextMessageAsync(chatId,
                        "🗂 Архив пуст\n\n" +
                        "ℹ️ Когда Вы захотите временно скрыть некоторые активности из главного меню и не отслеживать их, " +
                        "Вы можете добавить их в архив, и они будут храниться здесь.");
                }
                else
                {
                    //Инициализация инлайн клавиатуры
                    InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(DB.GetActivityList(chatId, getOnlyArchived: true));

                    //Вывод клавиатуры с сообщением
                    await botClient.SendTextMessageAsync(chatId,
                        "🗂 Архив\n\n" +
                        "ℹ️ Эти активности в данный момент скрыты из главного меню, и их отслеживание недоступно. " +
                        "Вы можете восстановить их или удалить, нажав на нужную активность.",
                        replyMarkup: archivedActivityKeyboard);
                }
            }

            else if (message.Text != null && message.Text == "/help")
            {
                await botClient.SendTextMessageAsync(chatId,
                text: "Чтобы запустить бота нажмите на команду /start\n" +
                "Хотите узнать больше? В нашей <a href=\"https://telegra.ph/Lovec-vremeni--Spravka-05-26\">справке</a> есть вся информация о функциях бота!",
                parseMode: ParseMode.Html,
                replyMarkup: InlineKeyboard.Help());
            }

            else if (message.Text != null && message.Text == "/menu")
            {
                if (!DB.HaveUser(chatId))
                {
                    await botClient.SendTextMessageAsync(chatId,
                        "🤔 Вы не зарегестрированы. Нажмите на команду /start");
                    return;
                }
                try
                {
                    //Инициализация инлайн клавиатуры
                    InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                    //Вывод клавиатуры с сообщением
                    Message messageAct = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⏱ Вот все Ваши активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                        replyMarkup: activityKeyboard);

                    //Удаление прошлой клавиатуры c активностями
                    int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                    InlineKeyboard.RemoveMessageId(chatId);
                    if (tempMessageId != 0)
                        await botClient.DeleteMessageAsync(chatId, tempMessageId);
                    InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                    Console.WriteLine(ex.Message);
                }
            }

            //Изменение названия активности - новая версия (проверка что такое название есть в архиве)
            else if (userInfo.state == User.State.WaitMessageForChangeAct && userInfo.actNumber.HasValue)
            {
                int isDeletedName = Activity.IsUniqueName(message.Text, chatId, userInfo.actNumber);
                if (isDeletedName == 0)
                {
                    // Если уже есть активность с таким названием
                    await botClient.SendTextMessageAsync(chatId,
                        text: "❗ У Вас уже есть активность с таким названием.");
                }
                else if (isDeletedName == -1)
                {
                    await botClient.SendTextMessageAsync(chatId,
                        text: "❗ У Вас есть активность с таким названием в архиве.");
                }
                else if (message.Text == null)
                {
                    // Если пользователь не ввел текст, отправляем предупреждение
                    await botClient.SendTextMessageAsync(chatId,
                        text: "❗ В качестве названия введите текст или смайлик.");
                }
                else
                {
                    // Пользователь ввел текст, обновляем название активности
                    try
                    {
                        DB.UpdateActivityName((int)userInfo.actNumber, message.Text, chatId);

                        //Удаление прошлой клавиатуры
                        int messageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                        InlineKeyboard.RemoveMessageId(chatId);
                        await botClient.DeleteMessageAsync(chatId, messageId);

                        // Сбросить состояние пользователя
                        User.ResetState(chatId);
                        InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                        // Отправляем сообщение
                        Message messageAct = await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.",
                            replyMarkup: activityKeyboard);

                        //Запомнить id сообщения списка активностей для удаления
                        InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                }
            }

            //Добавление новой активности/восстановление активностей
            else if (userInfo.state == User.State.WaitMessageForAddAct)
            {
                int isDeletedName = Activity.IsUniqueName(message.Text, chatId);
                if (isDeletedName == 0)
                {
                    // Если уже есть активность с таким названием
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❗ У Вас уже есть активность с таким названием.");
                }
                else if (message.Text == null)
                {
                    // Если пользователь не ввел текст, отправляем предупреждение
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❗ В качестве названия введите текст или смайлик.");
                }
                else
                {
                    // Пользователь ввёл название, добавляем активность/восстанавливаем активность
                    try
                    {
                        if (isDeletedName == -1)
                        {
                            await botClient.SendTextMessageAsync(chatId, "У Вас есть активность с таким названием в архиве. Воостанавливаю ее.");
                            int recoveringNumber = Activity.GetRecoveringActNumber(message.Text, chatId); //номер активности, которая была удалена и которую мы собираемя восстановить
                            DB.UpdateDateEndStatus(chatId, recoveringNumber); //обновляется дата окончания активности на null
                        }
                        else DB.AddActivity(chatId, message.Text);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }

                    //Удаление прошлой клавиатуры
                    int messageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                    InlineKeyboard.RemoveMessageId(chatId);
                    await botClient.DeleteMessageAsync(chatId, messageId);

                    // Сбросить состояние пользователя
                    User.ResetState(chatId);

                    InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                    Message messageAct = await botClient.SendTextMessageAsync(
                      chatId: chatId,
                      text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.",
                      replyMarkup: activityKeyboard);

                    InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);
                }
            }
        }

        static List<Activity> GetStatisticList(long chatId, DateTime? firstDate = null, DateTime? secondDate = null)
        {
            List<Activity> statistic = new();
            try
            {
                List<Activity> activityList = DB.GetActivityList(chatId, true);

                foreach (Activity activity in activityList)
                {
                    int totalSeconds = 0;

                    if (!firstDate.HasValue && !secondDate.HasValue) //за всё время
                    {
                        totalSeconds = DB.GetStatistic(chatId, activity.Number);
                    }
                    else if (!secondDate.HasValue) //за день
                    {
                        totalSeconds = DB.GetStatistic(chatId, activity.Number, firstDate);
                    }
                    else //за определённый период (неделя, месяц)
                    {
                        totalSeconds = DB.GetStatistic(chatId, activity.Number, firstDate, secondDate);
                    }

                    if (totalSeconds > 0)
                    {
                        statistic.Add(new Activity(activity.Name, totalSeconds));
                    }
                }
                statistic.Sort(); //Сортируем полученный список активностей по затраченному времени
            }
            catch (Exception)
            {
                throw;
            }
            return statistic;
        }

        static async void SendStatictic(long chatId, List<Activity> statisticList, string message)
        {
            if (statisticList.Count == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "У Вас пока нет записей об активностях за этот период.\n" +
                    "🚀 Запускайте таймер и сможете отследить свой прогресс!");
                return;
            }

            string text = message + "\n";

            string[] medals = { "🥇", "🥈", "🥉" };

            // Формирование текста со статистикой
            for (int i = 0; i < statisticList.Count; i++)
            {
                if (i < 3)
                    text += medals[i] + statisticList[i].ToString() + "\n";
                else
                    text += "\n — " + statisticList[i].ToString();
            }

            await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: text);
        }

        //Обработка: КАЛЛБЭКИ ОТ ИНЛАЙН-КНОПОК
        static async Task CallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            int messageId = callbackQuery.Message.MessageId;
            long chatId = callbackQuery.Message.Chat.Id;
            List<Activity> activityList = DB.GetActivityList(chatId);

            switch (Regex.Replace(callbackQuery.Data, @"\d", ""))
            {
                case "add_activity":
                    {
                        try
                        {
                            if (activityList.Count() == totalActivitiesCount)
                            {
                                Console.WriteLine($"{chatId}: Попытка добавления >{totalActivitiesCount} активностей");

                                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                                $"⚙️ Вы достигли максимального количества активностей ({totalActivitiesCount}).\n\n" +
                                "Пожалуйста, отправьте в архив или удалите неиспользуемые активности, чтобы добавить новую.",
                                showAlert: true);
                                break;
                            }

                            //Изменение состояния пользователя
                            User.SetState(chatId, User.State.WaitMessageForAddAct);
                            await botClient.SendTextMessageAsync(chatId,
                            text: $"✏ Введите название для новой активности");

                            //Получение message.id для последующего удаления
                            InlineKeyboard.SetMessageIdForDelete(chatId, messageId);
                        }
                        catch (Exception ex) 
                        {
                            Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                        }

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "statistic":
                    {
                        await botClient.SendTextMessageAsync(chatId,
                            text: "Выберете, в каком формате Вы хотите получить статистику",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: InlineKeyboard.StaticticType());

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "statistic_":
                    {
                        int statisticType = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));

                        //За всё время
                        if (statisticType == 1)
                        {
                            SendStatictic(chatId, GetStatisticList(chatId), "Статистика за всё время:");

                            await botClient.DeleteMessageAsync(chatId, messageId);
                            Console.WriteLine($"{chatId}: Получение статистики за всё время");

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Статистика получена");
                        }

                        //За месяц
                        else if (statisticType == 2)
                        {
                            InlineKeyboardMarkup monthKeyboard = InlineKeyboard.Months();

                            await botClient.EditMessageTextAsync(chatId, messageId,
                            text: "Выберете месяц, за который Вы хотите получить статистику активностей",
                            replyMarkup: monthKeyboard);

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        }

                        //За неделю
                        else if (statisticType == 3)
                        {
                            DateTime today = DateTime.Now.Date;
                            SendStatictic(chatId, GetStatisticList(chatId, today.AddDays(-7), today.AddDays(1)), "Статистика за последнюю неделю:");

                            await botClient.DeleteMessageAsync(chatId, messageId);
                            Console.WriteLine($"{chatId}: Получение статистики за неделю");

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Статистика получена");
                        }

                        //За день
                        else if (statisticType == 4)
                        {
                            DateTime today = DateTime.Now.Date;
                            SendStatictic(chatId, GetStatisticList(chatId, today), "Статистика за текущий день");

                            await botClient.DeleteMessageAsync(chatId, messageId);
                            Console.WriteLine($"{chatId}: Получение статистики за текущий день");

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Статистика получена");
                        }

                        break;
                    }
                case "month_":
                    {
                        int monthNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));

                        if (monthNumber == 13)
                        {
                            await botClient.EditMessageTextAsync(chatId, messageId,
                                text: "Выберете, в каком формате Вы хотите получить статистику",
                                parseMode: ParseMode.Markdown,
                                replyMarkup: InlineKeyboard.StaticticType());
                            break;
                        }

                        DateTime today = DateTime.Now.Date;
                        DateTime firstDate = new(today.Year, monthNumber, 1);
                        DateTime secondDate = firstDate.AddMonths(1);
                        List<Activity> statistic = GetStatisticList(chatId, firstDate, secondDate);

                        string month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNumber);
                        SendStatictic(chatId, statistic, $"Статистика за {month}:");

                        await botClient.DeleteMessageAsync(chatId, messageId);
                        Console.WriteLine($"{chatId}: Получение статистики за месяц");

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Статистика получена");
                        break;
                    }

                case "aboutAct":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity != null)
                        {
                            string status = activity.IsTracking ? ": Отслеживается ⏱" : "";

                            //Получение message.id для последующего удаления
                            InlineKeyboard.SetMessageIdForDelete(chatId, messageId);

                            await botClient.SendTextMessageAsync(chatId,
                                text: $"{activity.Name}{status}\n\n" +
                                $"Вы можете изменить название активности, отправить в архив или удалить её",
                                parseMode: ParseMode.Markdown,
                                replyMarkup: InlineKeyboard.ChangeActivity(actNumber));
                        }
                        else
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }
                case "archive":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }
                        if (activity.IsTracking)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                            "⚙️ Вы отправили в архив отслеживаемую активность. Её таймер остановлен.",
                            showAlert: true);
                        }
                        try
                        {
                            if (activity.IsTracking)   //Остановка таймера активности
                                Activity.Stop(chatId, actNumber);

                            //Отправление активности в архив
                            DB.ArchiveActivity(chatId, actNumber);

                            await botClient.SendTextMessageAsync(chatId,
                            text: $"🗂 {activity.Name}: отправлено в архив\nПоказать архив — /archive");

                            //Отправка списка активностей
                            InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));
                            Message message = await botClient.SendTextMessageAsync(chatId,
                                text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.",
                                replyMarkup: activityKeyboard);

                            //Удаление клавиатуры aboutact
                            await botClient.DeleteMessageAsync(chatId, messageId);

                            //Удаление прошлой клавиатуры c активностями
                            int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                            InlineKeyboard.RemoveMessageId(chatId);
                            await botClient.DeleteMessageAsync(chatId, tempMessageId);

                            //Запомнить id сообщения для удаления
                            InlineKeyboard.SetMessageIdForDelete(chatId, message.MessageId);

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗂 Активность отправлена в архив");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n"
                            + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        }
                        break;
                    }

                case "aboutArchive":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));

                        List<Activity> archive = DB.GetActivityList(chatId, getOnlyArchived: true);
                        Activity? activity = archive.FirstOrDefault(a => a.Number == actNumber);

                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }

                        await botClient.EditMessageTextAsync(chatId, messageId,
                            text: $"🗂 {activity.Name} в архиве\n\n" +
                                  $"Вы можете восстановить её, чтобы снова отслеживать её, или полностью удалить.",
                            replyMarkup: InlineKeyboard.ChangeArchive(actNumber));
                        InlineKeyboard.SetMessageIdForDelete(chatId, messageId);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "backToArchive":
                    {
                        List<Activity> archive = DB.GetActivityList(chatId, getOnlyArchived: true);

                        if (archive == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }

                        if (archive.Count == 0)
                        {
                            //Вывод клавиатуры с сообщением
                            await botClient.EditMessageTextAsync(chatId, messageId,
                                "🗂 Архив пуст\n\n" +
                                "ℹ️ Когда Вы захотите временно скрыть некоторые активности из главного меню и не отслеживать их, " +
                                "Вы можете добавить их в архив, и они будут храниться здесь.");
                            break;
                        }

                        //Инициализация инлайн клавиатуры
                        InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(archive);

                        //Вывод клавиатуры с сообщением
                        await botClient.EditMessageTextAsync(chatId, messageId,
                            "🗂 Архив\n\n" +
                            "ℹ️ Эти активности в данный момент скрыты из главного меню, и их отслеживание недоступно. " +
                            "Вы можете восстановить их или удалить, нажав на нужную активность.",
                            replyMarkup: archivedActivityKeyboard);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "recover":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        List<Activity> archive = DB.GetActivityList(chatId, getOnlyArchived: true);
                        Activity? activity = archive.FirstOrDefault(a => a.Number == actNumber);

                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }
                        try
                        {
                            //Обновляется дата окончания активности на null
                            DB.UpdateDateEndStatus(chatId, actNumber);

                            await botClient.SendTextMessageAsync(chatId,
                            text: $"📤 {activity.Name}: восстановлено из архива");

                            archive = DB.GetActivityList(chatId, getOnlyArchived: true);
                            InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(archive);

                            if (archive.Count != 0)
                            {
                                await botClient.SendTextMessageAsync(chatId,
                               "🗂 Архив\n\n" +
                               "ℹ️ Эти активности в данный момент скрыты из главного меню, и их отслеживание недоступно. " +
                               "Вы можете восстановить их или удалить, нажав на нужную активность.",
                               replyMarkup: archivedActivityKeyboard);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId,
                                "🗂 Архив пуст\n\n" +
                                "ℹ️ Когда вы захотите временно скрыть некоторые активности из главного меню и не отслеживать их, " +
                                "вы можете добавить их в архив, и они будут храниться здесь.");
                            }
                            //Удаление AboutArchiveAct
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "📤 Активность восстановленна");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка: {ex.Message}.\n"
                            + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        }
                        break;
                    }
                case "rename":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }

                        //Изменение состояния пользователя
                        User.SetState(chatId, User.State.WaitMessageForChangeAct, actNumber);

                        await botClient.SendTextMessageAsync(chatId,
                        text: $"✏️ Введите новое название для активности \"{activity.Name}\"");
                        //Удаление клавиатуры aboutact
                        await botClient.DeleteMessageAsync(chatId, messageId);
                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "delete":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }
                        if (activity.IsTracking)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                            "⚙️ Вы удалили отслеживаемую активность.",
                            showAlert: true);
                        }
                        try
                        {
                            //Удаление активности
                            DB.DeleteActivity(chatId, actNumber);

                            await botClient.SendTextMessageAsync(chatId,
                            text: $"🗑 {activity.Name}: активность удалена");

                            //Отправка списка активностей
                            InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                            Message messageAct = await botClient.SendTextMessageAsync(chatId,
                            text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                            replyMarkup: activityKeyboard);

                            //Удаление прошлой клавиатуры c активностями
                            int tempMessageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                            InlineKeyboard.RemoveMessageId(chatId);
                            await botClient.DeleteMessageAsync(chatId, tempMessageId);

                            //Удаление клавиатуры aboutact
                            await botClient.DeleteMessageAsync(chatId, messageId);

                            InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗑 Активность удалена");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n"
                            + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        }

                        break;
                    }

                case "deleteInArchive":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        List<Activity> archive = DB.GetActivityList(chatId, getOnlyArchived: true);
                        Activity? activity = archive.FirstOrDefault(a => a.Number == actNumber);

                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }
                        try
                        {
                            //Удаление активности
                            DB.DeleteActivity(chatId, actNumber);

                            await botClient.SendTextMessageAsync(chatId,
                            text: $"🗑 {activity.Name}: активность удалена");

                            //удаление клавиатуры aboutact
                            await botClient.DeleteMessageAsync(chatId, messageId);

                            archive = DB.GetActivityList(chatId, getOnlyArchived: true);
                            InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(archive);

                            if (archive.Count != 0)
                            {
                                await botClient.SendTextMessageAsync(chatId,
                               "🗂 Архив\n\n" +
                               "ℹ️ Эти активности в данный момент скрыты из главного меню, и их отслеживание недоступно. " +
                               "Вы можете восстановить их или удалить, нажав на нужную активность.",
                               replyMarkup: archivedActivityKeyboard);
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId,
                                "🗂 Архив пуст\n\n" +
                                "ℹ️ Когда вы захотите временно скрыть некоторые активности из главного меню и не отслеживать их, " +
                                "вы можете добавить их в архив, и они будут храниться здесь.");
                            }

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка: " + chatId + " " + ex.Message);
                            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n"
                            + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        }
                        break;
                    }

                case "start_":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);
                        if (activity == null)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                            //Удаление клавиатуры с невозможным действием
                            await botClient.DeleteMessageAsync(chatId, messageId);
                            break;
                        }
                        if (activity.IsTracking)
                        {
                            await Console.Out.WriteLineAsync($"{chatId}: Активность уже начата");
                            break;
                        }
                        //Начать таймер активности
                        Activity.Start(chatId, actNumber);

                        //Обновление клавиатуры
                        await botClient.EditMessageReplyMarkupAsync(chatId, messageId,
                            replyMarkup: InlineKeyboard.Main(DB.GetActivityList(chatId))
                        );

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Тайм-трекер запущен");
                        break;
                    }
                case "stop_":
                    {
                        try
                        {
                            if (User.GetState(chatId).state == User.State.Deleting)
                                break;

                            int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                            Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                            //Удаление прошлого списка активностей
                            if (messageId != 0)
                                await botClient.DeleteMessageAsync(chatId, messageId);

                            if (activity == null)
                            {
                                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Действие невозможно");

                                //Удаление клавиатуры с невозможным действием
                                await botClient.DeleteMessageAsync(chatId, messageId);
                                break;
                            }

                            if (!activity.IsTracking)
                            {
                                await Console.Out.WriteLineAsync($"{chatId}: Активность уже остановленна");
                                break;
                            }

                            User.SetState(chatId, User.State.Deleting);

                            //Остановить таймер активности
                            int result = Activity.Stop(chatId, actNumber);
                            activity.TotalTime = result;

                            //Отправка списка активностей
                            Message messageAct = await botClient.SendTextMessageAsync(chatId,
                            text: "⏱ Вот все Ваши активности. Нажмите на ту, которую хотите изменить или узнать подробности.",
                            replyMarkup: InlineKeyboard.Main(DB.GetActivityList(chatId)));

                            InlineKeyboard.SetMessageIdForDelete(chatId, messageAct.MessageId);

                            await botClient.SendTextMessageAsync(chatId,
                                $"🏁 {activity.Name}: затрачено {activity.TotalTimeToString()}");

                            User.ResetState(chatId);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Ошибка: "+ chatId + " " + e.Message);
                        }

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Тайм-трекер остановлен");
                        break;
                    }
            }
        }

        //Метод если появляется ошибка
        async static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {
            Console.WriteLine("Произошла ошибка: "+ exception.Message);
        }
    }
}