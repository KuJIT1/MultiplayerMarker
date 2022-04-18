namespace MultiplayerMarker.Utils
{
    using MultiplayerMarker.Entities;

    /// <summary>
    /// Результат запроса <see cref="Hub.GameHub.TryAddUser"/>
    /// </summary>
    public class TryAddUserActionResult :TryActionResult
    {
        public User User { get; set; }
    }
}
