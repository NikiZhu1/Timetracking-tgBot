using System.Globalization;
using System.Text.RegularExpressions;
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
        const string token = "6761464907:AAHFMCFJJaRlEvt1obDsgYgqgliWw9mdyHg";

        //Старт бота
        static async Task Main(string[] args)
        {
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
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "У вас есть несколько предустановленых активностей: работа, спорт, отдых.\n" +
                "*Старт* — запускается таймер активности,\n" +
                "*Стоп* — таймер активности останавливается.\n" +
                "\n" +
                "📊 Активности могут отслеживатся одновременно\n" +
                "⚠️ Главное не забывайте их останавливать\n" +
                "\n" +
                "Узнать больше о функционале бота — /help",
                parseMode: ParseMode.Markdown);

                //Регистрация пользователя в БД
                try
                {
                    DB.Registration(chatId, message.Chat.Username);

                    //Инициализация инлайн клавиатуры
                    InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                    //Вывод клавиатуры с сообщением
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⏱ Вот все ваши активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                        replyMarkup: activityKeyboard);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                }
            }

            if (message.Text == "/Archive")
            {
                //Инициализация инлайн клавиатуры
                InlineKeyboardMarkup archivedActivityKeyboard = InlineKeyboard.Archive(DB.GetActivityList(chatId, true, true));

                //Вывод клавиатуры с сообщением
                await botClient.SendTextMessageAsync(chatId, "Вот все заархивированные активности. Вы можете удалить их или восстановить",
                    replyMarkup: archivedActivityKeyboard);
            }

            if (message.Text != null && message.Text == "/help")
            {
                await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: " ⏳ Бот Ловец времени поможет Вам отслеживать время выполнения Ваших задач.\n" +
                "\n" +
                "Чтобы запустить бота выбере команду 'Начать работу с ботом' в меню чата или отправьте боту '/start' \n" +
                "\n" +
                "  ⏱ УПРАВЛЕНИЕ ТАЙМ-ТРЕКЕРОМ   \n" +
                "Чтобы начать работу с трекером, нажмите кнопку 'Начать', расположенную напротив той активности, которую Вы хотите запустить. \n" +
                "Бот отправит Вам новую клавиатуру. Чтобы управлять трекером, нажмите кнопки 'Старт' или 'Стоп'.\n" +
                "\n" +
                "  📊 СТАТИСТИКА АКТИВНОСТЕЙ   \n" +
                "При завершении задачи бот отправит Вам время ее выполнения. Полную статистику активностей Вы можете посмотреть, нажав на кнопку 'Статистика активностей'. \n" +
                "Там Вы сможете посмотреть суммарное время выполнения Ваших активностей, которые были запущены хотя бы раз.\n" +
                "\n" +
                "  ➕ ДОБАВЛЕНИЕ АКТИВНОСТЕЙ  \n" +
                "Вы можете добавить новую активность при нажатии на кнопку 'Добавить активность'. Введите название новой задачи, и бот обновит клавиатуру активностей.\n" +
                "Вы можете добавить 10 активностей\n" +
                "\n" +
                "  ✏️ УДАЛЕНИЕ АКТИВНОСТЕЙ И ИЗМЕНЕНИЕ ИХ НАЗВАНИЙ   \n" +
                "Вы можете изменить название активности или удалить ее, нажав на кнопку с необходимой задачей. \n" +
                "В качестве названия активности Вы можете использовать алфавит любого языка или эмодзи.\n",
                parseMode: ParseMode.Markdown,
                replyMarkup: InlineKeyboard.Help());
            }

            ////Изменение названия активности - старая версия
            //if (userInfo.state == User.State.WaitMessageForChangeAct && userInfo.actNumber.HasValue)
            //{
            //    bool isNonrepeatingName = Activity.IsNotRepeatingName(message.Text, chatId, userInfo.actNumber);
            //    if (!isNonrepeatingName)
            //    {
            //        // Если уже есть активность с таким названием
            //        await botClient.SendTextMessageAsync(
            //            chatId: chatId,
            //            text: "❗ У вас уже есть активность с таким названием.");
            //    }
            //    else if (message.Text == null)
            //    {
            //        // Если пользователь не ввел текст, отправляем предупреждение
            //        await botClient.SendTextMessageAsync(
            //            chatId: chatId,
            //            text: "❗ В качестве названия введите текст или смайлик.");
            //    }
            //    else
            //    {
            //        // Пользователь ввел текст, обновляем название активности
            //        try
            //        {
            //            DB.UpdateActivityName((int)userInfo.actNumber, message.Text, chatId);

            //            //Удаление прошлой клавиатуры
            //            //int messageId = User.GetMessageIdForDelete(chatId);
            //            //User.RemoveMessageId(chatId);
            //            int messageId = InlineKeyboard.GetMessageIdForDelete(chatId);
            //            InlineKeyboard.RemoveMessageId(chatId);
            //            await botClient.DeleteMessageAsync(chatId, messageId);

            //            // Сбросить состояние пользователя
            //            User.ResetState(chatId);

            //            InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

            //            // Отправляем сообщение
            //            await botClient.SendTextMessageAsync(
            //                chatId: chatId,
            //                text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
            //                replyMarkup: activityKeyboard);
            //        }
            //        catch (Exception ex)
            //        {
            //            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" +
            //            $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
            //        }
            //    }
            //}


            //Изменение названия активности - новая версия (проверка что такое название есть в архиве)
            if (userInfo.state == User.State.WaitMessageForChangeAct && userInfo.actNumber.HasValue)
            {
                int isDeletedName = Activity.IsUniqueName(message.Text, chatId);
                if (isDeletedName == 0)
                {
                    // Если уже есть активность с таким названием
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❗ У вас уже есть активность с таким названием.");
                }
                else if (isDeletedName == -1)
                {
                    await botClient.SendTextMessageAsync(chatId, "У Вас есть активность с таким названием в архиве.");  
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
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                            replyMarkup: activityKeyboard);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                }
            }

            //Добавление новой активности/восстановление активностей
            if (userInfo.state == User.State.WaitMessageForAddAct)
            {
                int isDeletedName = Activity.IsUniqueName(message.Text, chatId);
                if (isDeletedName == 0)
                {
                    // Если уже есть активность с таким названием
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❗ У вас уже есть активность с таким названием.");
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

                        //Удаление прошлой клавиатуры
                        int messageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                        InlineKeyboard.RemoveMessageId(chatId);
                        await botClient.DeleteMessageAsync(chatId, messageId);

                        // Сбросить состояние пользователя
                        User.ResetState(chatId);

                        InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                        await botClient.SendTextMessageAsync(
                          chatId: chatId,
                          text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                          replyMarkup: activityKeyboard);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
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
                      text: "У вас пока нет записей об активностях за этот период.\n" +
                      "🚀 Запускай таймер и можешь отследить свой прогресс!");
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
                        if (activityList.Count() == totalActivitiesCount)
                        {
                            Console.WriteLine($"{chatId}: Попытка добавления >{totalActivitiesCount} активностей");

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                            $"⚙️ Вы достигли максимального количества активностей ({totalActivitiesCount}).\n\n" +
                            "Пожалуйста, удалите одну из существующих, чтобы добавить новую.",
                            showAlert: true);
                            break;
                        }

                        //Изменение состояния пользователя
                        User.SetState(chatId, User.State.WaitMessageForAddAct);
                        await botClient.SendTextMessageAsync(chatId,
                        text: $"✏ Введите название для новой активности");

                        //Получение message.id для последующего удаления
                        //User.SetMessageIdForDelete(chatId, messageId);
                        InlineKeyboard.SetMessageIdForDelete(chatId, messageId);

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
                            //await botClient.SendTextMessageAsync(chatId, $"Статистика за весь период");
                            //ShowStatistic(chatId, 0, default);

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
                            //await botClient.SendTextMessageAsync(chatId, $"Статистика за последнюю неделю");
                            DateTime today = DateTime.Now.Date;
                            //ShowStatistic(chatId, 0, today);
                            SendStatictic(chatId, GetStatisticList(chatId, today.AddDays(-7), today), "Статистика за последнюю неделю:");

                            await botClient.DeleteMessageAsync(chatId, messageId);
                            Console.WriteLine($"{chatId}: Получение статистики за неделю");

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Статистика получена");
                        }

                        //За день
                        else if (statisticType == 4)
                        {
                            //await botClient.SendTextMessageAsync(chatId, $"Статистика за текущий день");
                            DateTime today = DateTime.Now.Date;
                            //ShowStatistic(chatId, 0, today, true);
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
                        //ShowStatistic(chatId, monthNumber, default);

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
                        DateTime secondDate = firstDate.AddMonths(1).AddDays(-1);
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

                        string status = "";
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity != null)
                        {
                            status = activity.IsTracking ? ": Отслеживается ⏱" : "";
                        }

                        //Получение message.id для последующего удаления
                        //User.SetMessageIdForDelete(chatId, messageId);
                        InlineKeyboard.SetMessageIdForDelete(chatId, messageId);


                        await botClient.SendTextMessageAsync(chatId,
                            text: $"{activity.Name}{status} \n\n" +
                            $"Ты можешь изменить название активности или удалить ее",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: InlineKeyboard.ChangeActivity(actNumber));

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }
                case "archive":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity.IsTracking)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                            "⚙️ Вы заархивировали отслеживаемую активность.",
                            showAlert: true);
                        }

                        try
                        {
                            //DB.EndActivity(chatId, actNumber);
                            DB.ArchiveActivity(chatId, actNumber);

                            InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId,true,true));

                            await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                            replyMarkup: activityKeyboard);

                            //удаление клавиатуры aboutact
                            await botClient.DeleteMessageAsync(chatId, messageId);

                            //Удаление прошлой клавиатуры c активностями
                            messageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                            InlineKeyboard.RemoveMessageId(chatId);
                            await botClient.DeleteMessageAsync(chatId, messageId);

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗑 Активность заархивирована");
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n"
                            + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        }
                        break;
                    }

                case "aboutArchive":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));

                        await botClient.SendTextMessageAsync(chatId, "Ты можешь изменить название активности или удалить ее",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: InlineKeyboard.ChangeArchive(actNumber));

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "recover":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));  
                        DB.UpdateDateEndStatus(chatId, actNumber); //обновляется дата окончания активности на null
                        InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                        await botClient.SendTextMessageAsync(
                          chatId: chatId,
                          text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                          replyMarkup: activityKeyboard);
                        break;
                    }
                case "rename":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        //Изменение состояния пользователя
                        User.SetState(chatId, User.State.WaitMessageForChangeAct, actNumber);

                        await botClient.SendTextMessageAsync(chatId,
                        text: $"Введите новое название для активности \"{activity.Name}\"");

                        //удаление клавиатуры aboutact
                        await botClient.DeleteMessageAsync(chatId, messageId);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }
                case "delete":
                    {
                        //int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        //Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        //if (activity.IsTracking)
                        //{
                        //    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                        //    "⚙️ Вы удалили отслеживаемую активность.",
                        //    showAlert: true);
                        //}

                        //try
                        //{
                        //    DB.EndActivity(chatId, actNumber);

                        //    InlineKeyboardMarkup activityKeyboard = InlineKeyboard.Main(DB.GetActivityList(chatId));

                        //    await botClient.SendTextMessageAsync(
                        //    chatId: chatId,
                        //    text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                        //    replyMarkup: activityKeyboard);

                        //    //удаление клавиатуры aboutact
                        //    await botClient.DeleteMessageAsync(chatId, messageId);

                        //    //Удаление прошлой клавиатуры c активностями
                        //    messageId = InlineKeyboard.GetMessageIdForDelete(chatId);
                        //    InlineKeyboard.RemoveMessageId(chatId);
                        //    await botClient.DeleteMessageAsync(chatId, messageId);

                        //    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗑 Активность удалена");
                        //}
                        //catch (Exception ex)
                        //{
                        //    await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением данных: {ex.Message}.\n"
                        //    + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        //}

                        break;
                    }
                case "start_":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity.IsTracking)
                        {
                            await Console.Out.WriteLineAsync($"{chatId}: Активность уже начата");
                            break;
                        }

                        Activity.Start(chatId, actNumber);

                        //Обновление клавиатуры
                        await botClient.EditMessageReplyMarkupAsync(
                            chatId: chatId,
                            messageId: messageId,
                            replyMarkup: InlineKeyboard.Main(DB.GetActivityList(chatId))
                        );

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Тайм-трекер запущен");
                        break;
                    }
                case "stop_":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (!activity.IsTracking)
                        {
                            await Console.Out.WriteLineAsync($"{chatId}: Активность уже остановленна");
                            break;
                        }

                        int result = Activity.Stop(chatId, actNumber);

                        int hours = (int)result / 3600;
                        int min = ((int)(result - hours * 3600)) / 60;
                        double sec = result - 3600 * hours - 60 * min;

                        await botClient.SendTextMessageAsync(chatId,
                        text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                        replyMarkup: InlineKeyboard.Main(DB.GetActivityList(chatId)));

                        await botClient.SendTextMessageAsync(chatId,
                            $"🏁 {activity.Name}: затрачено {hours} ч. {min} мин. {sec} сек");

                        await botClient.DeleteMessageAsync(chatId, messageId);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Тайм-трекер остановлен");
                        break;
                    }
            }
        }

        //Метод если появляется ошибка
        async static Task Error(ITelegramBotClient botClient, Exception exception, CancellationToken token)
        {

        }
    }
}