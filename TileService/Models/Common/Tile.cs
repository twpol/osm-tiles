using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using TileService.Models.Geometry;
using Point = TileService.Models.Geometry.Point;

namespace TileService.Models.Common
{
    public class Tile
    {
        protected const double C = 40075016.686;

        public static readonly TileCache Cache = new(14, 16, 22, 16, (zoom, x, y) => new Tile(zoom, x, y), (zoom, x, y, copy) => new Tile(zoom, x, y, copy));

        public int Zoom { get; }
        public int X { get; }
        public int Y { get; }
        public Point NW { get; }
        public Point SE { get; }
        public float ImageScale { get; }

        public IEnumerable<string> Layers { get; private set; }
        public ImmutableList<Way> Rails { get; private set; }
        public ImmutableList<Way> Roads { get; private set; }
        public ImmutableList<Junction> RoadJunctions { get; private set; }

        Tile(int zoom, int x, int y)
        {
            Zoom = zoom;
            X = x;
            Y = y;
            NW = GetPointFromTile(zoom, x, y);
            SE = GetPointFromTile(zoom, x + 1, y + 1);
            ImageScale = (float)(1 / (C * Math.Cos(NW.Lat * Math.PI / 180) / Math.Pow(2, zoom + 8)));
        }

        Tile(int zoom, int x, int y, Tile copy)
            : this(zoom, x, y)
        {
            Debug.Assert(copy.Layers != null, "Cannot copy data from Tile without any data");

            Layers = copy.Layers;
            Rails = copy.Rails;
            Roads = copy.Roads;
            RoadJunctions = copy.RoadJunctions;
        }

        public override string ToString()
        {
            return $"{GetType().Name}({Zoom}, {X}, {Y})";
        }

        public PointF GetPointFromPoint(Point point)
        {
            return new PointF(
                (float)(256 * (point.Lon - NW.Lon) / (SE.Lon - NW.Lon)),
                (float)(256 * (point.Lat - NW.Lat) / (SE.Lat - NW.Lat))
            );
        }

        static Point GetPointFromTile(int zoom, int x, int y)
        {
            var n = Math.Pow(2, zoom);
            return new Point(
                180 / Math.PI * Math.Atan(Math.Sinh(Math.PI - (2 * Math.PI * y / n))),
                (x / n * 360) - 180
            );
        }

        async public Task<Tile> Load()
        {
            Debug.Assert(Layers == null, "Cannot load data for Tile more than once");

            var overpass = await Overpass.Query.GetTile(this);
            var overpassWays = overpass.elements.Where(element => element.type == "way" && element.tags != null).ToArray();
            var overpassNodes = overpass.elements.Where(element => element.type == "node").ToArray();
            var overpassNodesById = new Dictionary<long, Overpass.Element>(
                overpassNodes.Select(node =>
                {
                    return new KeyValuePair<long, Overpass.Element>(
                        node.id,
                        node
                    );
                })
            );

            Layers = ImmutableList.ToImmutableList(overpassWays.Select(way =>
            {
                return way.tags.GetValueOrDefault("layer", "0");
            }).Distinct().Where(layer => int.TryParse(layer, out var result))).Sort((a, b) =>
            {
                return int.Parse(a) - int.Parse(b);
            });

            var overpassRails = overpassWays.Where(way =>
            {
                if (way.tags.GetValueOrDefault("area", "no") == "yes")
                {
                    return false;
                }
                return way.tags.GetValueOrDefault("railway", "no") switch
                {
                    "abandoned" => true,
                    "construction" => true,
                    "disused" => true,
                    "funicular" => true,
                    "light_rail" => true,
                    "monorail" => true,
                    "narrow_gauge" => true,
                    "preserved" => true,
                    "rail" => true,
                    "subway" => true,
                    "tram" => true,
                    _ => false,
                };
            }).ToList();
            var overpassRailJunctions = overpassNodes.Where(node =>
            {
                return 1 < overpassRails.Where(road => road.nodes.Contains(node.id)).Count();
            });
            Rails = ImmutableList.ToImmutableList(overpassRails.Select(way =>
            {
                return new Way(
                    this,
                    way.tags,
                    way.nodes.Select(nodeId =>
                    {
                        var node = overpassNodesById[nodeId];
                        return new Point(node.lat, node.lon);
                    })
                );
            }));

            var overpassRoads = overpassWays.Where(way =>
            {
                if (way.tags.GetValueOrDefault("area", "no") == "yes")
                {
                    return false;
                }
                return way.tags.GetValueOrDefault("highway", "no") switch
                {
                    "motorway" => true,
                    "trunk" => true,
                    "primary" => true,
                    "secondary" => true,
                    "tertiary" => true,
                    "unclassified" => true,
                    "residential" => true,
                    "service" => true,
                    "motorway_link" => true,
                    "trunk_link" => true,
                    "primary_link" => true,
                    "secondary_link" => true,
                    "tertiary_link" => true,
                    _ => false,
                };
            }).ToList();
            var overpassRoadJunctions = overpassNodes.Where(node =>
            {
                return 1 < overpassRoads.Where(road => road.nodes.Contains(node.id)).Count();
            });
            Roads = ImmutableList.ToImmutableList(overpassRoads.Select(way =>
            {
                return new Way(
                    this,
                    way.tags,
                    way.nodes.Select(nodeId =>
                    {
                        var node = overpassNodesById[nodeId];
                        return new Point(node.lat, node.lon);
                    })
                );
            }));
            RoadJunctions = ImmutableList.ToImmutableList(overpassRoadJunctions.Select(node =>
            {
                return new Junction(
                    overpassRoads.Where(road => road.nodes.Contains(node.id)).Select(road =>
                    {
                        var way = Roads[overpassRoads.IndexOf(road)];
                        var point = way.Points[Array.IndexOf(road.nodes, node.id)];
                        return new WayPoint(way, point);
                    })
                );
            }));

            Console.WriteLine($"new {this}: {Rails.Count} rails, {Roads.Count} roads, {RoadJunctions.Count} road junctions");

            return this;
        }
    }
}
