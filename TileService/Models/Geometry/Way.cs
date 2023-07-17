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
        static readonly float LaneWidthVerge = 2.0f;
        static readonly float LaneWidthShoulder = 3.0f;
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
                Enumerable.Range(0, Points.Count - 1).Select(index =>
                {
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
            if (drivingLanes.Total == 0)
            {
                return new Road(lanes, center);
            }

            for (var i = 1; i <= drivingLanes.Forward; i++)
            {
                lanes.Add(new Lane(LaneType.Car, LaneDirection.Forward, LaneWidthCar));
            }
            for (var i = 1; i <= drivingLanes.Both; i++)
            {
                lanes.Add(new Lane(LaneType.Car, LaneDirection.Both, LaneWidthCar));
            }
            for (var i = 1; i <= drivingLanes.Backward; i++)
            {
                lanes.Add(new Lane(LaneType.Car, LaneDirection.Backward, LaneWidthCar));
            }
            center += LaneWidthCar * drivingLanes.Total / 2;

            // TODO: Support for driving on the right.
            var cycleway = way.Tags.GetValueOrDefault("cycleway", "no");
            var cyclewayBoth = way.Tags.GetValueOrDefault("cycleway:both", "no");
            var cyclewayLeft = way.Tags.GetValueOrDefault("cycleway:left", "no");
            var cyclewayRight = way.Tags.GetValueOrDefault("cycleway:right", "no");
            var cyclewayTwoWayBoth = way.Tags.GetValueOrDefault("cycleway:both:oneway", "yes") == "no";
            var cyclewayTwoWayLeft = way.Tags.GetValueOrDefault("cycleway:left:oneway", "yes") == "no";
            var cyclewayTwoWayRight = way.Tags.GetValueOrDefault("cycleway:right:oneway", "yes") == "no";
            if (cyclewayBoth != "no")
            {
                cyclewayLeft = cyclewayBoth;
                cyclewayRight = cyclewayBoth;
            }
            if (cyclewayTwoWayBoth)
            {
                cyclewayTwoWayLeft = true;
                cyclewayTwoWayRight = true;
            }
            if (cycleway != "no")
            {
                if (drivingLanes.Direction != LaneDirection.Backward) cyclewayLeft = cycleway;
                if (drivingLanes.Direction != LaneDirection.Forward) cyclewayRight = cycleway;
            }
            if (cyclewayTwoWayLeft)
            {
                if (cyclewayLeft == "lane")
                {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneDirection.Backward, LaneWidthCycle));
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneDirection.Forward, LaneWidthCycle));
                    center += LaneWidthCycle * 2;
                }
            }
            else
            {
                if (cyclewayLeft == "lane")
                {
                    lanes.Insert(0, new Lane(LaneType.Cycle, drivingLanes.Direction != LaneDirection.Backward ? LaneDirection.Forward : LaneDirection.Backward, LaneWidthCycle));
                    center += LaneWidthCycle;
                }
                else if (cyclewayLeft == "opposite_lane")
                {
                    lanes.Insert(0, new Lane(LaneType.Cycle, drivingLanes.Direction != LaneDirection.Backward ? LaneDirection.Backward : LaneDirection.Forward, LaneWidthCycle));
                    center += LaneWidthCycle;
                }
            }
            if (cyclewayTwoWayRight)
            {
                if (cyclewayRight == "lane")
                {
                    lanes.Add(new Lane(LaneType.Cycle, LaneDirection.Forward, LaneWidthCycle));
                    lanes.Add(new Lane(LaneType.Cycle, LaneDirection.Backward, LaneWidthCycle));
                }
            }
            else
            {
                if (cyclewayRight == "lane")
                {
                    lanes.Add(new Lane(LaneType.Cycle, drivingLanes.Direction != LaneDirection.Forward ? LaneDirection.Backward : LaneDirection.Forward, LaneWidthCycle));
                }
                else if (cyclewayRight == "opposite_lane")
                {
                    lanes.Add(new Lane(LaneType.Cycle, drivingLanes.Direction != LaneDirection.Forward ? LaneDirection.Forward : LaneDirection.Backward, LaneWidthCycle));
                }
            }

            var shoulder = way.Tags.GetValueOrDefault("shoulder", drivingLanes.Motorway ? "yes" : "no");
            var shoulderBoth = way.Tags.GetValueOrDefault("shoulder:both", "no");
            var shoulderLeft = way.Tags.GetValueOrDefault("shoulder:left", "no");
            var shoulderRight = way.Tags.GetValueOrDefault("shoulder:right", "no");
            if (shoulderBoth != "no")
            {
                shoulderLeft = shoulderBoth;
                shoulderRight = shoulderBoth;
            }
            switch (shoulder)
            {
                case "yes":
                    if (drivingLanes.Direction != LaneDirection.Backward) shoulderLeft = "yes";
                    if (drivingLanes.Direction != LaneDirection.Forward) shoulderRight = "yes";
                    break;
                case "both":
                    shoulderLeft = "yes";
                    shoulderRight = "yes";
                    break;
                case "left":
                    shoulderLeft = "yes";
                    break;
                case "right":
                    shoulderRight = "yes";
                    break;
            }
            if (shoulderLeft == "yes")
            {
                lanes.Insert(0, new Lane(LaneType.Shoulder, drivingLanes.Direction != LaneDirection.Backward ? LaneDirection.Forward : LaneDirection.Backward, LaneWidthShoulder));
                center += LaneWidthShoulder;
            }
            if (shoulderRight == "yes")
            {
                lanes.Add(new Lane(LaneType.Shoulder, drivingLanes.Direction != LaneDirection.Forward ? LaneDirection.Backward : LaneDirection.Forward, LaneWidthShoulder));
            }

            var parkingLeftLanes = GetWidthOfParkingLanes(way, ":left");
            if (parkingLeftLanes > 0)
            {
                lanes.Insert(0, new Lane(LaneType.Parking, parkingLeftLanes));
                center += parkingLeftLanes;
            }
            var parkingRightLanes = GetWidthOfParkingLanes(way, ":right");
            if (parkingRightLanes > 0)
            {
                lanes.Add(new Lane(LaneType.Parking, parkingRightLanes));
            }
            var parkingBothLanes = GetWidthOfParkingLanes(way, ":both");
            if (parkingBothLanes > 0)
            {
                lanes.Insert(0, new Lane(LaneType.Parking, parkingBothLanes));
                lanes.Add(new Lane(LaneType.Parking, parkingBothLanes));
                center += parkingBothLanes;
            }

            var verge = way.Tags.GetValueOrDefault("verge", "no");
            var vergeBoth = way.Tags.GetValueOrDefault("verge:both", "no");
            var vergeLeft = way.Tags.GetValueOrDefault("verge:left", "no");
            var vergeRight = way.Tags.GetValueOrDefault("verge:right", "no");
            if (vergeBoth != "no")
            {
                vergeLeft = vergeBoth;
                vergeRight = vergeBoth;
            }
            switch (verge)
            {
                case "yes":
                case "both":
                    vergeLeft = "yes";
                    vergeRight = "yes";
                    break;
                case "left":
                    vergeLeft = "yes";
                    break;
                case "right":
                    vergeRight = "yes";
                    break;
            }
            if (vergeLeft == "yes")
            {
                lanes.Insert(0, new Lane(LaneType.Verge, LaneWidthVerge));
                center += LaneWidthVerge;
            }
            if (vergeRight == "yes")
            {
                lanes.Add(new Lane(LaneType.Verge, LaneWidthVerge));
            }

            var sidewalk = way.Tags.GetValueOrDefault("sidewalk", "no");
            if (sidewalk == "both" || sidewalk == "left")
            {
                lanes.Insert(0, new Lane(LaneType.Sidewalk, LaneWidthSidewalk));
                center += LaneWidthSidewalk;
            }
            if (sidewalk == "both" || sidewalk == "right")
            {
                lanes.Add(new Lane(LaneType.Sidewalk, LaneWidthSidewalk));
            }

            lanes.Insert(0, new Lane(LaneType.Edge, 0));
            lanes.Add(new Lane(LaneType.Edge, 0));

            return new Road(lanes, center);
        }

        struct DrivingLanes
        {
            public bool Motorway;
            public LaneDirection Direction;
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
            var isMotorway = highway == "motorway" || highway == "motorway_link";
            var oneway = way.Tags.GetValueOrDefault("oneway", isMotorway ? "yes" : "no");
            var lanes = way.Tags.GetValueOrDefault("lanes");
            var lanesForward = way.Tags.GetValueOrDefault("lanes:forward");
            var lanesBackward = way.Tags.GetValueOrDefault("lanes:backward");
            var lanesBothWays = way.Tags.GetValueOrDefault("lanes:both_ways"); // NOTE: This is only a proposal!

            // Based on https://wiki.openstreetmap.org/wiki/Key:lanes#Lanes_in_different_directions
            var direction = oneway == "yes" ? LaneDirection.Forward : oneway == "-1" ? LaneDirection.Backward : LaneDirection.Both;
            var numTotal = ParseInt(lanes);
            var numForward = ParseInt(lanesForward);
            var numBackward = ParseInt(lanesBackward);
            var numBoth = ParseInt(lanesBothWays);

            if (numTotal.HasValue)
            {
                if (numForward.HasValue && numBackward.HasValue && numBoth.HasValue)
                {
                    // lanes= + lanes:forward= + lanes:backward= + lanes:both_ways=
                }
                else if (numForward.HasValue && numBackward.HasValue && !numBoth.HasValue)
                {
                    // lanes= + lanes:forward= + lanes:backward=
                    numBoth = numTotal - numForward - numBackward;
                }
                else if (numForward.HasValue && !numBackward.HasValue)
                {
                    // lanes= + lanes:forward= [+ lanes:both_ways=]
                    numBackward = numTotal - numForward - (numBoth ?? 0);
                }
                else if (!numForward.HasValue && numBackward.HasValue)
                {
                    // lanes= + lanes:backward= [+ lanes:both_ways=]
                    numForward = numTotal - numBackward - (numBoth ?? 0);
                }
                else if (!numForward.HasValue && !numBackward.HasValue && numBoth.HasValue)
                {
                    // lanes= + lanes:both_ways=
                    var remaining = numTotal - numBoth;
                    numForward = direction == LaneDirection.Forward ? remaining : direction == LaneDirection.Backward ? 0 : remaining / 2;
                    numBackward = direction == LaneDirection.Forward ? 0 : direction == LaneDirection.Backward ? remaining : remaining - numForward;
                }
                else if (!numForward.HasValue && !numBackward.HasValue && !numBoth.HasValue)
                {
                    // lanes=
                    numForward = direction == LaneDirection.Forward ? numTotal : direction == LaneDirection.Backward ? 0 : numTotal / 2;
                    numBackward = direction == LaneDirection.Forward ? 0 : direction == LaneDirection.Backward ? numTotal : numTotal / 2;
                    numBoth = direction == LaneDirection.Forward || direction == LaneDirection.Backward ? 0 : numTotal % 2;
                }
                numBoth ??= 0;
            }
            else
            {
                // Based on https://wiki.openstreetmap.org/wiki/Key:lanes#Assumptions
                var defaultLanes = direction != LaneDirection.Both || highway == "unclassified" || highway == "service" ? 1 : 2;
                numBoth ??= 0;
                if (numForward.HasValue && numBackward.HasValue)
                {
                    // lanes:forward= + lanes:backward= [+ lanes:both_ways=]
                }
                else if (numForward.HasValue && !numBackward.HasValue)
                {
                    // lanes:forward= [+ lanes:both_ways=]
                    numBackward = defaultLanes - numForward - numBoth;
                }
                else if (!numForward.HasValue && numBackward.HasValue)
                {
                    // lanes:backward= [+ lanes:both_ways=]
                    numForward = defaultLanes - numBackward - numBoth;
                }
                else if (!numForward.HasValue && !numBackward.HasValue)
                {
                    // [lanes:both_ways=]
                    if (direction == LaneDirection.Forward)
                    {
                        numForward = defaultLanes - numBoth;
                        numBackward = 0;
                    }
                    else if (direction == LaneDirection.Backward)
                    {
                        numForward = 0;
                        numBackward = defaultLanes - numBoth;
                    }
                    else if (defaultLanes == 2)
                    {
                        numForward = 1;
                        numBackward = 1;
                    }
                    else
                    {
                        numForward = 0;
                        numBackward = 0;
                        numBoth = 1;
                    }
                }
                numTotal = numForward + numBackward + numBoth;
            }

            if (numTotal != numForward + numBackward + numBoth || numTotal < 1 || numForward < 0 || numBackward < 0 || numBoth < 0 || (direction == LaneDirection.Forward && numTotal != numForward) || (direction == LaneDirection.Backward && numTotal != numBackward))
            {
                Console.WriteLine($"Warning: Unusual combination of oneway/lanes (name={way.Tags.GetValueOrDefault("name")} oneway={oneway} lanes={lanes} lanes:forward={lanesForward} lanes:backward={lanesBackward} lanes:both_ways={lanesBothWays})");
            }

            return new DrivingLanes
            {
                Motorway = isMotorway,
                Direction = direction,
                Forward = numForward.Value,
                Both = numBoth.Value,
                Backward = numBackward.Value,
            };
        }

        static float GetWidthOfParkingLanes(Way way, string side)
        {
            switch (way.Tags.GetValueOrDefault("parking:lane" + side, "no"))
            {
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
