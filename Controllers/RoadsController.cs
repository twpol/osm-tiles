using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.Primitives;

namespace osm_road_overlay.Controllers
{
    [Route("[controller]")]
    public class RoadsController : Controller
    {
        // GET roads/:zoom/:x/:y.png
        [HttpGet("{zoom}/{x}/{y}.png")]
        public FileStreamResult Get()
        {
            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context => context.DrawPolygon(
                new Rgba32(255, 0, 0),
                1,
                new PointF(0, 0),
                new PointF(256, 0),
                new PointF(256, 256),
                new PointF(0, 256),
                new PointF(0, 0)
            ));

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            return File(stream, "image/png");
        }
    }
}
