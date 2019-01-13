using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TileService.Models.Geometry;
using PointF = SixLabors.Primitives.PointF;

namespace TileService.Controllers.Overlays
{
    [Route("overlays/completeness")]
    public class CompletenessController : Controller
    {
        const int ZoomMinimum = 10;
        const int ZoomMaximum = 10;

        // GET overlays/completeness/:zoom/:x/:y.png
        [HttpGet("{zoom}/{x}/{y}.png")]
        public async Task<ActionResult> Get(int zoom, int x, int y)
        {
            if (zoom < ZoomMinimum || zoom > ZoomMaximum) {
                return BadRequest();
            }

            var start = DateTimeOffset.UtcNow;

            var tile = await CompletenessTile.Cache.Get(zoom, x, y);

            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context =>
            {
                var color = new Rgba32(1 - tile.Completeness, tile.Completeness, 0, 0.5f);
                context.FillPolygon(
                    color,
                    new PointF(0, 0),
                    new PointF(256, 0),
                    new PointF(256, 256),
                    new PointF(0, 256)
                );
            });

            var end = DateTimeOffset.UtcNow;
            Console.WriteLine($"Rendered {tile} in {(end - start).TotalMilliseconds:F0} ms");

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            HttpContext.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=43200" });

            return File(stream, "image/png");
        }
    }
}
