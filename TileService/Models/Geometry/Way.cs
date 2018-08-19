using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TileService.Models.Geometry
{
    public class Way
    {
        static readonly float LaneWidthSidewalk = 2.0f;
        static readonly float LaneWidthCycle = 1.0f;
        static readonly float LaneWidthCar = 3.0f;

        public ImmutableDictionary<string, string> Tags { get; }
        public ImmutableList<Point> Points { get; }
        public ImmutableList<Line> Segments { get; }
        public Road Road { get; }

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
            Road = GetRoad(this);
        }

        static Road GetRoad(Way way)
        {
            var lanes = new List<Lane>();
            var center = 0f;

            var drivingLanes = GetNumberOfDrivingLanes(way);
            if (drivingLanes == 0) {
                return new Road(lanes, center);
            }

            for (var i = 0; i < drivingLanes; i++) {
                lanes.Add(new Lane(LaneType.Car, LaneWidthCar));
            }
            center += LaneWidthCar * drivingLanes / 2;

            var cycleway = way.Tags.GetValueOrDefault("cycleway", "no");
            var cyclewayBoth = way.Tags.GetValueOrDefault("cycleway:both", "no");
            if (cycleway == "lane" || cyclewayBoth == "lane" || cyclewayBoth == "opposite_lane") {
                lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
                center += LaneWidthCycle;
            } else if (cycleway == "opposite_lane") {
                lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
            } else {
                var cyclewayLeft = way.Tags.GetValueOrDefault("cycleway:left", "no");
                var cyclewayRight = way.Tags.GetValueOrDefault("cycleway:right", "no");
                if (cyclewayLeft == "lane" || cyclewayLeft == "opposite_lane") {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                    center += LaneWidthCycle;
                    if (way.Tags.GetValueOrDefault("cycleway:left:oneway", "yes") == "no") {
                        lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                        center += LaneWidthCycle;
                    }
                }
                if (cyclewayRight == "lane" || cyclewayRight == "opposite_lane") {
                    lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
                    if (way.Tags.GetValueOrDefault("cycleway:right:oneway", "yes") == "no") {
                        lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
                    }
                }
            }

            var parkingLeftLanes = GetWidthOfParkingLanes(way, ":left");
            if (parkingLeftLanes > 0) {
                lanes.Insert(0, new Lane(LaneType.Parking, parkingLeftLanes));
                center += parkingLeftLanes;
            }
            var parkingRightLanes = GetWidthOfParkingLanes(way, ":right");
            if (parkingRightLanes > 0) {
                lanes.Add(new Lane(LaneType.Parking, parkingRightLanes));
            }
            var parkingBothLanes = GetWidthOfParkingLanes(way, ":both");
            if (parkingBothLanes > 0) {
                lanes.Insert(0, new Lane(LaneType.Parking, parkingBothLanes));
                lanes.Add(new Lane(LaneType.Parking, parkingBothLanes));
                center += parkingLeftLanes;
            }

            var sidewalk = way.Tags.GetValueOrDefault("sidewalk", "no");
            if (sidewalk == "both" || sidewalk == "left") {
                lanes.Insert(0, new Lane(LaneType.Sidewalk, LaneWidthSidewalk));
                center += LaneWidthSidewalk;
            }
            if (sidewalk == "both" || sidewalk == "right") {
                lanes.Add(new Lane(LaneType.Sidewalk, LaneWidthSidewalk));
            }

            lanes.Insert(0, new Lane(LaneType.Edge, 0));
            lanes.Add(new Lane(LaneType.Edge, 0));

            return new Road(lanes, center);
        }

        static int GetNumberOfDrivingLanes(Way way)
        {
            var oneway = way.Tags.GetValueOrDefault("oneway", "no");
            var defaultLanes = oneway == "yes" || oneway == "-1" ? "1" : "2";
            return int.Parse(way.Tags.GetValueOrDefault("lanes", defaultLanes));
        }

        static float GetWidthOfParkingLanes(Way way, string side)
        {
            switch (way.Tags.GetValueOrDefault("parking:lane" + side, "no")) {
                case "parallel":
                    return LaneWidthCar;
                case "diagonal":
                    return LaneWidthCar * 1.5f;
                case "perpendicular":
                    return LaneWidthCar * 2;
                default:
                    return 0;
            }
        }
    }
}
