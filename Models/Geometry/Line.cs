using System.Collections.Generic;

namespace osm_road_overlay.Models.Geometry
{
    public class Line
    {
        public Point Start { get; }
        public Point End { get; }

        public Line(Point start, Point end)
        {
            Start = start;
            End = end;
        }
    }
}
