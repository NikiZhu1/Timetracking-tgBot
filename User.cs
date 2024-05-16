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
        //ключом служит id юзера, значением служит пара из состояния и числа (или null)
        //Число для облегчения работы с состояниями,
        //чтобы не создавать состояния для изменения активности 1, 2, 3...
        //создадим одно состояние и добавляем число, показывая с какой акт. работаем

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
    }
}
