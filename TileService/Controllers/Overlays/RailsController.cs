using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using TileService.Models.Geometry;
using Point = TileService.Models.Geometry.Point;

namespace TileService.Controllers.Overlays
{
    [Route("overlays/rails")]
    public class RailController : Controller
    {
        const int ZoomMinimum = 16;
        const int ZoomMaximum = 22;
        const int DefaultGaugeMM = 1435;
        const float SleeperWidth = 1.8f; // Sleeper size of 250mm x 130mm x 2600mm vs default gauge is 1.8118466899
        const float BallastWidth = 2.8f; // Center-to-center spacing of 4.0m vs default gauge is 2.7874564460
        static readonly Pen RailLine = new Pen(new Rgba32(192, 192, 192), 1);
        static readonly Pen DisusedRailLine = new Pen(new Rgba32(64, 64, 64), 1);
        static readonly Rgba32 SleeperColour = new Rgba32(133, 133, 130); // https://en.wikipedia.org/wiki/List_of_colors - battleship grey
        static readonly Rgba32 BallastColour = new Rgba32(102, 66, 33); // https://en.wikipedia.org/wiki/List_of_colors - dark brown

        static SizeF GetRailOffset(RailTile tile, Line line, Point point)
        {
            var lengthExtension = (float)Math.Cos(Angle.Difference(line.Angle, point.Angle).Radians);
            if (lengthExtension < 0.5)
            {
                Console.WriteLine($"Warning: Unusual length extension for rail offset (tile={tile}, line={line.Angle}, point={point.Angle}, extension={lengthExtension})");
                lengthExtension = 0.5f;
            }
            var sin = (float)Math.Sin(Angle.Subtract(Angle.QuarterTurn, point.Angle).Radians);
            var cos = (float)Math.Cos(Angle.Subtract(Angle.QuarterTurn, point.Angle).Radians);
            return new SizeF()
            {
                Width = -tile.ImageScale / lengthExtension * cos,
                Height = tile.ImageScale / lengthExtension * sin
            };
        }

        static PointF Offset(PointF point, SizeF direction, float distance)
        {
            return new PointF()
            {
                X = point.X + direction.Width * distance,
                Y = point.Y + direction.Height * distance,
            };
        }

        // GET overlays/rails/:zoom/:x/:y.png
        [HttpGet("{zoom}/{x}/{y}.png")]
        public async Task<ActionResult> Get(int zoom, int x, int y)
        {
            if (zoom < ZoomMinimum || zoom > ZoomMaximum)
            {
                return BadRequest();
            }

            var start = DateTimeOffset.UtcNow;

            var tile = await RailTile.Cache.Get(zoom, x, y);

            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context =>
            {
                foreach (var layer in tile.Layers)
                {
                    RenderRails(tile, layer, (way) =>
                    {
                        if (!float.TryParse(way.Tags.GetValueOrDefault("gauge", "unknown"), out var gauge)) gauge = DefaultGaugeMM;
                        var width = gauge / 1000 * BallastWidth;
                        RenderRailSegments(way, (line) =>
                        {
                            var point1 = tile.GetPointFromPoint(line.Start);
                            var point2 = tile.GetPointFromPoint(line.End);
                            var offsetDir1 = GetRailOffset(tile, line, line.Start);
                            var offsetDir2 = GetRailOffset(tile, line, line.End);
                            RenderLane(context, BallastColour, point1, point2, offsetDir1, offsetDir2, -width / 2, width / 2);
                        });
                    });
                    RenderRails(tile, layer, (way) =>
                    {
                        var railway = way.Tags.GetValueOrDefault("railway", "no");
                        if (railway != "abandoned")
                        {
                            if (!float.TryParse(way.Tags.GetValueOrDefault("gauge", "unknown"), out var gauge)) gauge = DefaultGaugeMM;
                            var width = gauge / 1000 * SleeperWidth;
                            RenderRailSegments(way, (line) =>
                            {
                                var point1 = tile.GetPointFromPoint(line.Start);
                                var point2 = tile.GetPointFromPoint(line.End);
                                var offsetDir1 = GetRailOffset(tile, line, line.Start);
                                var offsetDir2 = GetRailOffset(tile, line, line.End);
                                RenderLane(context, SleeperColour, point1, point2, offsetDir1, offsetDir2, -width / 2, width / 2);
                            });
                        }
                    });
                    RenderRails(tile, layer, (way) =>
                    {
                        var railway = way.Tags.GetValueOrDefault("railway", "no");
                        if (railway != "abandoned" && railway != "construction")
                        {
                            if (!float.TryParse(way.Tags.GetValueOrDefault("gauge", "unknown"), out var gauge)) gauge = DefaultGaugeMM;
                            var width = gauge / 1000;

                            var points = way.Segments
                                .Select(segment => tile.GetPointFromPoint(segment.Start))
                                .Append(tile.GetPointFromPoint(way.Segments.Last().End))
                                .ToList();

                            var offsetDirs = way.Segments
                                .Select(segment => GetRailOffset(tile, segment, segment.Start))
                                .Append(GetRailOffset(tile, way.Segments.Last(), way.Segments.Last().End))
                                .ToList();

                            var railPoints1 = Enumerable.Range(0, way.Segments.Count + 1)
                                .Select(index => Offset(points[index], offsetDirs[index], -width / 2))
                                .ToArray();

                            var railPoints2 = Enumerable.Range(0, way.Segments.Count + 1)
                                .Select(index => Offset(points[index], offsetDirs[index], width / 2))
                                .ToArray();

                            var line = railway == "disused" ? DisusedRailLine : RailLine;

                            context.DrawLines(line, railPoints1);
                            context.DrawLines(line, railPoints2);
                        }
                    });
                }
            });

            var end = DateTimeOffset.UtcNow;
            Console.WriteLine($"Rendered {tile} in {(end - start).TotalMilliseconds:F0} ms");

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            HttpContext.Response.Headers.Add("Cache-Control", new[] { "public", "max-age=43200" });

            return File(stream, "image/png");
        }

        static void RenderLane(IImageProcessingContext context, Rgba32 color, PointF point1, PointF point2, SizeF offsetDir1, SizeF offsetDir2, float offset1, float offset2)
        {
            context.FillPolygon(
                color,
                Offset(point1, offsetDir1, offset1),
                Offset(point1, offsetDir1, offset2),
                Offset(point2, offsetDir2, offset2),
                Offset(point2, offsetDir2, offset1)
            );
        }

        static void RenderRails(RailTile tile, string layer, Action<Way> render)
        {
            foreach (var way in tile.Rails)
            {
                if (way.Tags.GetValueOrDefault("layer", "0") == layer)
                {
                    render(way);
                }
            }
        }

        static void RenderRailSegments(Way way, Action<Line> render)
        {
            foreach (var segment in way.Segments)
            {
                render(segment);
            }
        }
    }
}
