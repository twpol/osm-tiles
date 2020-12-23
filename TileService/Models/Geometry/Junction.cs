using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TileService.Models.Geometry
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
                var extraAngle1 = TerminatedWayPoints[0].IsFirst ? Angle.HalfTurn : Angle.Zero;
                var extraAngle2 = TerminatedWayPoints[1].IsLast ? Angle.HalfTurn : Angle.Zero;
                var angleRad = Angle.Average(
                    Angle.Add(TerminatedWayPoints[0].Point.Angle, extraAngle1),
                    Angle.Add(TerminatedWayPoints[1].Point.Angle, extraAngle2)
                );
                TerminatedWayPoints[0].Point.Angle = Angle.Add(angleRad, extraAngle1);
                TerminatedWayPoints[1].Point.Angle = Angle.Add(angleRad, extraAngle2);
            } else if (WayPoints.All(wp => wp.Way.Tags.GetValueOrDefault("oneway", "no") == "yes")) {
                var angleRad = Angle.Average(
                    WayPoints.Select(wp => wp.Point.Angle).ToArray()
                );
                foreach (var wp in WayPoints) {
                    wp.Point.Angle = angleRad;
                }
            }
        }
    }
}
