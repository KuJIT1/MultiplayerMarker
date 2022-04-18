namespace MultiplayerMarker.Utils
{
    /// <summary>
    /// Результат запроса
    /// </summary>
    public class TryActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
