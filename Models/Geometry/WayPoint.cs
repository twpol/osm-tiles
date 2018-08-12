using System.Linq;

namespace osm_road_overlay.Models.Geometry
{
    public class WayPoint
    {
        public Way Way { get; }
        public Point Point { get; }
        public bool IsFirst { get; }
        public bool IsMiddle { get; }
        public bool IsLast { get; }

        public WayPoint(Way way, Point point)
        {
            Way = way;
            Point = point;
            IsFirst = way.Points.First() == point;
            IsLast = way.Points.Last() == point;
            IsMiddle = !IsFirst && !IsLast;
        }
    }
}
