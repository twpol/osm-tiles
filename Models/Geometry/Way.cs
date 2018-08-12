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

        public Way(IDictionary<string, string> tags, IEnumerable<Point> points)
        {
            Tags = ImmutableDictionary.ToImmutableDictionary(tags);
            Points = ImmutableList.ToImmutableList(points);
            Segments = ImmutableList.ToImmutableList(
                Enumerable.Range(0, Points.Count - 1).Select(index => {
                    return new Line(Points[index], Points[index + 1]);
                })
            );
            Points[0].AngleRad = Segments[0].AngleRad;
            for (var index = 1; index < Points.Count - 1; index++)
            {
                Points[index].AngleRad = (Segments[index - 1].AngleRad + Segments[index].AngleRad) / 2;
            }
            Points[Points.Count - 1].AngleRad = Segments[Segments.Count - 1].AngleRad;
        }
    }
}
