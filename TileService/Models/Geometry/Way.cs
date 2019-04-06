using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TileService.Models.Common;

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

            var oneway = way.Tags.GetValueOrDefault("oneway", "no");
            for (var i = 1; i <= drivingLanes; i++) {
                // TODO: Support for lanes:forward and lanes:backward
                lanes.Add(new Lane(
                    LaneType.Car,
                    oneway == "yes" ? LaneDirection.Forward :
                    oneway == "-1" ? LaneDirection.Backward :
                    i * 2 < drivingLanes + 1 ? LaneDirection.Forward :
                    i * 2 == drivingLanes + 1 ? LaneDirection.Both :
                    LaneDirection.Backward,
                    LaneWidthCar
                ));
            }
            center += LaneWidthCar * drivingLanes / 2;

            // TODO: Support for driving on the right.
            var twoway = way.Tags.GetValueOrDefault("oneway", "no") == "no";
            var cycleway = way.Tags.GetValueOrDefault("cycleway", "no");
            var cyclewayBoth = way.Tags.GetValueOrDefault("cycleway:both", "no");
            var cyclewayLeft = way.Tags.GetValueOrDefault("cycleway:left", "no");
            var cyclewayRight = way.Tags.GetValueOrDefault("cycleway:right", "no");
            var cyclewayTwowayBoth = way.Tags.GetValueOrDefault("cycleway:both:oneway", "yes") == "no";
            var cyclewayTwowayLeft = way.Tags.GetValueOrDefault("cycleway:left:oneway", "yes") == "no";
            var cyclewayTwowayRight = way.Tags.GetValueOrDefault("cycleway:right:oneway", "yes") == "no";
            if (cyclewayBoth != "no") {
                cyclewayLeft = cyclewayBoth;
                cyclewayRight = cyclewayBoth;
            }
            if (cyclewayTwowayBoth) {
                cyclewayTwowayLeft = true;
                cyclewayTwowayRight = true;
            }
            if (cycleway != "no") {
                cyclewayLeft = cycleway;
                if (twoway) cyclewayRight = cycleway;
            }
            if (cyclewayTwowayLeft) {
                if (cyclewayLeft == "lane") {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneDirection.Backward, LaneWidthCycle));
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneDirection.Forward, LaneWidthCycle));
                    center += LaneWidthCycle * 2;
                }
            } else {
                if (cyclewayLeft == "lane") {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneDirection.Forward, LaneWidthCycle));
                    center += LaneWidthCycle;
                } else if (cyclewayLeft == "opposite_lane") {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneDirection.Backward, LaneWidthCycle));
                    center += LaneWidthCycle;
                }
            }
            if (cyclewayTwowayRight) {
                if (cyclewayRight == "lane") {
                    lanes.Add(new Lane(LaneType.Cycle, LaneDirection.Forward, LaneWidthCycle));
                    lanes.Add(new Lane(LaneType.Cycle, LaneDirection.Backward, LaneWidthCycle));
                }
            } else {
                if (cyclewayRight == "lane") {
                    lanes.Add(new Lane(LaneType.Cycle, twoway ? LaneDirection.Backward : LaneDirection.Forward, LaneWidthCycle));
                } else if (cyclewayRight == "opposite_lane") {
                    lanes.Add(new Lane(LaneType.Cycle, twoway ? LaneDirection.Forward : LaneDirection.Backward, LaneWidthCycle));
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
                center += parkingBothLanes;
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
            var highway = way.Tags.GetValueOrDefault("highway", "");
            var oneway = way.Tags.GetValueOrDefault("oneway", "no");
            var defaultLanes =
                oneway == "yes" || oneway == "-1" ? "1" :
                highway == "service" ? "1" :
                "2";
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
