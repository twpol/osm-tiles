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
