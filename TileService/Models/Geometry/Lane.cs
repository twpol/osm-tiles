using System.Collections.Generic;

namespace TileService.Models.Geometry
{
    public class Lane
    {
        public LaneType Type { get; }
        public LaneDirection Direction { get; }
        public float Width { get; }

        public Lane(LaneType type, LaneDirection direction, float width)
        {
            Type = type;
            Direction = direction;
            Width = width;
        }

        public Lane(LaneType type, float width)
            : this(type, LaneDirection.None, width)
        {
        }

        static readonly Dictionary<LaneDirection, string> DirectionKey = new Dictionary<LaneDirection, string>() {
            { LaneDirection.None, "" },
            { LaneDirection.Forward, " ↑" },
            { LaneDirection.Backward, " ↓" },
            { LaneDirection.Both, " ↕" },
        };

        public override string ToString()
        {
            if (Width == 0)
            {
                return $"{Type}{DirectionKey[Direction]}";
            }
            return $"{Type}{DirectionKey[Direction]} {Width:F1}m";
        }
    }

    public enum LaneType
    {
        Edge,
        Sidewalk,
        Verge,
        Parking,
        Shoulder,
        Cycle,
        Car,
    }

    public enum LaneDirection
    {
        None,
        Forward,
        Backward,
        Both,
    }
}
