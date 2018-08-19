using System.Collections.Generic;
using System.Collections.Immutable;

namespace TileService.Models.Geometry
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

        public override string ToString()
        {
            return $"Road({string.Join("|", Lanes)}, Center={Center:F1}m)";
        }
    }
}
