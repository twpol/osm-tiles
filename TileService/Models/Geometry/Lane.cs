namespace TileService.Models.Geometry
{
    public class Lane
    {
        public LaneType Type { get; }
        public float Width { get; }

        public Lane(LaneType type, float width)
        {
            Type = type;
            Width = width;
        }

        public override string ToString()
        {
            return $"{Type} {Width:F1}m";
        }
    }

    public enum LaneType
    {
        Edge,
        Sidewalk,
        Parking,
        Cycle,
        Car,
    }
}
