using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using osm_road_overlay.Models.Geometry;
using osm_road_overlay.Models.Overpass;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Drawing.Pens;
using PointF = SixLabors.Primitives.PointF;
using SizeF = SixLabors.Primitives.SizeF;

namespace osm_road_overlay.Controllers.Overlays
{
    [Route("overlays/roads")]
    public class RoadsController : Controller
    {
        const float LaneWidthCycle = 0.333f;

        static string GetBoundingBoxFromTile(Tile tile, double oversize)
        {
            var latExtra = oversize * (tile.NW.Lat - tile.SE.Lat);
            var lonExtra = oversize * (tile.SE.Lon - tile.NW.Lon);
            return $"{tile.SE.Lat - latExtra},{tile.NW.Lon - lonExtra},{tile.NW.Lat + latExtra},{tile.SE.Lon + lonExtra}";
        }

        static PointF GetPointFromNode(Tile tile, Point point) {
            return new PointF(
                (float)(256 * (point.Lon - tile.NW.Lon) / (tile.SE.Lon - tile.NW.Lon)),
                (float)(256 * (point.Lat - tile.NW.Lat) / (tile.SE.Lat - tile.NW.Lat))
            );
        }

        static SizeF Vector(PointF start, PointF end) {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            return new SizeF() {
                Width = (float)(dx / length),
                Height = (float)(dy / length)
            };
        }

        static SizeF Flip(SizeF point) {
            return new SizeF() {
                Width = -point.Height,
                Height = point.Width
            };
        }

        static PointF Offset(PointF point, SizeF direction, float distance) {
            return new PointF() {
                X = point.X + direction.Width * distance,
                Y = point.Y + direction.Height * distance,
            };
        }

        // GET overlays/roads/:zoom/:x/:y.png
        [HttpGet("{zoom}/{x}/{y}.png")]
        public async Task<ActionResult> Get(int zoom, int x, int y)
        {
            if (zoom < 18) {
                return BadRequest();
            }

            var tile = new Tile(zoom, x, y);
            var bbox = GetBoundingBoxFromTile(tile, 0.5);
            var overpassQuery = $"[out:json][timeout:60];(way[\"highway\"]({bbox}););out body;>;out skel qt;";
            var world = await Models.Overpass.Query.GetGeometry(overpassQuery);

            var laneWidth = (float)(2 * tile.ImageScale);

            var sidewalkColor = new Rgba32(128, 128, 128);
            var kerbColor = new Rgba32(64, 64, 64);
            var roadColor = new Rgba32(192, 192, 192);
            var laneLine = new Pen<Rgba32>(new Rgba32(255, 255, 255), 1, new float[] {
                10,
                5,
            });

            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context =>
            {
                RenderWays(world, (way) => {
                    var lanes = GetLanes(way);
                    var sidewalk = way.Tags.GetValueOrDefault("sidewalk", "no");
                    var sidewalkLeft = sidewalk == "both" || sidewalk == "left";
                    var sidewalkRight = sidewalk == "both" || sidewalk == "right";
                    if (lanes.Count > 0 && (sidewalkLeft || sidewalkRight)) {
                        RenderWaySegments(way, (line) => {
                            var point1 = GetPointFromNode(tile, line.Start);
                            var point2 = GetPointFromNode(tile, line.End);
                            var dir = Vector(point1, point2);
                            var offDir = Flip(dir);
                            var halfWidth = laneWidth * lanes.Sum() / 2 + 2;
                            var offsetLeft = halfWidth + (sidewalkLeft ? laneWidth / 2 : 0);
                            var offsetRight = halfWidth + (sidewalkRight ? laneWidth / 2 : 0);
                            context.FillPolygon(
                                sidewalkColor,
                                Offset(point1, offDir, -offsetLeft),
                                Offset(point1, dir, -halfWidth),
                                Offset(point1, offDir, offsetRight),
                                Offset(point2, offDir, offsetRight),
                                Offset(point2, dir, halfWidth),
                                Offset(point2, offDir, -offsetLeft)
                            );
                        });
                    }
                });
                RenderWays(world, (way) => {
                    var lanes = GetLanes(way);
                    if (lanes.Count > 0) {
                        RenderWaySegments(way, (line) => {
                            var point1 = GetPointFromNode(tile, line.Start);
                            var point2 = GetPointFromNode(tile, line.End);
                            var dir = Vector(point1, point2);
                            var offDir = Flip(dir);
                            var halfWidth = laneWidth * lanes.Sum() / 2 + 2;
                            context.FillPolygon(
                                kerbColor,
                                Offset(point1, offDir, halfWidth),
                                Offset(point1, dir, -halfWidth),
                                Offset(point1, offDir, -halfWidth),
                                Offset(point2, offDir, -halfWidth),
                                Offset(point2, dir, halfWidth),
                                Offset(point2, offDir, halfWidth)
                            );
                        });
                    }
                });
                RenderWays(world, (way) => {
                    var lanes = GetLanes(way);
                    if (lanes.Count > 0) {
                        RenderWaySegments(way, (line) => {
                            var point1 = GetPointFromNode(tile, line.Start);
                            var point2 = GetPointFromNode(tile, line.End);
                            var dir = Vector(point1, point2);
                            var offDir = Flip(dir);
                            var halfWidth = laneWidth * lanes.Sum() / 2 + 1;
                            context.FillPolygon(
                                roadColor,
                                Offset(point1, offDir, halfWidth),
                                Offset(point1, dir, -halfWidth),
                                Offset(point1, offDir, -halfWidth),
                                Offset(point2, offDir, -halfWidth),
                                Offset(point2, dir, halfWidth),
                                Offset(point2, offDir, halfWidth)
                            );
                        });
                    }
                });
                RenderWays(world, (way) => {
                    var lanes = GetLanes(way);
                    if (lanes.Count > 1) {
                        RenderWaySegments(way, (line) => {
                            var point1 = GetPointFromNode(tile, line.Start);
                            var point2 = GetPointFromNode(tile, line.End);
                            var dir = Vector(point1, point2);
                            var offDir = Flip(dir);
                            var laneOffset = -lanes.Sum() / 2;
                            for (var laneIndex = 0; laneIndex < lanes.Count - 1; laneIndex++) {
                                laneOffset += lanes[laneIndex];
                                context.DrawLines(
                                    laneLine,
                                    Offset(point1, offDir, laneWidth * laneOffset),
                                    Offset(point2, offDir, laneWidth * laneOffset)
                                );
                            }
                        });
                    }
                });
            });

            Console.WriteLine($"{DateTimeOffset.Now}  Tile generated: {zoom}/{x}/{y}.png");

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            return File(stream, "image/png");
        }

        static List<float> GetLanes(Way way) {
            var lanes = new List<float>();
            var drivingLanes = GetDrivingLanes(way);
            if (drivingLanes == 0) {
                return lanes;
            }
            for (var i = 0; i < drivingLanes; i++) {
                lanes.Add(1);
            }
            var parkingLeftLanes = GetParkingLanes(way, ":left");
            if (parkingLeftLanes > 0) {
                lanes.Insert(0, parkingLeftLanes);
            }
            var parkingRightLanes = GetParkingLanes(way, ":right");
            if (parkingRightLanes > 0) {
                lanes.Add(parkingRightLanes);
            }
            var parkingBothLanes = GetParkingLanes(way, ":both");
            if (parkingBothLanes > 0) {
                lanes.Insert(0, parkingBothLanes);
                lanes.Add(parkingBothLanes);
            }
            if (way.Tags.GetValueOrDefault("cycleway", "no") == "lane") {
                lanes.Insert(0, LaneWidthCycle);
                lanes.Add(LaneWidthCycle);
            } else if (way.Tags.GetValueOrDefault("cycleway", "no") == "opposite") {
                lanes.Add(LaneWidthCycle);
            } else {
                if (way.Tags.GetValueOrDefault("cycleway:left", "no") == "lane") {
                    lanes.Insert(0, LaneWidthCycle);
                }
                if (way.Tags.GetValueOrDefault("cycleway:right", "no") == "lane") {
                    lanes.Add(LaneWidthCycle);
                }
            }
            return lanes;
        }

        static int GetDrivingLanes(Way way) {
            switch (way.Tags.GetValueOrDefault("highway", "no")) {
                case "motorway":
                case "trunk":
                case "primary":
                case "secondary":
                case "tertiary":
                case "unclassified":
                case "residential":
                case "service":
                case "motorway_link":
                case "trunk_link":
                case "primary_link":
                case "secondary_link":
                case "tertiary_link":
                    var defaultLanes = way.Tags.GetValueOrDefault("oneway", "no") == "yes" ? "1" : "2";
                    return int.Parse(way.Tags.GetValueOrDefault("lanes", defaultLanes));
                default:
                    return 0;
            }
        }

        static float GetParkingLanes(Way way, string side) {
            switch (way.Tags.GetValueOrDefault("parking:lane" + side, "no")) {
                case "parallel":
                    return 1;
                case "diagonal":
                    return 1.5f;
                case "perpendicular":
                    return 2;
                default:
                    return 0;
            }
        }

        static void RenderWays(World world, Action<Way> render)
        {
            foreach (var way in world.Ways)
            {
                if (way.Tags.GetValueOrDefault("layer", "0") != "0") {
                    continue;
                }
                if (way.Tags.GetValueOrDefault("area", "no") == "yes") {
                    continue;
                }
                render(way);
            }
        }

        static void RenderWaySegments(Way way, Action<Line> render)
        {
            foreach (var segment in way.Segments)
            {
                render(segment);
            }
        }
    }
}
