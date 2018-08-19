using System.Collections.Generic;
using TileService.Models.Geometry;
using Xunit;

namespace TileService.Tests
{
    public class Roads
    {
        static readonly Tile Tile = new Tile(18, 0, 0);
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
                "Road(Edge 0.0m|Car 3.0m|Car 3.0m|Edge 0.0m, Center=3.0m)",
                GetStraightRoadText("highway=road")
            );
        }
    }
}
