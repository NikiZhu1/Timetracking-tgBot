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
                DB.Registration(chatId, message.Chat.Username);

                //Инициализация инлайн клавиатуры
                InlineKeyboardMarkup activityKeyboard = BuildNewKeyboard(DB.GetActivityList(chatId));

                //Вывод клавиатуры с сообщением
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "⏱ Вот все ваши активности. Нажми на ту, которую хочешь изменить или узнать подробности.",
                    replyMarkup: activityKeyboard);
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

        //Обработка: КАЛЛБЭКИ ОТ ИНЛАЙН-КНОПОК
        static async Task CallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            int messageId = callbackQuery.Message.MessageId;
            long chatId = callbackQuery.Message.Chat.Id;
            //List<string> activityList = DB.GetActivityList(chatId);
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
                        activityList = DB.GetActivityList(chatId, true);

                        string textWithStatistic = "";
                        foreach (Activity activity in activityList)
                        {
                            double result = DB.GetStatistic(chatId, activity.Number, 6);
                            if (result != 0)
                            {
                                int hours = (int)result / 3600;
                                int min = ((int)(result - hours * 3600)) / 60;
                                double sec = result - 3600 * hours - 60 * min;
                                textWithStatistic += $"{activity.Name}: {hours} ч. {min} мин. {sec} сек.\n"; //убрать нули когда-нибудь
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

                        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
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