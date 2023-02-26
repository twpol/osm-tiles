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

            var stream = await Renderer.Render(type, zoom, x, y);

            HttpContext.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=43200" });

            return File(stream, "image/png");
        }
    }
}
