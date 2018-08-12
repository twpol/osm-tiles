using System;
using System.Collections.Generic;

namespace osm_road_overlay.Models.Geometry
{
    public class Line
    {
        public Point Start { get; }
        public Point End { get; }
        public Angle Angle { get; }

        public Line(Tile tile, Point start, Point end)
        {
            Start = start;
            End = end;
            var pointStart = tile.GetPointFromPoint(start);
            var pointEnd = tile.GetPointFromPoint(end);
            Angle = new Angle(Math.Atan2(pointEnd.Y - pointStart.Y, pointEnd.X - pointStart.X));
        }
    }
}
