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

            var drivingLanes = GetDrivingLanes(way);
            if (drivingLanes.Total == 0) {
                return new Road(lanes, center);
            }

            for (var i = 1; i <= drivingLanes.Forward; i++) {
                lanes.Add(new Lane(LaneType.Car, LaneDirection.Forward, LaneWidthCar));
            }
            for (var i = 1; i <= drivingLanes.Both; i++) {
                lanes.Add(new Lane(LaneType.Car, LaneDirection.Both, LaneWidthCar));
            }
            for (var i = 1; i <= drivingLanes.Backward; i++) {
                lanes.Add(new Lane(LaneType.Car, LaneDirection.Backward, LaneWidthCar));
            }
            center += LaneWidthCar * drivingLanes.Total / 2;

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

        struct DrivingLanes
        {
            public int Forward;
            public int Both;
            public int Backward;
            public int Total { get => Forward + Both + Backward; }
        }

        static int? ParseInt(string s)
        {
            if (int.TryParse(s, out var i))
                return i;
            return null;
        }

        static DrivingLanes GetDrivingLanes(Way way)
        {
            var highway = way.Tags.GetValueOrDefault("highway", "");
            var oneway = way.Tags.GetValueOrDefault("oneway", "no");
            var lanes = way.Tags.GetValueOrDefault("lanes");
            var lanesForward = way.Tags.GetValueOrDefault("lanes:forward");
            var lanesBackward = way.Tags.GetValueOrDefault("lanes:backward");
            var lanesBothWays = way.Tags.GetValueOrDefault("lanes:both_ways"); // NOTE: This is only a proposal!

            // Based on https://wiki.openstreetmap.org/wiki/Key:lanes#Lanes_in_different_directions
            var isOneway = oneway == "yes" || oneway == "-1";
            var numTotal = ParseInt(lanes);
            var numForward = ParseInt(lanesForward);
            var numBackward = ParseInt(lanesBackward);
            var numBoth = ParseInt(lanesBothWays);

            if (numTotal.HasValue) {
                if (numForward.HasValue && numBackward.HasValue && numBoth.HasValue) {
                    // lanes= + lanes:forward= + lanes:backward= + lanes:both_ways=
                } else if (numForward.HasValue && numBackward.HasValue && !numBoth.HasValue) {
                    // lanes= + lanes:forward= + lanes:backward=
                    numBoth = numTotal - numForward - numBackward;
                } else if (numForward.HasValue && !numBackward.HasValue) {
                    // lanes= + lanes:forward= [+ lanes:both_ways=]
                    numBackward = numTotal - numForward - (numBoth ?? 0);
                } else if (!numForward.HasValue && numBackward.HasValue) {
                    // lanes= + lanes:backward= [+ lanes:both_ways=]
                    numForward = numTotal - numBackward - (numBoth ?? 0);
                } else if (!numForward.HasValue && !numBackward.HasValue && numBoth.HasValue) {
                    // lanes= + lanes:both_ways=
                    var remaining = numTotal - numBoth;
                    numForward = isOneway ? remaining : remaining / 2;
                    numBackward = isOneway ? 0 : remaining - numForward;
                } else if (!numForward.HasValue && !numBackward.HasValue && !numBoth.HasValue) {
                    // lanes=
                    numForward = isOneway ? numTotal : numTotal / 2;
                    numBackward = isOneway ? 0 : numTotal / 2;
                    numBoth = isOneway ? 0 : numTotal % 2;
                }
                numBoth = numBoth ?? 0;
            } else {
                // Based on https://wiki.openstreetmap.org/wiki/Key:lanes#Assumptions
                var defaultLanes =
                    isOneway ? 1 :
                    highway == "unclassified" || highway == "service" ? 1 :
                    2;
                numBoth = numBoth ?? 0;
                if (numForward.HasValue && numBackward.HasValue) {
                    // lanes:forward= + lanes:backward= [+ lanes:both_ways=]
                } else if (numForward.HasValue && !numBackward.HasValue) {
                    // lanes:forward= [+ lanes:both_ways=]
                    numBackward = defaultLanes - numForward - numBoth;
                } else if (!numForward.HasValue && numBackward.HasValue) {
                    // lanes:backward= [+ lanes:both_ways=]
                    numForward = defaultLanes - numBackward - numBoth;
                } else if (!numForward.HasValue && !numBackward.HasValue) {
                    // [lanes:both_ways=]
                    numForward = 1;
                    numBackward = defaultLanes == 2 ? 1 : 0;
                }
                numTotal = numForward + numBackward + numBoth;
            }

            if (numTotal != numForward + numBackward + numBoth || numTotal < 1 || numForward < 0 || numBackward < 0 || numBoth < 0 || (isOneway && numTotal != numForward)) {
                Console.WriteLine($"Warning: Unusual combination of oneway/lanes (name={way.Tags.GetValueOrDefault("name")} oneway={oneway} lanes={lanes} lanes:forward={lanesForward} lanes:backward={lanesBackward} lanes:both_ways={lanesBothWays})");
            }

            return new DrivingLanes {
                Forward = numForward.Value,
                Both = numBoth.Value,
                Backward = numBackward.Value,
            };
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
