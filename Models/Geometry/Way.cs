using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace osm_road_overlay.Models.Geometry
{
    public class Way
    {
        public ImmutableDictionary<string, string> Tags { get; }
        public ImmutableList<Point> Points { get; }
        public ImmutableList<Line> Segments { get; }

        public Way(Tile tile, IDictionary<string, string> tags, IEnumerable<Point> points)
        {
            Tags = ImmutableDictionary.ToImmutableDictionary(tags);
            Points = ImmutableList.ToImmutableList(points);
            Segments = ImmutableList.ToImmutableList(
                Enumerable.Range(0, Points.Count - 1).Select(index => {
                    return new Line(tile, Points[index], Points[index + 1]);
                })
            );
            Points[0].Angle = Segments[0].Angle;
            for (var index = 1; index < Points.Count - 1; index++)
            {
                Points[index].Angle = Angle.Average(Segments[index - 1].Angle, Segments[index].Angle);
            }
            Points[Points.Count - 1].Angle = Segments[Segments.Count - 1].Angle;
        }
    }
}
