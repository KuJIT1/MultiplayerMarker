namespace MultiplayerMarker.Logger
{
    using System;
    using System.Linq;

    using MultiplayerMarker.DbModel;
    using MultiplayerMarker.Entities;

    /// <summary>
    /// Класс для записи действий пользователя в БД
    /// </summary>
    public class DbUserActionLogger
    {
        /// <summary>
        /// Количество действий, которое храниться в БД
        /// </summary>
        private readonly int limit = 20;

        private object lockObject = new object();

        private readonly ApplicationContext dbContext;

        public DbUserActionLogger(ApplicationContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Обработчик события изменения списка меток пользователя
        /// TODO: выделить обработку в метод
        /// </summary>
        /// <param name="changePathType">Тип события</param>
        /// <param name="user">Пользователь</param>
        /// <param name="mark">Добавленная/удалённая метка</param>
        public void PathChanged(ChangePathType changePathType, User user, Mark mark)
        {
            var actionType = this.GetUserActionType(changePathType);
            if (actionType == null)
            {
                return;
            }

            var userAction = new UserActionDbLog
            {
                ActionType = actionType.Value,
                UserName = user.Name,
                X = mark?.X,
                Y = mark?.Y,
                DateTime = DateTime.Now
            };

            // т.к. dbContext singleton, кажется, нужна блокировка
            lock (lockObject)
            {
                this.dbContext.UserActions.Add(userAction);
                var count = this.dbContext.UserActions.Count() + 1;
                if (count >= this.limit)
                {
                    this.dbContext.UserActions.OrderBy(x => x.Id).Take(Math.Max(0, count - this.limit));
                    this.dbContext.UserActions.RemoveRange(this.dbContext.UserActions.OrderBy(x => x.Id).Take(Math.Max(0, count - this.limit)));
                }

                this.dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Конвертер типа события в тип действия для записи в БД
        /// </summary>
        /// <param name="changePathType"></param>
        /// <returns></returns>
        private UserActionType? GetUserActionType(ChangePathType changePathType)
        {
            switch (changePathType)
            {
                case ChangePathType.Added:
                    return UserActionType.MarkAdded;
                case ChangePathType.Removed:
                    return UserActionType.MarkRemoved;
                case ChangePathType.RemovedAll:
                    return UserActionType.ClearMarks;
                default:
                    break;
            }

            return null;
        }
    }
}
