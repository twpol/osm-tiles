using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace osm_road_overlay.Models.Geometry
{
    public class Junction
    {
        public Point Location { get; }
        public ImmutableList<WayPoint> WayPoints { get; }
        public ImmutableList<WayPoint> ThroughWayPoints { get; }
        public ImmutableList<WayPoint> TerminatedWayPoints { get; }

        public Junction(IEnumerable<WayPoint> wayPoints)
        {
            Location = wayPoints.First().Point;
            WayPoints = ImmutableList.ToImmutableList(wayPoints);
            ThroughWayPoints = ImmutableList.ToImmutableList(wayPoints.Where(wayPoint => wayPoint.IsMiddle));
            TerminatedWayPoints = ImmutableList.ToImmutableList(wayPoints.Where(wayPoint => !wayPoint.IsMiddle));

            if (ThroughWayPoints.Count == 0 && TerminatedWayPoints.Count == 2) {
                var angleRad = (TerminatedWayPoints[0].Point.AngleRad + TerminatedWayPoints[1].Point.AngleRad) / 2;
                TerminatedWayPoints[0].Point.AngleRad = angleRad;
                TerminatedWayPoints[1].Point.AngleRad = angleRad;
            }
        }
    }
}
