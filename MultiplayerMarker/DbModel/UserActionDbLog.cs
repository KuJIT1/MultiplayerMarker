namespace MultiplayerMarker.DbModel
{
    using System;

    /// <summary>
    /// Объект для записи в БД типа пользователя
    /// </summary>
    public class UserActionDbLog
    {
        public long Id { get; set; }

        public string UserName { get; set; }

        public UserActionType ActionType { get; set; }

        public int? X { get; set; }

        public int? Y { get; set; }

        public DateTime DateTime { get; set; }
    }

    /// <summary>
    /// Тип действия пользователя
    /// </summary>
    public enum UserActionType
    {
        MarkAdded,
        MarkRemoved,
        ClearMarks
    }
}
