using System;

namespace MultiplayerMarker.DbModel
{
    public class UserActionDbLog
    {
        public long Id { get; set; }

        public string UserName { get; set; }

        public UserActionType ActionType { get; set; }

        public int? X { get; set; }

        public int? Y { get; set; }

        public DateTime DateTime { get; set; }
    }

    public enum UserActionType
    {
        MarkAdded,
        MarkRemoved,
        ClearMarks
    }
}
