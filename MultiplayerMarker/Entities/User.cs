using System.Collections.Generic;
using System.Linq;

namespace MultiplayerMarker.Entities
{
    public class User
    {
        public delegate void PathChanged(ChangePathType changePathType, User user, Mark mark);

        public event PathChanged PathChangedEvent;

        private readonly List<Mark> marks = new List<Mark>();

        private int nextMarkId = 0;

        public string UserId { get; }

        public string Name { get; }

        // TODO: интегрировать в serverbased
        public string MarkIcon { get; set; }

        public string WayfarerIcon { get; set; }

        // TODO: убрать
        public List<Mark> Marks => this.marks;

        public Wayfarer Wayfarer { get; } = new Wayfarer();

        public User(string name, string userId)
        {
            this.Name = name;
            this.UserId = userId;
        }

        public void AddMark(Mark mark)
        {
            mark.Id = nextMarkId++;
            this.marks.Add(mark);
            this.PathChangedEvent?.Invoke(ChangePathType.Added, this, mark);
        }

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

        public void ClearMarks()
        {
            this.marks.Clear();
            this.PathChangedEvent?.Invoke(ChangePathType.RemovedAll, this, null);
        }
    }

    public enum ChangePathType
    {
        Added,
        Removed,
        RemovedAll
    }
}
