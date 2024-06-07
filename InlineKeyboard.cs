using Telegram.Bot.Types.ReplyMarkups;

namespace Timetracking_HSE_Bot
{
    public class InlineKeyboard
    {
        //Главная клавиатура со списком активностей
        public static InlineKeyboardMarkup Main(List<Activity> activityList)
        {
            List<InlineKeyboardButton[]> rows = new()
            {
                new[] {InlineKeyboardButton.WithCallbackData("Добавить активность", "add_activity")}
            };

            foreach (Activity activity in activityList)
            {
                InlineKeyboardButton activityButton = new("");
                InlineKeyboardButton statusButton = new("");

                if (!activity.InArchive)
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

        //Клавиатура для вывода месяцев
        public static InlineKeyboardMarkup Months()
        {
            var monthKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("❄️Январь", $"month_01"), InlineKeyboardButton.WithCallbackData("❄️Февраль", $"month_02"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("🍀Март", $"month_03"), InlineKeyboardButton.WithCallbackData("🍀Апрель", $"month_04"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("🍀Май", $"month_05"), InlineKeyboardButton.WithCallbackData("☀️Июнь ", $"month_06"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("☀️Июль", $"month_07"), InlineKeyboardButton.WithCallbackData("☀️Август", $"month_08"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("🍁Сентябрь", $"month_09"), InlineKeyboardButton.WithCallbackData("🍁Октябрь", $"month_10"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("🍁Ноябрь", $"month_11"), InlineKeyboardButton.WithCallbackData("❄️Декабрь", $"month_12"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("◀️Назад ", $"month_13"),
                },
            });

            return monthKeyboard;
        }

        //Клавиатора с выбором типа статистики
        public static InlineKeyboardMarkup StaticticType()
        {
            var statisticKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("За всё время", $"statistic_1"),
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

            return statisticKeyboard;
        }

        //Клавиатура в AboutAct
        public static InlineKeyboardMarkup ChangeActivity(int actNumber)
        {
            InlineKeyboardMarkup changeActKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("✏️ Изменить", $"rename{actNumber}"), InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"delete{actNumber}"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithCallbackData("🗂 Отправить в архив", $"archive{actNumber}"),
                },
            });

            return changeActKeyboard;
        }

        //Клавиатура в /help
        public static InlineKeyboardMarkup Help()
        {
            InlineKeyboardMarkup technicalSupportKeyboard = new(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("Техническая поддержка", "https://forms.gle/p87wy2ETYGC7WDMdA"),
                },
                new InlineKeyboardButton[]
                {
                    InlineKeyboardButton.WithUrl("Сотрудничество", "https://forms.gle/9W8C3epktot9inR66"),
                }
            }
            );

            return technicalSupportKeyboard;
        }

        //Клавиатура с архивированными активностями
        public static InlineKeyboardMarkup Archive(List<Activity> archivedActivity)
        {
            List<InlineKeyboardButton[]> rows = new();

            foreach (Activity activity in archivedActivity)
            {
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{activity.Name}", $"aboutArchive{activity.Number}") });
            }

            return new InlineKeyboardMarkup(rows);
        }

        //Клавиатура в AboutAct
        public static InlineKeyboardMarkup ChangeArchive(int actNumber)
        {
            var changeActKeyboard = new InlineKeyboardMarkup(
            new List<InlineKeyboardButton[]>()
            {
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("📤 Восстановить", $"recover{actNumber}"), InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"deleteInArchive{actNumber}"),
                },
                new InlineKeyboardButton[]
                {
                        InlineKeyboardButton.WithCallbackData("◀️ Назад в архив", "backToArchive"),
                },
            });

            return changeActKeyboard;
        }

        //Словаь в котором хранятся состояния для удаления
        private static readonly Dictionary<long, int> messageIdsForDelete = new();

        //Записать message.id для удаления
        public static void SetMessageIdForDelete(long userId, int messageId)
        {
            messageIdsForDelete[userId] = messageId;
        }

        //Получить message.id для удаления
        public static int GetMessageIdForDelete(long userId)
        {
            if (messageIdsForDelete.TryGetValue(userId, out var messageId))
            {
                return messageId;
            }

            return 0;
        }

        //Удалить message.id после удаленния
        public static void RemoveMessageId(long userId)
        {
            messageIdsForDelete.Remove(userId);
        }
    }
}

