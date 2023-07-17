using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TileService.Models.Geometry
{
    public class Road
    {
        public ImmutableList<Lane> Lanes { get; }
        public float Center { get; }

        public bool IsOneWay(LaneType type)
        {
            var directions = Lanes.Where(lane => lane.Type == type).Select(lane => lane.Direction).Distinct().ToList();
            return directions.Count == 1 && directions[0] != LaneDirection.Both;
        }

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
