using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TileService.Models.Common;
using TileService.Models.Overpass;

namespace TileService.Controllers.Overlays
{
    [Route("test")]
    public class TestController : Controller
    {
        // GET test/:zoom/:type.png
        [HttpGet("{zoom}/{type}.png")]
        public ActionResult Get(int zoom, string type, string[] ways)
        {
            Console.WriteLine($"Test: type={type} {string.Join(" ", ways.Select(way => $"ways={way.Replace("\n", ",")}"))}");

            var tile = new Tile(zoom, 0, 0);
            var tileCenter = (tile.NW + tile.SE) / 2;
            var tileSize = tile.SE - tile.NW;
            var wayGaps = ways.Where(way => way.StartsWith("-")).Select(way => way.Length).Prepend(0).CumulativeSum().ToArray();
            var wayGapTotal = wayGaps.Last();
            var overpassWays = ways.Where(way => !way.StartsWith("-")).Select((way, i) => new Element()
            {
                id = 2001 + i,
                type = "way",
                tags = way.Split('\n').Where(tag => tag.Contains('=')).Select(tag => tag.Split('=')).ToDictionary(tag => tag[0], tag => tag[1]),
                nodes = new long[] { 1001, 1002 + i },
            }).ToArray();
            Debug.Assert(wayGaps.Length == overpassWays.Length + 1, "Number of ways and gaps must match");
            var overpassNodes = Enumerable.Range(0, wayGaps.Length).Select(i => new Element()
            {
                id = 1001 + i,
                type = "node",
                lat = tileCenter.Lat + (i == 0 ? 0 : Math.Sin(wayGaps[i] * 2 * Math.PI / wayGapTotal) * tileSize.Lat * Math.Sqrt(2) / 2),
                lon = tileCenter.Lon + (i == 0 ? 0 : Math.Cos(wayGaps[i] * 2 * Math.PI / wayGapTotal) * tileSize.Lon * Math.Sqrt(2) / 2),
            }).ToArray();
            tile.Load(overpassWays, overpassNodes);

            var start = DateTimeOffset.UtcNow;
            var stream = Renderer.Render(tile, 1024, rails: true, roads: true, debug: false);
            var end = DateTimeOffset.UtcNow;
            Console.WriteLine($"Rendered {type} on {tile} in {(end - start).TotalMilliseconds:F0} ms");

            HttpContext.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=43200" });

            return File(stream, "image/png");
        }
    }
}
