using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TileService.Models.Common;

namespace TileService.Controllers.Overlays
{
    [Route("overlays")]
    public class OverlayController : Controller
    {
        const int ZoomMinimum = 16;
        const int ZoomMaximum = 22;

        // GET overlays/:type/:zoom/:x/:y.png
        [HttpGet("{type}/{zoom}/{x}/{y}.png")]
        public async Task<ActionResult> Get(string type, int zoom, int x, int y)
        {
            if (zoom < ZoomMinimum || zoom > ZoomMaximum)
            {
                return BadRequest();
            }

            var tile = await Tile.Cache.Get(zoom, x, y);

            var start = DateTimeOffset.UtcNow;
            var rails = type == "all" || type == "rails";
            var roads = type == "all" || type == "roads";
            var stream = Renderer.Render(tile, 256, rails: rails, roads: roads);
            var end = DateTimeOffset.UtcNow;
            Console.WriteLine($"Rendered {type} on {tile} in {(end - start).TotalMilliseconds:F0} ms");

            HttpContext.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=43200" });

            return File(stream, "image/png");
        }
    }
}
