using System.Collections.Generic;
using TileService.Models.Common;
using TileService.Models.Geometry;
using Xunit;

namespace TileService.Tests
{
    public class Roads
    {
        // TODO: Fix bug in tile Overpass query that exceeds 180 range on bounding box so we can use (18, 0, 0) here.
        static readonly Tile Tile = Tile.Cache.Get(18, 1 << 4, 1 << 4).Result;
        static readonly Point[] StraightWayPoints = {
            new Point(0, 0),
            new Point(0, 1),
        };

        static IDictionary<string, string> GetTagsFromList(IList<string> list)
        {
            var dict = new Dictionary<string, string>(list.Count);
            for (var index = 0; index < list.Count; index++)
            {
                var split = list[index].Split('=', 2);
                dict.Add(split[0], split[1]);
            }
            return dict;
        }

        static Way GetStraightWay(IDictionary<string, string> tags)
        {
            return new Way(Tile, tags, StraightWayPoints);
        }

        static Road GetStraightRoad(IDictionary<string, string> tags)
        {
            return GetStraightWay(tags).Road;
        }

        static string GetStraightRoadText(params string[] tags)
        {
            return GetStraightRoad(GetTagsFromList(tags)).ToString();
        }

        [Theory]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)", "highway=road", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=no", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=no", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=no", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=no", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=yes", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=yes", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=yes", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=yes", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=-1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=-1", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=-1", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=-1", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=-1", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)", "highway=service")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)", "highway=service", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=service", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=service", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=service", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)", "highway=service", "oneway=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↕ 3.0m|Edge, Center=1.5m)", "highway=service", "oneway=no", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=service", "oneway=no", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=service", "oneway=no", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=service", "oneway=no", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=service", "oneway=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=service", "oneway=yes", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=3.0m)", "highway=service", "oneway=yes", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=service", "oneway=yes", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)", "highway=service", "oneway=yes", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Edge, Center=1.5m)", "highway=service", "oneway=-1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Edge, Center=1.5m)", "highway=service", "oneway=-1", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=service", "oneway=-1", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=service", "oneway=-1", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=service", "oneway=-1", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)", "highway=motorway", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=7.5m)", "highway=motorway", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=9.0m)", "highway=motorway", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↕ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=no", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↕ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=7.5m)", "highway=motorway", "oneway=no", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=9.0m)", "highway=motorway", "oneway=no", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=yes", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=7.5m)", "highway=motorway", "oneway=yes", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Car ↑ 3.0m|Edge, Center=9.0m)", "highway=motorway", "oneway=yes", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=-1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=-1", "lanes=1")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)", "highway=motorway", "oneway=-1", "lanes=2")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=-1", "lanes=3")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=-1", "lanes=4")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=no", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=no", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=no", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=no", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=no", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=road", "oneway=no", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=no", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "oneway=no", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=yes", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=yes", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=yes", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=yes", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=yes", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=yes", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=yes", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=yes", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=-1", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=-1", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=-1", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=-1", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=-1", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=road", "oneway=-1", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=-1", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=road", "oneway=-1", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=motorway", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=1.5m)", "highway=motorway", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=motorway", "oneway=no", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=3.0m)", "highway=motorway", "oneway=no", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=6.0m)", "highway=motorway", "oneway=no", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=yes", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=yes", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↑ 3.0m|Car ↑ 3.0m|Shoulder ↑ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=yes", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=-1", "shoulder=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=-1", "shoulder=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=-1", "shoulder=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=-1", "shoulder:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=-1", "shoulder=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Shoulder ↓ 3.0m|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=4.5m)", "highway=motorway", "oneway=-1", "shoulder:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=-1", "shoulder=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↓ 3.0m|Shoulder ↓ 3.0m|Edge, Center=1.5m)", "highway=motorway", "oneway=-1", "shoulder:right=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=3.0m)", "highway=road", "verge=no")]
        [InlineData("GetStraightRoadText", "Road(Edge|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Verge 2.0m|Edge, Center=5.0m)", "highway=road", "verge=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Verge 2.0m|Edge, Center=5.0m)", "highway=road", "verge=both")]
        [InlineData("GetStraightRoadText", "Road(Edge|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Verge 2.0m|Edge, Center=5.0m)", "highway=road", "verge:both=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=5.0m)", "highway=road", "verge=left")]
        [InlineData("GetStraightRoadText", "Road(Edge|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=5.0m)", "highway=road", "verge:left=yes")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Verge 2.0m|Edge, Center=3.0m)", "highway=road", "verge=right")]
        [InlineData("GetStraightRoadText", "Road(Edge|Car ↑ 3.0m|Car ↓ 3.0m|Verge 2.0m|Edge, Center=3.0m)", "highway=road", "verge:right=yes")]
        public void StraightRoadTest(string type, string expected, params string[] tags)
        {
            Assert.Equal("GetStraightRoadText", type);
            Assert.Equal(expected, GetStraightRoadText(tags));
        }

        [Fact]
        public void HighwayRoadCyclewayLaneShoulderBothParkingLaneBothParallelVergeBothSidewalkBoth()
        {
            Assert.Equal(
                "Road(Edge|Sidewalk 2.0m|Verge 2.0m|Parking 3.0m|Shoulder ↑ 3.0m|Cycle ↑ 1.0m|Car ↑ 3.0m|Car ↓ 3.0m|Cycle ↓ 1.0m|Shoulder ↓ 3.0m|Parking 3.0m|Verge 2.0m|Sidewalk 2.0m|Edge, Center=14.0m)",
                GetStraightRoadText("highway=road", "cycleway=lane", "shoulder=both", "parking:lane:both=parallel", "verge=both", "sidewalk=both")
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
            // Ambigous with a cycleway on the left/right side of the road: Assert.Equal(road, GetStraightRoadText("highway=road", "oneway=yes", "lanes=2", "cycleway=lane"));
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
            //     "Road(Edge|Cycle ↑ 1.0m|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Verge 2.0m|Cycle ↓ 1.0m|Edge, Center=4.5m)",
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
            //     "Road(Edge|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=5.5m)",
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
            //     "Road(Edge|Cycle ↑ 1.0m|Cycle ↓ 1.0m|Verge 2.0m|Car ↑ 3.0m|Edge, Center=4.0m)",
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
            //     "Road(Edge|Cycle ↓ 1.0m|Verge 2.0m|Car ↑ 3.0m|Car ↓ 3.0m|Edge, Center=4.5m)",
            //     GetStraightRoadText("highway=road", "cycleway:left=track")
            // );
        }

        // TODO: Examples S1 through S5

        // TODO: Examples B1 through B6
    }
}
