namespace MultiplayerMarker.Entities
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Пользователь
    /// </summary>
    public class User
    {
        /// <summary>
        /// Делегат для обработки события изменения списка меток
        /// </summary>
        /// <param name="changePathType">Тип события</param>
        /// <param name="user">Этот Пользователь</param>
        /// <param name="mark">Добавленная/удалённая метка</param>
        public delegate void PathChanged(ChangePathType changePathType, User user, Mark mark);

        /// <summary>
        /// Событие изменения метки
        /// </summary>
        public event PathChanged PathChangedEvent;

        private readonly List<Mark> marks = new List<Mark>();

        private int nextMarkId = 0;

        public string UserId { get; }

        public string Name { get; }

        // TODO: интегрировать в serverbased
        /// <summary>
        /// Иконка меток
        /// </summary>
        public string MarkIcon { get; set; }

        /// <summary>
        /// Иконка путника
        /// </summary>
        public string WayfarerIcon { get; set; }

        // TODO: убрать свойство?
        /// <summary>
        /// Список меток
        /// </summary>
        public List<Mark> Marks => this.marks;

        /// <summary>
        /// Путник
        /// </summary>
        public Wayfarer Wayfarer { get; } = new Wayfarer();

        public User(string name, string userId)
        {
            this.Name = name;
            this.UserId = userId;
        }

        /// <summary>
        /// Добавить метку
        /// </summary>
        /// <param name="mark">Метка</param>
        public void AddMark(Mark mark)
        {
            mark.Id = nextMarkId++;
            this.marks.Add(mark);
            this.PathChangedEvent?.Invoke(ChangePathType.Added, this, mark);
        }

        /// <summary>
        /// Удалить метку
        /// </summary>
        /// <param name="markId">Идентификатор метки</param>
        /// <returns>false, если метки с таким идентификатором не найдено</returns>
        public bool RemoveMark(int markId)
        {
            var mark = this.marks.FirstOrDefault(m => m.Id == markId);
            return this.RemoveMark(mark);
        }

        private bool RemoveMark(Mark mark)
        {
            if (mark != null)
            {
                this.marks.Remove(mark);
                this.PathChangedEvent?.Invoke(ChangePathType.Removed, this, mark);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Очистить все метки
        /// </summary>
        public void ClearMarks()
        {
            this.marks.Clear();
            this.PathChangedEvent?.Invoke(ChangePathType.RemovedAll, this, null);
        }
    }

    /// <summary>
    /// Типы события изменений списка меток
    /// </summary>
    public enum ChangePathType
    {
        Added,
        Removed,
        RemovedAll
    }
}
