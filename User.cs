namespace Timetracking_HSE_Bot
{
    public class User
    {
        //Состояния пользователя
        public enum State
        {
            None,
            WaitMessageForChangeAct, // Ожидает сообщения для изменения названия активности
            WaitMessageForAddAct, // Ожидает сообщения для изменения названия активности
        }

        public static List<string> ActivityList { get; set; }

        public User(long chatId)
        {

        }

        //Словаь в котором хранятся состояния пользователей
        private static readonly Dictionary<long, (State state, int? actNumber)> userStates = new();

        //Установить состояние
        public static void SetState(long userId, State state, int? actNumber = null)
        {
            userStates[userId] = (state, actNumber);
        }

        //Получить текущее состояние
        public static (State state, int? actNumber) GetState(long userId)
        {
            if (userStates.TryGetValue(userId, out var userInfo))
            {
                return userInfo;
            }

            return (State.None, null);
        }

        //Сбросить состояние
        public static void ResetState(long userId)
        {
            SetState(userId, State.None);
        }

        ////Словаь в котором хранятся состояния пользователей
        //private static readonly Dictionary<long, int> messageIdsForDelete = new();

        ////Записать message.id для удаления
        //public static void SetMessageIdForDelete(long userId, int messageId)
        //{
        //    messageIdsForDelete[userId] = messageId;
        //}

        ////Записать message.id для удаления
        //public static int GetMessageIdForDelete(long userId)
        //{
        //    if (messageIdsForDelete.TryGetValue(userId, out var messageId))
        //    {
        //        return messageId;
        //    }

        //    return 0;
        //}

        //public static void RemoveMessageId(long userId)
        //{
        //    messageIdsForDelete.Remove(userId);
        //}
    }
}
