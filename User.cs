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
            Deleting
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
            //SetState(userId, State.None);
            userStates.Remove(userId);
        }
    }
}
