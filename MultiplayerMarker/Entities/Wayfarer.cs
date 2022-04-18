namespace MultiplayerMarker.Entities
{
    using System;

    /// <summary>
    /// Объект для описания движения метки. Путник.
    /// TODO: предусмотреть смену направления
    /// </summary>
    public class Wayfarer
    {
        /// <summary>
        /// Метка начала движения
        /// </summary>
        public Mark StartMark { get; set; }

        /// <summary>
        /// Метка конца движения
        /// </summary>
        public Mark EndMark { get; set; }

        /// <summary>
        /// Время начала движения
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Флаг движения
        /// </summary>
        public bool InProgress { get; set; }

        /// <summary>
        /// Время в пути
        /// </summary>
        public int? MovementTime { get; set; }
    }
}