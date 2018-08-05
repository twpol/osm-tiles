using System.Collections.Generic;
using System.Collections.Immutable;

namespace osm_road_overlay.Models.Geometry
{
    public class World
    {
        public ImmutableList<Way> Ways { get; }

        public World(IEnumerable<Way> ways)
        {
            Ways = ImmutableList.ToImmutableList(ways);
        }
    }
}
