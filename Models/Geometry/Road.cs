using System.Collections.Generic;
using System.Collections.Immutable;

namespace osm_road_overlay.Models.Geometry
{
    public class Road
    {
        public ImmutableList<Lane> Lanes { get; }
        public float Center { get; }

        public Road(IEnumerable<Lane> lanes, float center)
        {
            Lanes = ImmutableList.ToImmutableList(lanes);
            Center = center;
        }
    }
}
