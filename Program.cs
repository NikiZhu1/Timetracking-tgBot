using System.Diagnostics;
using System.Net.NetworkInformation;
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
                    InlineKeyboardMarkup activityKeyboard = BuildNewKeyboard(DB.GetActivityList(chatId));

                    //Вывод клавиатуры с сообщением
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "⏱ Вот все ваши активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                        replyMarkup: activityKeyboard);
                }
                catch (Exception ex)
                {
                    await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением к базе данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                }


            }

            if (message.Text != null && message.Text == "/help")
            {
                InlineKeyboardMarkup technicalSupportKeyboard = new(
                   new InlineKeyboardButton[]
                   {
                        InlineKeyboardButton.WithUrl("Техническая поддержка", "https://forms.gle/p87wy2ETYGC7WDMdA"),
                   }
                );

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
                replyMarkup: technicalSupportKeyboard);
            }

            //Изменение названия активности
            if (userInfo.state == User.State.WaitMessageForChangeAct && userInfo.actNumber.HasValue)
            {
                bool isNonrepeatingName = Activity.IsNotRepeatingName(message.Text, chatId, userInfo.actNumber);
                if (!isNonrepeatingName)
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
                    // Пользователь ввел текст, обновляем название активности
                    try
                    {
                        DB.UpdateActivityName((int)userInfo.actNumber, message.Text, chatId);

                        //Удаление прошлой клавиатуры
                        int messageId = User.GetMessageIdForDelete(chatId);
                        User.RemoveMessageId(chatId);
                        await botClient.DeleteMessageAsync(chatId, messageId);

                        // Сбросить состояние пользователя
                        User.ResetState(chatId);

                        InlineKeyboardMarkup activityKeyboard = BuildNewKeyboard(DB.GetActivityList(chatId));

                        // Отправляем сообщение
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                            replyMarkup: activityKeyboard);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением к базе данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                }
            }

            //Добавление новой активности
            if (userInfo.state == User.State.WaitMessageForAddAct)
            {
                bool isNonrepeatingName = Activity.IsNotRepeatingName(message.Text, chatId);
                if (!isNonrepeatingName)
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
                    // Пользователь ввёл название, добавляем активность
                    try
                    {
                        DB.AddActivity(chatId, message.Text);

                        //Удаление прошлой клавиатуры
                        int messageId = User.GetMessageIdForDelete(chatId);
                        User.RemoveMessageId(chatId);
                        await botClient.DeleteMessageAsync(chatId, messageId);

                        // Сбросить состояние пользователя
                        User.ResetState(chatId);

                        InlineKeyboardMarkup activityKeyboard = BuildNewKeyboard(DB.GetActivityList(chatId));

                        await botClient.SendTextMessageAsync(
                          chatId: chatId,
                          text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                          replyMarkup: activityKeyboard);
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением к базе данных: {ex.Message}.\n" +
                        $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                    }
                }
            }
        }

        static async void ShowStatistic(long chatId, int month, DateTime today)
        {
            try
            {
                List<Activity>  activityList = DB.GetActivityList(chatId, true);
                string textWithStatistic = "";
                foreach (Activity activity in activityList)
                {
                    int seconds = DB.GetStatistic(chatId, activity.Number, month, today);

                    if (seconds != 0)
                    {
                        TimeSpan result = TimeSpan.FromSeconds(seconds);
                        int hour = result.Hours;
                        int min = result.Minutes;
                        int sec = result.Seconds;

                        //Только секунды
                        if (min == 0)
                            textWithStatistic += $"{activity.Name}: {sec} сек.\n";

                        //Только минуты с секундами
                        else if (hour == 0 && min != 0)
                            textWithStatistic += $"{activity.Name}: {min} мин. {sec} сек.\n";

                        else textWithStatistic += $"{activity.Name}: {hour} ч. {min} мин. {sec} сек.\n";
                    }
                }

                Console.WriteLine($"{chatId}: Получение статистики");
                if (textWithStatistic != "")
                {
                    await botClient.SendTextMessageAsync(
                          chatId: chatId,
                          text: textWithStatistic);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                          chatId: chatId,
                          text: "У вас пока нет записей о затраченном времени\n" +
                          "🚀 Запускай таймер и можешь отследить свой прогресс!");
                }
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением к базе данных: {ex.Message}.\n" +
                     $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
            }
        }

        //Строит клавиатуру по списку активностей
        public static InlineKeyboardMarkup BuildNewKeyboard(List<Activity> activityList)
        {
            List<InlineKeyboardButton[]> rows = new()
            {
                new[] {InlineKeyboardButton.WithCallbackData("Добавить активность", "add_activity")}
            };

            foreach (Activity activity in activityList)
            {
                InlineKeyboardButton activityButton = new("");
                InlineKeyboardButton statusButton = new("");

                if (!activity.IsEnded)
                {
                    // Создаем кнопки для активности
                    activityButton = activity.IsTracking
                        ? InlineKeyboardButton.WithCallbackData($"⏱️ {activity.Name}", $"aboutAct{activity.Number}")
                        : InlineKeyboardButton.WithCallbackData($"{activity.Name}", $"aboutAct{activity.Number}");
                    statusButton = activity.IsTracking
                        ? InlineKeyboardButton.WithCallbackData("⏹ СТОП", $"stop_{activity.Number}")
                        : InlineKeyboardButton.WithCallbackData("❇️ СТАРТ", $"start_{activity.Number}");
                }

                rows.Add(new[] { activityButton, statusButton });
            }

            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("Статистика активностей", "statistic") });

            return new InlineKeyboardMarkup(rows);
        }

        //Строит клавиатуру для вывода месяцев
        public static InlineKeyboardMarkup BuildMonthKeyboard(long chatId)
        {
            var monthKeyboard = new InlineKeyboardMarkup(
                       new List<InlineKeyboardButton[]>()
                       {
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("Январь", $"month_01"), InlineKeyboardButton.WithCallbackData("Февраль", $"month_02"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("Март", $"month_03"), InlineKeyboardButton.WithCallbackData("Апрель", $"month_04"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("Май", $"month_05"), InlineKeyboardButton.WithCallbackData("Июнь ", $"month_06"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("Июль", $"month_07"), InlineKeyboardButton.WithCallbackData("Август", $"month_08"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("Сентябрь", $"month_09"), InlineKeyboardButton.WithCallbackData("Октябрь", $"month_10"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("Ноябрь", $"month_11"), InlineKeyboardButton.WithCallbackData("Декабрь", $"month_12"),
                            },
                       });

            return monthKeyboard;
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
                        User.SetMessageIdForDelete(chatId, messageId);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }
                
                case "statistic":
                    {
                        //try
                        //{
                        //    activityList = DB.GetActivityList(chatId, true);
                        //    DateTime today = DateTime.Now.Date;
                        //    string textWithStatistic = "";
                        //    foreach (Activity activity in activityList)
                        //    {                        
                        //        //int seconds = DB.GetStatistic(chatId, activity.Number);
                        //        int seconds = DB.GetStatisticсссс(chatId, activity.Number, 0, today);

                        //        if (seconds != 0)
                        //        {
                        //            TimeSpan result = TimeSpan.FromSeconds(seconds);
                        //            int hour = result.Hours;
                        //            int min = result.Minutes;
                        //            int sec = result.Seconds;

                        //            //Только секунды
                        //            if (min == 0)
                        //                textWithStatistic += $"{activity.Name}: {sec} сек.\n";

                        //            //Только минуты с секундами
                        //            else if (hour == 0 && min != 0)
                        //                textWithStatistic += $"{activity.Name}: {min} мин. {sec} сек.\n";

                        //            else textWithStatistic += $"{activity.Name}: {hour} ч. {min} мин. {sec} сек.\n";
                        //        }
                        //    }

                        //    Console.WriteLine($"{chatId}: Получение статистики");
                        //    if (textWithStatistic != "")
                        //    {
                        //        await botClient.SendTextMessageAsync(
                        //              chatId: chatId,
                        //              text: textWithStatistic);
                        //    }
                        //    else
                        //    {
                        //        await botClient.SendTextMessageAsync(
                        //              chatId: chatId,
                        //              text: "У вас пока нет записей о затраченном времени\n" +
                        //              "🚀 Запускай таймер и можешь отследить свой прогресс!");
                        //    }

                        //    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        //}
                        //catch (Exception ex)
                        //{
                        //    await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением к базе данных: {ex.Message}.\n" +
                        //         $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        //}


                        var statisticKeyboard = new InlineKeyboardMarkup(
                        new List<InlineKeyboardButton[]>()
                        {
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("За весь период", $"statistic_1"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("За определенный месяц", $"statistic_2"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("За последние 7 дней", $"statistic_3"),
                            },
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("За этот день", $"statistic_4"),
                            },
                        });

                        await botClient.SendTextMessageAsync(chatId,
                            text: "Выберете, в каком формате Вы хотите получить статистику",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: statisticKeyboard);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }

                case "statistic_":
                    {
                        int statisticType = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        if (statisticType == 1)
                            ShowStatistic(chatId, 0, default);
                        else if (statisticType == 2)
                        {
                            InlineKeyboardMarkup monthKeyboard = BuildMonthKeyboard(chatId);
                           await botClient.SendTextMessageAsync(chatId,
                           text: "Выберете месяц, за который Вы хотите получить статистику активностей",
                           parseMode: ParseMode.Markdown,
                           replyMarkup: monthKeyboard);
                        }
                        else if (statisticType == 4)
                        {
                            DateTime today = DateTime.Now.Date;
                            ShowStatistic(chatId, 0, today);
                        }
                        break;
                    }
                case "month_":
                    {
                        int monthNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        ShowStatistic(chatId, monthNumber, default);
                        break;
                    }

                case "aboutAct":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));

                        string status = "";
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity != null)
                        {
                            // Теперь, когда у вас есть активность, вы можете проверить, отслеживается ли она
                            status = activity.IsTracking ? ": Отслеживается ⏱" : "";
                        }

                        //Получение message.id для последующего удаления
                        User.SetMessageIdForDelete(chatId, messageId);

                        var changeActKeyboard = new InlineKeyboardMarkup(
                        new List<InlineKeyboardButton[]>()
                        {
                            new InlineKeyboardButton[]
                            {
                                 InlineKeyboardButton.WithCallbackData("✏️ Изменить", $"rename{actNumber}"), InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"delete{actNumber}"),
                            },
                        });

                        await botClient.SendTextMessageAsync(chatId,
                            text: $"{activity.Name}{status} \n\n" +
                            $"Ты можешь изменить название активности или удалить ее",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: changeActKeyboard);

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
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

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        break;
                    }
                case "delete":
                    {
                        int actNumber = int.Parse(Regex.Replace(callbackQuery.Data, @"\D", ""));
                        Activity? activity = activityList.FirstOrDefault(a => a.Number == actNumber);

                        if (activity.IsTracking)
                        {
                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                            "⚙️ Вы удалили отслеживаемую активность.",
                            showAlert: true);
                        }

                        try
                        {
                            DB.EndActivity(chatId, actNumber);

                            InlineKeyboardMarkup activityKeyboard = BuildNewKeyboard(DB.GetActivityList(chatId));

                            await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "⏱ Вот все твои активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                            replyMarkup: activityKeyboard);

                            //Удаление прошлой клавиатуры
                            messageId = User.GetMessageIdForDelete(chatId);
                            User.RemoveMessageId(chatId);
                            await botClient.DeleteMessageAsync(chatId, messageId);

                            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗑 Активность удалена");
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendTextMessageAsync(chatId, $"‼ Возникла ошибка с подключением к базе данных: {ex.Message}.\n"
                            + $"Пожалуйста, свяжитесь с нами через техническую поддержку для устранения ошибки");
                        }

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
                            replyMarkup: BuildNewKeyboard(DB.GetActivityList(chatId))
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
                        replyMarkup: BuildNewKeyboard(DB.GetActivityList(chatId)));

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