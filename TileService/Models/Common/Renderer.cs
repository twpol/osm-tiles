using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TileService.Models.Geometry;
using Point = TileService.Models.Geometry.Point;

namespace TileService.Models.Common
{
    public static class Renderer
    {
        const int LaneKerbZoomMinimum = 19;
        const int LaneLineZoomMinimum = 18;
        static readonly Rgba32 SidewalkColor = new(128, 128, 128);
        static readonly Pen KerbLine = new(new Rgba32(192, 192, 192), 1);
        static readonly Rgba32 ParkingColor = new(64, 64, 192);
        static readonly Rgba32 ShoulderColor = new(192, 192, 192);
        static readonly Rgba32 CycleLaneColor = new(64, 192, 64);
        static readonly Rgba32 CarLaneColor = new(0, 0, 0);
        static readonly Pen LaneLine = new(new Rgba32(255, 255, 255), 1, new float[] {
            10,
            5,
        });
        static readonly Pen LaneSameDirLine = new(new Rgba32(255, 255, 255), 1, new float[] {
            5,
            10,
        });
        static readonly HashSet<string> LaneTransitionKerb = new() {
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
        static readonly HashSet<string> LaneTransitionLine = new() {
            "Parking|Cycle",
            "Parking|Car",
            "Cycle|Car",
            "Car|Car",
            "Car|Cycle",
            "Car|Parking",
            "Cycle|Parking",
            "Cycle|Cycle",
        };

        const int DefaultGaugeMM = 1435;
        const float SleeperWidth = 1.8f; // Sleeper size of 250mm x 130mm x 2600mm vs default gauge is 1.8118466899
        const float BallastWidth = 2.8f; // Center-to-center spacing of 4.0m vs default gauge is 2.7874564460
        static readonly Pen RailLine = new(new Rgba32(192, 192, 192), 1);
        static readonly Pen DisusedRailLine = new(new Rgba32(64, 64, 64), 1);
        static readonly Rgba32 SleeperColour = new(133, 133, 130); // https://en.wikipedia.org/wiki/List_of_colors - battleship grey
        static readonly Rgba32 BallastColour = new(102, 66, 33); // https://en.wikipedia.org/wiki/List_of_colors - dark brown

        static SizeF GetOffset(Tile tile, Line line, Point point)
        {
            var lengthExtension = (float)Math.Cos(Angle.Difference(line.Angle, point.Angle).Radians);
            if (lengthExtension < 0.5)
            {
                Console.WriteLine($"Warning: Unusual length extension for road offset (tile={tile}, line={line.Angle}, point={point.Angle}, extension={lengthExtension})");
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

        public static MemoryStream Render(Tile tile, bool rails, bool roads)
        {
            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context =>
            {
                foreach (var layer in tile.Layers)
                {
                    if (rails)
                    {
                        RenderRails(tile, layer, (way) =>
                        {
                            if (!float.TryParse(way.Tags.GetValueOrDefault("gauge", "unknown"), out var gauge)) gauge = DefaultGaugeMM;
                            var width = gauge / 1000 * BallastWidth;
                            RenderRailSegments(way, (line) =>
                            {
                                var point1 = tile.GetPointFromPoint(line.Start);
                                var point2 = tile.GetPointFromPoint(line.End);
                                var offsetDir1 = GetOffset(tile, line, line.Start);
                                var offsetDir2 = GetOffset(tile, line, line.End);
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
                                    var offsetDir1 = GetOffset(tile, line, line.Start);
                                    var offsetDir2 = GetOffset(tile, line, line.End);
                                    RenderLane(context, SleeperColour, point1, point2, offsetDir1, offsetDir2, -width / 2, width / 2);
                                });
                            }
                        });
                    }
                    if (roads)
                    {
                        RenderRoads(tile, layer, (way) =>
                        {
                            var road = way.Road;
                            if (road.Lanes.Count > 0)
                            {
                                RenderRoadSegments(way, (line) =>
                                {
                                    var point1 = tile.GetPointFromPoint(line.Start);
                                    var point2 = tile.GetPointFromPoint(line.End);
                                    var offsetDir1 = GetOffset(tile, line, line.Start);
                                    var offsetDir2 = GetOffset(tile, line, line.End);

                                    var offset1 = -road.Center;
                                    foreach (var lane in road.Lanes)
                                    {
                                        var offset2 = offset1 + lane.Width;
                                        if (lane.Type == LaneType.Sidewalk)
                                        {
                                            RenderLane(context, SidewalkColor, point1, point2, offsetDir1, offsetDir2, offset1, offset2);
                                        }
                                        offset1 = offset2;
                                    }
                                });
                            }
                        });
                        RenderRoads(tile, layer, (way) =>
                        {
                            var road = way.Road;
                            if (road.Lanes.Count > 0)
                            {
                                RenderRoadSegments(way, (line) =>
                                {
                                    var point1 = tile.GetPointFromPoint(line.Start);
                                    var point2 = tile.GetPointFromPoint(line.End);
                                    var offsetDir1 = GetOffset(tile, line, line.Start);
                                    var offsetDir2 = GetOffset(tile, line, line.End);

                                    var offset1 = -road.Center;
                                    foreach (var lane in road.Lanes)
                                    {
                                        var offset2 = offset1 + lane.Width;
                                        if (lane.Type == LaneType.Parking)
                                        {
                                            RenderLane(context, ParkingColor, point1, point2, offsetDir1, offsetDir2, offset1, offset2);
                                        }
                                        else if (lane.Type == LaneType.Shoulder)
                                        {
                                            RenderLane(context, ShoulderColor, point1, point2, offsetDir1, offsetDir2, offset1, offset2);
                                        }
                                        else if (lane.Type == LaneType.Cycle)
                                        {
                                            RenderLane(context, CycleLaneColor, point1, point2, offsetDir1, offsetDir2, offset1, offset2);
                                        }
                                        else if (lane.Type == LaneType.Car)
                                        {
                                            RenderLane(context, CarLaneColor, point1, point2, offsetDir1, offsetDir2, offset1, offset2);
                                        }
                                        offset1 = offset2;
                                    }
                                });
                            }
                        });
                        if (tile.Zoom >= LaneKerbZoomMinimum || tile.Zoom >= LaneLineZoomMinimum)
                        {
                            RenderRoads(tile, layer, (way) =>
                            {
                                var road = way.Road;
                                if (road.Lanes.Count > 0)
                                {
                                    var points = way.Segments
                                        .Select(segment => tile.GetPointFromPoint(segment.Start))
                                        .Append(tile.GetPointFromPoint(way.Segments.Last().End))
                                        .ToList();

                                    var offsetDirs = way.Segments
                                        .Select(segment => GetOffset(tile, segment, segment.Start))
                                        .Append(GetOffset(tile, way.Segments.Last(), way.Segments.Last().End))
                                        .ToList();

                                    var offset = -road.Center + road.Lanes[0].Width;
                                    for (var laneIndex = 1; laneIndex < road.Lanes.Count; laneIndex++)
                                    {
                                        var transition = $"{road.Lanes[laneIndex - 1].Type}|{road.Lanes[laneIndex].Type}";
                                        var lanePoints = Enumerable.Range(0, way.Segments.Count + 1)
                                            .Select(index => Offset(points[index], offsetDirs[index], offset))
                                            .ToArray();

                                        if (LaneTransitionKerb.Contains(transition) && tile.Zoom >= LaneKerbZoomMinimum)
                                        {
                                            context.DrawLines(
                                                KerbLine,
                                                lanePoints
                                            );
                                        }

                                        if (LaneTransitionLine.Contains(transition) && tile.Zoom >= LaneLineZoomMinimum)
                                        {
                                            var laneLine = road.Lanes[laneIndex - 1].Direction == road.Lanes[laneIndex].Direction
                                                ? LaneSameDirLine
                                                : LaneLine;
                                            context.DrawLines(
                                                laneLine,
                                                lanePoints
                                            );
                                        }

                                        offset += road.Lanes[laneIndex].Width;
                                    }
                                }
                            });
                        }
                    }
                    if (rails)
                    {
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
                                    .Select(segment => GetOffset(tile, segment, segment.Start))
                                    .Append(GetOffset(tile, way.Segments.Last(), way.Segments.Last().End))
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
                }
            });

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            return stream;
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

        static void RenderRoads(Tile tile, string layer, Action<Way> render)
        {
            foreach (var way in tile.Roads)
            {
                if (way.Tags.GetValueOrDefault("layer", "0") == layer)
                {
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

        static void RenderRails(Tile tile, string layer, Action<Way> render)
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
