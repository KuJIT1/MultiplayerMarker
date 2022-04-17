using Microsoft.EntityFrameworkCore;
using MultiplayerMarker.DbModel;
using MultiplayerMarker.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MultiplayerMarker.Logger
{
    public class DbUserActionLogger
    {
        private readonly IServiceProvider serviceProvider;

        private readonly int limit = 20;

        private object lockObject = new object();

        private readonly ApplicationContext dbContext;

        public DbUserActionLogger(ApplicationContext dbContext)
        {
            this.dbContext = dbContext;
        }

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
