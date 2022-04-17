using System;

namespace MultiplayerMarker.Entities
{
    public class Wayfarer
    {
        public Mark StartMark { get; set; }

        public Mark EndMark { get; set; }

        public DateTime? StartTime { get; set; }

        public bool InProgress { get; set; }

        public int? MovementTime { get; set; }
    }
}