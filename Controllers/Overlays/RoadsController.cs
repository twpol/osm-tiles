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

        static SizeF GetRoadOffset(Tile tile, Line line, Point point) {
            var lengthExtension = (float)Math.Cos(Angle.Difference(line.Angle, point.Angle).Radians);
            if (lengthExtension < 0.5) {
                Console.WriteLine($"Warning: Unusual length extension for road offset (tile={tile}, line={line.Angle}, point={point.Angle}, extension={lengthExtension})");
                lengthExtension = 0.5f;
            }
            var sin = (float)Math.Sin(Angle.Subtract(Angle.QuarterTurn, point.Angle).Radians);
            var cos = (float)Math.Cos(Angle.Subtract(Angle.QuarterTurn, point.Angle).Radians);
            return new SizeF() {
                Width = -tile.ImageScale / lengthExtension * cos,
                Height = tile.ImageScale / lengthExtension * sin
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
                foreach (var layer in tile.Layers) {
                    RenderRoads(tile, layer, (way) => {
                        var road = GetRoad(way);
                        if (road.Lanes.Count > 0) {
                            RenderRoadSegments(way, (line) => {
                                var point1 = tile.GetPointFromPoint(line.Start);
                                var point2 = tile.GetPointFromPoint(line.End);
                                var offsetDir1 = GetRoadOffset(tile, line, line.Start);
                                var offsetDir2 = GetRoadOffset(tile, line, line.End);

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
                    RenderRoads(tile, layer, (way) => {
                        var road = GetRoad(way);
                        if (road.Lanes.Count > 0) {
                            RenderRoadSegments(way, (line) => {
                                var point1 = tile.GetPointFromPoint(line.Start);
                                var point2 = tile.GetPointFromPoint(line.End);
                                var offsetDir1 = GetRoadOffset(tile, line, line.Start);
                                var offsetDir2 = GetRoadOffset(tile, line, line.End);

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
                    RenderRoads(tile, layer, (way) => {
                        var road = GetRoad(way);
                        if (road.Lanes.Count > 0) {
                            RenderRoadSegments(way, (line) => {
                                var point1 = tile.GetPointFromPoint(line.Start);
                                var point2 = tile.GetPointFromPoint(line.End);
                                var offsetDir1 = GetRoadOffset(tile, line, line.Start);
                                var offsetDir2 = GetRoadOffset(tile, line, line.End);

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
                }
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

            var cycleway = way.Tags.GetValueOrDefault("cycleway", "no");
            var cyclewayBoth = way.Tags.GetValueOrDefault("cycleway:both", "no");
            if (cycleway == "lane" || cyclewayBoth == "lane") {
                lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
                center += LaneWidthCycle;
            } else if (cycleway == "opposite_lane") {
                lanes.Add(new Lane(LaneType.Cycle, LaneWidthCycle));
            } else {
                var cyclewayLeft = way.Tags.GetValueOrDefault("cycleway:left", "no");
                var cyclewayRight = way.Tags.GetValueOrDefault("cycleway:right", "no");
                if (cyclewayLeft == "lane" || cyclewayRight == "opposite_lane") {
                    lanes.Insert(0, new Lane(LaneType.Cycle, LaneWidthCycle));
                    center += LaneWidthCycle;
                }
                if (cyclewayLeft == "opposite_lane" || cyclewayRight == "lane") {
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
            var defaultLanes = way.Tags.GetValueOrDefault("oneway", "no") == "yes" ? "1" : "2";
            return int.Parse(way.Tags.GetValueOrDefault("lanes", defaultLanes));
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

        static void RenderRoads(Tile tile, string layer, Action<Way> render)
        {
            foreach (var way in tile.Roads) {
                if (way.Tags.GetValueOrDefault("layer", "0") == layer) {
                    render(way);
                }
            }
        }

        static void RenderRoadSegments(Way way, Action<Line> render)
        {
            foreach (var segment in way.Segments)
            {
                render(segment);
            }
        }
    }
}
