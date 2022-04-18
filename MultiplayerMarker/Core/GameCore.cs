namespace MultiplayerMarker.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MultiplayerMarker.Entities;
    using MultiplayerMarker.Logger;
    using MultiplayerMarker.Utils;

    /// <summary>
    /// Содержит информацию обо всех объектах игры: пользователях, метках. Управляет ими, связыает хаб, движок, БдЛоггер
    /// TODO: проработать потокобезопасность. Разделить по функциям.
    /// </summary>
    public class GameCore
    {
        private object userLock = new object();

        private List<User> users { get; set; } = new List<User>();

        private readonly Engine engine;

        private readonly DbUserActionLogger logger;

        public User[] Users => this.users.ToArray();

        public GameCore(Engine engine, DbUserActionLogger logger)
        {
            this.engine = engine;
            this.logger = logger;
        }

        /// <summary>
        /// Попытаться добавить пользователя по имени
        /// </summary>
        /// <param name="name">Имя пользователя</param>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        public TryAddUserActionResult TryAddUser(string name, string userId)
        {
            var result = new TryAddUserActionResult();
            if (string.IsNullOrEmpty(name))
            {
                result.Message += "Имя должно быть заполнено";
                return result;
            }

            lock (this.userLock)
            {
                if (this.users.Any(u => u.Name == name))
                {
                    result.Message += "Имя занято";
                    return result;
                }

                if (this.users.Any(u => u.UserId == userId && u.Name != name))
                {
                    result.Message += "Нельзя изменить имя";
                    return result;
                }

                var newUser = new User(name, userId);

                this.users.Add(newUser);
                result.Success = true;
                result.User = newUser;

                newUser.PathChangedEvent += this.engine.PathChanged;
                newUser.PathChangedEvent += this.logger.PathChanged;
            }

            return result;
        }

        // TODO: убрать побочные действия - модификацию mark. Обработка ошибок
        /// <summary>
        /// Добавить метку
        /// </summary>
        /// <param name="mark">Метка</param>
        /// <param name="userId">Идентификатор пользователя</param>
        public void AddMark(Mark mark, string userId)
        {
            var user = this.GetUser(userId);
            // TODO: параллельная обработка
            user.AddMark(mark);
        }

        /// <summary>
        /// Удалить метку
        /// </summary>
        /// <param name="markId">Идентификатор метки</param>
        /// <param name="userId">Идентификатор пользователя</param>
        public void RemoveMark(int markId, string userId)
        {
            var user = this.GetUser(userId);
            user.RemoveMark(markId);
        }

        /// <summary>
        /// Очистка меток
        /// </summary>
        /// <param name="userId">Идентификатор пользоватля</param>
        public void ClearMarks(string userId)
        {
            var user = this.GetUser(userId);
            user.ClearMarks();
        }

        private User GetUser(string userId)
        {
            return this.users.FirstOrDefault(u => u.UserId == userId) ?? throw new Exception("Пользователь не найден");
        }
    }
}
