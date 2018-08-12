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
        static readonly float LaneWidthSidewalk = 2.0f;
        static readonly float LaneWidthCycle = 1.0f;
        static readonly float LaneWidthCar = 3.0f;
        static readonly Rgba32 SidewalkColor = new Rgba32(128, 128, 128);
        static readonly Pen<Rgba32> KerbLine = new Pen<Rgba32>(new Rgba32(64, 64, 64), 1);
        static readonly Rgba32 RoadColor = new Rgba32(192, 192, 192);
        static readonly Pen<Rgba32> LaneLine = new Pen<Rgba32>(new Rgba32(255, 255, 255), 1, new float[] {
            10,
            5,
        });
        static readonly HashSet<string> LaneTransitionKerb = new HashSet<string>() {
            "Edge|Parking",
            "Edge|Cycle",
            "Edge|Car",
            "Sidewalk|Parking",
            "Sidewalk|Cycle",
            "Sidewalk|Car",
            "Parking|Sidewalk",
            "Cycle|Sidewalk",
            "Car|Sidewalk",
            "Parking|Edge",
            "Cycle|Edge",
            "Car|Edge",
        };
        static readonly HashSet<string> LaneTransitionLine = new HashSet<string>() {
            "Parking|Cycle",
            "Parking|Car",
            "Cycle|Car",
            "Car|Car",
            "Car|Cycle",
            "Car|Parking",
            "Cycle|Parking",
        };

        static SizeF GetRoadOffset(float imageScale, Line line, Point point) {
            var angleDifference = Math.Abs(line.AngleRad - point.AngleRad);
            var lengthExtension = (float)Math.Cos(angleDifference);
            var sin = (float)Math.Sin(Math.PI / 2 - point.AngleRad);
            var cos = (float)Math.Cos(Math.PI / 2 - point.AngleRad);
            return new SizeF() {
                Width = -imageScale / lengthExtension * cos,
                Height = imageScale / lengthExtension * sin
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
            if (zoom < 16 || zoom > 22) {
                return BadRequest();
            }

            var start = DateTimeOffset.UtcNow;

            var tile = await Tile.Get(zoom, x, y);

            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context =>
            {
                RenderWays(tile, (way) => {
                    var road = GetRoad(way);
                    if (road.Lanes.Count > 0) {
                        RenderWaySegments(way, (line) => {
                            var point1 = tile.GetPointFromPoint(line.Start);
                            var point2 = tile.GetPointFromPoint(line.End);
                            var offsetDir1 = GetRoadOffset(tile.ImageScale, line, line.Start);
                            var offsetDir2 = GetRoadOffset(tile.ImageScale, line, line.End);

                            var offset1 = -road.Center;
                            foreach (var lane in road.Lanes)
                            {
                                var offset2 = offset1 + lane.Width;
                                if (lane.Type == LaneType.Sidewalk) {
                                    context.FillPolygon(
                                        SidewalkColor,
                                        Offset(point1, offsetDir1, offset1),
                                        Offset(point1, offsetDir1, offset2),
                                        Offset(point2, offsetDir2, offset2),
                                        Offset(point2, offsetDir2, offset1)
                                    );
                                }
                                offset1 = offset2;
                            }
                        });
                    }
                });
                RenderWays(tile, (way) => {
                    var road = GetRoad(way);
                    if (road.Lanes.Count > 0) {
                        RenderWaySegments(way, (line) => {
                            var point1 = tile.GetPointFromPoint(line.Start);
                            var point2 = tile.GetPointFromPoint(line.End);
                            var offsetDir1 = GetRoadOffset(tile.ImageScale, line, line.Start);
                            var offsetDir2 = GetRoadOffset(tile.ImageScale, line, line.End);

                            var offset1 = -road.Center;
                            foreach (var lane in road.Lanes)
                            {
                                var offset2 = offset1 + lane.Width;
                                if (lane.Type == LaneType.Parking || lane.Type == LaneType.Cycle || lane.Type == LaneType.Car) {
                                    context.FillPolygon(
                                        RoadColor,
                                        Offset(point1, offsetDir1, offset1),
                                        Offset(point1, offsetDir1, offset2),
                                        Offset(point2, offsetDir2, offset2),
                                        Offset(point2, offsetDir2, offset1)
                                    );
                                }
                                offset1 = offset2;
                            }
                        });
                    }
                });
                RenderWays(tile, (way) => {
                    var road = GetRoad(way);
                    if (road.Lanes.Count > 0) {
                        RenderWaySegments(way, (line) => {
                            var point1 = tile.GetPointFromPoint(line.Start);
                            var point2 = tile.GetPointFromPoint(line.End);
                            var offsetDir1 = GetRoadOffset(tile.ImageScale, line, line.Start);
                            var offsetDir2 = GetRoadOffset(tile.ImageScale, line, line.End);

                            var offset = -road.Center + road.Lanes[0].Width;
                            for (var laneIndex = 1; laneIndex < road.Lanes.Count; laneIndex++)
                            {
                                var transition = $"{road.Lanes[laneIndex - 1].Type}|{road.Lanes[laneIndex].Type}";

                                if (LaneTransitionKerb.Contains(transition)) {
                                    context.DrawLines(
                                        KerbLine,
                                        Offset(point1, offsetDir1, offset),
                                        Offset(point2, offsetDir2, offset)
                                    );
                                }

                                if (LaneTransitionLine.Contains(transition)) {
                                    context.DrawLines(
                                        LaneLine,
                                        Offset(point1, offsetDir1, offset),
                                        Offset(point2, offsetDir2, offset)
                                    );
                                }

                                offset += road.Lanes[laneIndex].Width;
                            }
                        });
                    }
                });
            });

            var end = DateTimeOffset.UtcNow;
            Console.WriteLine($"Rendered {tile} in {(end - start).TotalMilliseconds:F0} ms");

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            return File(stream, "image/png");
        }

        static Road GetRoad(Way way) {
            var lanes = new List<Lane>();
            var center = 0f;

            var drivingLanes = GetNumberOfDrivingLanes(way);
            if (drivingLanes == 0) {
                return new Road(lanes, center);
            }

            for (var i = 0; i < drivingLanes; i++) {
                lanes.Add(new Lane(LaneType.Car, LaneWidthCar));
            }
            center += LaneWidthCar * drivingLanes / 2;

            if (way.Tags.GetValueOrDefault("cycleway", "no") == "lane") {
                lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
                center += LaneWidthCycle;
            } else if (way.Tags.GetValueOrDefault("cycleway", "no") == "opposite") {
                lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
            } else {
                if (way.Tags.GetValueOrDefault("cycleway:left", "no") == "lane") {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                    center += LaneWidthCycle;
                }
                if (way.Tags.GetValueOrDefault("cycleway:right", "no") == "lane") {
                    lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
                }
            }

            var parkingLeftLanes = GetWidthOfParkingLanes(way, ":left");
            if (parkingLeftLanes > 0) {
                lanes.Insert(0, new Lane(LaneType.Parking, parkingLeftLanes));
                center += parkingLeftLanes;
            }
            var parkingRightLanes = GetWidthOfParkingLanes(way, ":right");
            if (parkingRightLanes > 0) {
                lanes.Add(new Lane(LaneType.Parking, parkingRightLanes));
            }
            var parkingBothLanes = GetWidthOfParkingLanes(way, ":both");
            if (parkingBothLanes > 0) {
                lanes.Insert(0, new Lane(LaneType.Parking, parkingBothLanes));
                lanes.Add(new Lane(LaneType.Parking, parkingBothLanes));
                center += parkingLeftLanes;
            }

            var sidewalk = way.Tags.GetValueOrDefault("sidewalk", "no");
            if (sidewalk == "both" || sidewalk == "left") {
                lanes.Insert(0, new Lane(LaneType.Sidewalk, LaneWidthSidewalk));
                center += LaneWidthSidewalk;
            }
            if (sidewalk == "both" || sidewalk == "right") {
                lanes.Add(new Lane(LaneType.Sidewalk, LaneWidthSidewalk));
            }

            lanes.Insert(0, new Lane(LaneType.Edge, 0));
            lanes.Add(new Lane(LaneType.Edge, 0));

            return new Road(lanes, center);
        }

        static int GetNumberOfDrivingLanes(Way way) {
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

        static float GetWidthOfParkingLanes(Way way, string side) {
            switch (way.Tags.GetValueOrDefault("parking:lane" + side, "no")) {
                case "parallel":
                    return LaneWidthCar;
                case "diagonal":
                    return LaneWidthCar * 1.5f;
                case "perpendicular":
                    return LaneWidthCar * 2;
                default:
                    return 0;
            }
        }

        static void RenderWays(Tile tile, Action<Way> render)
        {
            foreach (var way in tile.Ways)
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
