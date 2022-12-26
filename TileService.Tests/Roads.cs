using System.Collections.Generic;
using TileService.Models.Geometry;
using Xunit;

namespace TileService.Tests
{
    public class Roads
    {
        // TODO: Fix bug in tile Overpass query that exceeds 180 range on bounding box so we can use (18, 0, 0) here.
        static readonly RoadTile Tile = RoadTile.Cache.Get(18, 1 << 4, 1 << 4).Result;
        static readonly Point[] StraightWayPoints = {
            new Point(0, 0),
            new Point(0, 1),
        };

        IDictionary<string, string> GetTagsFromList(IList<string> list)
        {
            var dict = new Dictionary<string, string>(list.Count);
            for (var index = 0; index < list.Count; index++) {
                var split = list[index].Split('=', 2);
                dict.Add(split[0], split[1]);
            }
            return dict;
        }

        Way GetStraightWay(IDictionary<string, string> tags)
        {
            return new Way(Tile, tags, StraightWayPoints);
        }

        Road GetStraightRoad(IDictionary<string, string> tags)
        {
            return GetStraightWay(tags).Road;
        }

        string GetStraightRoadText(params string[] tags)
        {
            return GetStraightRoad(GetTagsFromList(tags)).ToString();
        }

        [Fact]
        public void HighwayRoad()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)",
                GetStraightRoadText("highway=road")
            );
        }

        [Fact]
        public void HighwayRoadLanes1()
        {
            Assert.Equal(
                "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)",
                GetStraightRoadText("highway=road", "lanes=1")
            );
        }

        [Fact]
        public void HighwayRoadLanes2()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)",
                GetStraightRoadText("highway=road", "lanes=2")
            );
        }

        [Fact]
        public void HighwayRoadLanes3()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=road", "lanes=3")
            );
        }

        [Fact]
        public void HighwayRoadLanes4()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "lanes=4")
            );
        }

        [Fact]
        public void HighwayRoadOnewayYes()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)",
                GetStraightRoadText("highway=road", "oneway=yes")
            );
        }

        [Fact]
        public void HighwayRoadOnewayYesLanes1()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)",
                GetStraightRoadText("highway=road", "oneway=yes", "lanes=1")
            );
        }

        [Fact]
        public void HighwayRoadOnewayYesLanes2()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=3.0m)",
                GetStraightRoadText("highway=road", "oneway=yes", "lanes=2")
            );
        }

        [Fact]
        public void HighwayRoadOnewayYesLanes3()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=road", "oneway=yes", "lanes=3")
            );
        }

        [Fact]
        public void HighwayRoadOnewayYesLanes4()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "oneway=yes", "lanes=4")
            );
        }

        [Fact]
        public void HighwayMotorway()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=motorway")
            );
        }

        [Fact]
        public void HighwayMotorwayLanes1()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=motorway", "lanes=1")
            );
        }

        [Fact]
        public void HighwayMotorwayLanes2()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=motorway", "lanes=2")
            );
        }

        [Fact]
        public void HighwayMotorwayLanes3()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=7.5m)",
                GetStraightRoadText("highway=motorway", "lanes=3")
            );
        }

        [Fact]
        public void HighwayMotorwayLanes4()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=9.0m)",
                GetStraightRoadText("highway=motorway", "lanes=4")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayYes()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=motorway", "oneway=yes")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayLanes1()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=motorway", "oneway=yes", "lanes=1")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayLanes2()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=motorway", "oneway=yes", "lanes=2")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayLanes3()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=7.5m)",
                GetStraightRoadText("highway=motorway", "oneway=yes", "lanes=3")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayLanes4()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=9.0m)",
                GetStraightRoadText("highway=motorway", "oneway=yes", "lanes=4")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayNo()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=motorway", "oneway=no")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayNoLanes1()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↕ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=motorway", "oneway=no", "lanes=1")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayNoLanes2()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=motorway", "oneway=no", "lanes=2")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayNoLanes3()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=7.5m)",
                GetStraightRoadText("highway=motorway", "oneway=no", "lanes=3")
            );
        }

        [Fact]
        public void HighwayMotorwayOnewayNoLanes4()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=9.0m)",
                GetStraightRoadText("highway=motorway", "oneway=no", "lanes=4")
            );
        }

        [Fact]
        public void HighwayRoadShoulderYes()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "shoulder=yes")
            );
        }

        [Fact]
        public void HighwayRoadShoulderBoth()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "shoulder=both")
            );
        }

        [Fact]
        public void HighwayRoadShoulderBothYes()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "shoulder:both=yes")
            );
        }

        [Fact]
        public void HighwayRoadShoulderLeft()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "shoulder=left")
            );
        }

        [Fact]
        public void HighwayRoadShoulderLeftYes()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)",
                GetStraightRoadText("highway=road", "shoulder:left=yes")
            );
        }

        [Fact]
        public void HighwayRoadShoulderRight()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)",
                GetStraightRoadText("highway=road", "shoulder=right")
            );
        }

        [Fact]
        public void HighwayRoadShoulderRightYes()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)",
                GetStraightRoadText("highway=road", "shoulder:right=yes")
            );
        }

        [Fact]
        public void HighwayRoadOnewayShoulderYes()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=road", "oneway=yes", "shoulder=yes")
            );
        }

        [Fact]
        public void HighwayRoadOnewayShoulderBoth()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=road", "oneway=yes", "shoulder=both")
            );
        }

        [Fact]
        public void HighwayRoadOnewayShoulderBothYes()
        {
            Assert.Equal(
                "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)",
                GetStraightRoadText("highway=road", "oneway=yes", "shoulder:both=yes")
            );
        }

        [Fact]
        public void HighwayMotorwayShoulderNo()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)",
                GetStraightRoadText("highway=motorway", "shoulder=no")
            );
        }

        // The following tests are based on the examples found at: https://wiki.openstreetmap.org/wiki/Bicycle
        // In all these tests, left/right have been flipped as wiki is using driving-on-right and we're driving-on-left.

        [Fact]
        public void WikiBicycleExampleL1a()
        {
            const string road = "Road(Edge|Cycle ↑ 1.0m|Car ↑ 3.0m|Car ↓ 3.0m|Cycle ↓ 1.0m|Edge, Center=4.0m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "cycleway=lane"));
            Assert.Equal(road, GetStraightRoadText("highway=road", "cycleway:left=lane", "cycleway:right=lane"));
            Assert.Equal(road, GetStraightRoadText("highway=road", "cycleway:both=lane"));
        }

        [Fact]
        public void WikiBicycleExampleL1b()
        {
            const string road = "Road(Edge|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=5.0m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "cycleway:left=lane", "cycleway:left:oneway=no"));
            // Can't be distinguished from L1a: Assert.Equal(road, GetStraightRoadText("highway=road", "cycleway=lane"));
        }

        [Fact]
        public void WikiBicycleExampleL2()
        {
            const string road = "Road(Edge|Cycle ↑ 1.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=4.0m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "cycleway:left=lane"));
        }

        [Fact]
        public void WikiBicycleExampleM1()
        {
            const string road = "Road(Edge|Cycle ↑ 1.0m|Car ↑ 3.0m|Cycle ↓ 1.0m|Edge, Center=2.5m)";
            // TODO: oneway:bicycle=no not supported: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "cycleway=lane", "oneway:bicycle=no"));
            Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "cycleway:left=lane", "cycleway:right=opposite_lane"));
        }

        [Fact]
        public void WikiBicycleExampleM2a()
        {
            const string road = "Road(Edge|Cycle ↑ 1.0m|Car ↑ 3.0m|Edge, Center=2.5m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "cycleway:left=lane"));
            // Can't be distinguished from M2b: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "cycleway=lane"));
        }

        [Fact]
        public void WikiBicycleExampleM2b()
        {
            const string road = "Road(Edge|Car ↑ 3.0m|Cycle ↑ 1.0m|Edge, Center=1.5m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "cycleway:right=lane"));
            // Can't be distinguished from M2a: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "cycleway=lane"));
        }

        [Fact]
        public void WikiBicycleExampleM2c()
        {
            const string road = "Road(Edge|Car ↑ 3.0m|Cycle ↑ 1.0m|Car ↑ 3.0m|Edge, Center=3.5m)";
            // Ambigious with a cycleway on the left/right side of the road: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "lanes=2", "cycleway=lane"));
        }

        [Fact]
        public void WikiBicycleExampleM2d()
        {
            const string road = "Road(Edge|Car ↑ 3.0m|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Edge, Center=1.5m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "oneway:bicycle=no", "cycleway:right=lane", "cycleway:right:oneway=no"));
        }

        [Fact]
        public void WikiBicycleExampleM3a()
        {
            const string road = "Road(Edge|Car ↑ 3.0m|Cycle ↓ 1.0m|Edge, Center=1.5m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "oneway:bicycle=no", "cycleway:right=opposite_lane"));
            // Can't be distinguished from M3b: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "oneway:bicycle=no", "cycleway=opposite_lane"));
        }

        [Fact]
        public void WikiBicycleExampleM3b()
        {
            const string road = "Road(Edge|Cycle ↓ 1.0m|Car ↑ 3.0m|Edge, Center=2.5m)";
            Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "oneway:bicycle=no", "cycleway:left=opposite_lane"));
            // Can't be distinguished from M3a: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "oneway:bicycle=no", "cycleway=opposite_lane"));
        }

        // WikiBicycleExampleM4 is comprised entirely of other examples

        // WikiBicycleExampleM5 is comprised entirely of other examples

        [Fact]
        public void WikiBicycleExampleT1MultiWayA()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)",
                GetStraightRoadText("highway=road", "bicycle=use_sidepath")
            );
        }

        [Fact]
        public void WikiBicycleExampleT1MultiWayBC()
        {
            // TODO: highway=cycleway not supported:
            // Assert.Equal(
            //     "Road(Edge|Cycle ↑ 1.0m|Edge, Center=0.5m)",
            //     GetStraightRoadText("highway=cycleway", "oneway=yes")
            // );
        }

        [Fact]
        public void WikiBicycleExampleT1SingleWay()
        {
            // TODO: cycleway=track not supported:
            // Assert.Equal(
            //     "Road(Edge|Cycle ↑ 1.0m|Verge 0.5m|Car ↑ 3.0m|Car ↓ 3.0m|Verge 0.5m|Cycle ↓ 1.0m|Edge, Center=4.5m)",
            //     GetStraightRoadText("highway=road", "cycleway=track")
            // );
        }

        // WikiBicycleExampleT2MultiWayA is identical to WikiBicycleExampleT1MultiWayA

        [Fact]
        public void WikiBicycleExampleT2MultiWayB()
        {
            // TODO: highway=cycleway not supported:
            // Assert.Equal(
            //     "Road(Edge|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Edge, Center=1.0m)",
            //     GetStraightRoadText("highway=cycleway", "oneway=no")
            // );
        }

        [Fact]
        public void WikiBicycleExampleT2SingleWay()
        {
            // TODO: cycleway=track not supported:
            // Assert.Equal(
            //     "Road(Edge|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Verge 0.5m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=5.5m)",
            //     GetStraightRoadText("highway=road", "cycleway:left=track", "cycleway:left:oneway=no")
            // );
        }

        [Fact]
        public void WikiBicycleExampleT3MultiWayA()
        {
            Assert.Equal(
                "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)",
                GetStraightRoadText("highway=road", "oneway=yes", "bicycle=use_sidepath")
            );
        }

        // WikiBicycleExampleT3MultiWayB is identical to WikiBicycleExampleT2MultiWayB

        [Fact]
        public void WikiBicycleExampleT3SingleWay()
        {
            // TODO: cycleway=track not supported:
            // Assert.Equal(
            //     "Road(Edge|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Verge 0.5m|Car ↑ 3.0m|Edge, Center=4.0m)",
            //     GetStraightRoadText("highway=road", "oneway=yes", "cycleway:left=track", "oneway:bicycle=no")
            // );
        }

        // WikiBicycleExampleT4MultiWayA is identical to WikiBicycleExampleT1MultiWayA

        // WikiBicycleExampleT4MultiWayB is identical to WikiBicycleExampleT1MultiWayB

        [Fact]
        public void WikiBicycleExampleT4SingleWay()
        {
            // TODO: cycleway=track not supported:
            // Assert.Equal(
            //     "Road(Edge|Cycle ↓ 1.0m|Verge 0.5m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)",
            //     GetStraightRoadText("highway=road", "cycleway:left=track")
            // );
        }

        // TODO: Examples S1 through S5

        // TODO: Examples B1 through B6
    }
}
