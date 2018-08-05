using System.Collections.Generic;
using System.Collections.Immutable;

namespace osm_road_overlay.Geometry
{
    public class Way
    {
        public ImmutableDictionary<string, string> Tags { get; }
        public ImmutableList<Line> Segments { get; }

        public Way(IDictionary<string, string> tags, IEnumerable<Line> segments)
        {
            Tags = ImmutableDictionary.ToImmutableDictionary(tags);
            Segments = ImmutableList.ToImmutableList(segments);
        }
    }
}
