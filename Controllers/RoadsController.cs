using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        const string OverpassAPIEndpoint = "https://overpass-api.de/api/interpreter";

        struct LatLon {
            public double Lat;
            public double Lon;
        }

        static LatLon GetLatLonFromTile(int zoom, int x, int y) {
            var n = Math.Pow(2, zoom);
            return new LatLon {
                Lat = 180 / Math.PI * Math.Atan(Math.Sinh(Math.PI - (2 * Math.PI * y / n))),
                Lon = (x / n * 360) - 180,
            };
        }

        static void GetLatLonBoxFromTile(int zoom, int x, int y, out LatLon nw, out LatLon se)
        {
            nw = GetLatLonFromTile(zoom, x, y);
            se = GetLatLonFromTile(zoom, x + 1, y + 1);
        }

        static string GetBoundingBoxFromLatLonBox(LatLon nw, LatLon se, double oversize)
        {
            var latExtra = oversize * (nw.Lat - se.Lat);
            var lonExtra = oversize * (se.Lon - nw.Lon);
            return $"{se.Lat - latExtra},{nw.Lon - lonExtra},{nw.Lat + latExtra},{se.Lon + lonExtra}";
        }

        static PointF GetPointFromNode(LatLon nw, LatLon se, OverpassResponseElement node) {
            return new PointF(
                (float)(256 * (node.lon - nw.Lon) / (se.Lon - nw.Lon)),
                (float)(256 * (node.lat - nw.Lat) / (se.Lat - nw.Lat))
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
                Width = point.Height,
                Height = -point.Width
            };
        }

        static PointF Offset(PointF point, SizeF direction, float distance) {
            return new PointF() {
                X = point.X + direction.Width * distance,
                Y = point.Y + direction.Height * distance,
            };
        }

        struct OverpassResponse {
            public OverpassResponseElement[] elements;
        }

        struct OverpassResponseElement {
            public string type;
            public long id;
            public double lat;
            public double lon;
            public long[] nodes;
            public Dictionary<string, string> tags;
        }

        readonly HttpClient Client = new HttpClient();

        // GET roads/:zoom/:x/:y.png
        [HttpGet("{zoom}/{x}/{y}.png")]
        public async Task<ActionResult> Get(int zoom, int x, int y)
        {
            if (zoom < 18) {
                return BadRequest();
            }

            GetLatLonBoxFromTile(zoom, x, y, out var nw, out var se);
            var bbox = GetBoundingBoxFromLatLonBox(nw, se, 0.5);
            var overpassQuery = $"[out:json][timeout:60];(way[\"highway\"]({bbox}););out body;>;out skel qt;";

            OverpassResponse overpass;
            using (var overpassResponse = await Client.PostAsync(
                OverpassAPIEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "data", overpassQuery },
                })
            ))
            using (var overpassReader = new StreamReader(await overpassResponse.Content.ReadAsStreamAsync()))
            using (var overpassJson = new JsonTextReader(overpassReader)) {
                overpass = new JsonSerializer().Deserialize<OverpassResponse>(overpassJson);
                // while (await overpassJson.ReadAsync()) {
                //     Console.WriteLine($"JSON  {overpassJson.Path}  {overpassJson.TokenType}  {overpassJson.ValueType}  {overpassJson.Value}");
                // }
            }

            var ways = overpass.elements.Where(element => element.type == "way").ToArray();
            var nodes = overpass.elements.Where(element => element.type == "node").ToArray();
            var nodesById = new Dictionary<long, OverpassResponseElement>(
                nodes.Select(node => {
                    return new KeyValuePair<long, OverpassResponseElement>(
                        node.id,
                        node
                    );
                })
            );

            var C = 40075016.686;
            var imageScale = (C * Math.Cos(nw.Lat) / Math.Pow(2, zoom + 8));
            var laneWidth = (float)(3 / imageScale);

            var kerbColor = new Rgba32(64, 64, 64);
            var roadColor = new Rgba32(192, 192, 192);

            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context =>
            {
                RenderWays(ways, (way) => {
                    var lanes = GetLanes(way);
                    if (lanes > 0) {
                        RenderWaySegments(way, nodesById, (node1, node2) => {
                            var point1 = GetPointFromNode(nw, se, node1);
                            var point2 = GetPointFromNode(nw, se, node2);
                            var dir = Vector(point1, point2);
                            var offDir = Flip(dir);
                            var halfWidth = laneWidth * lanes / 2 + 2;
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
                RenderWays(ways, (way) => {
                    var lanes = GetLanes(way);
                    if (lanes > 0) {
                        RenderWaySegments(way, nodesById, (node1, node2) => {
                            var point1 = GetPointFromNode(nw, se, node1);
                            var point2 = GetPointFromNode(nw, se, node2);
                            var dir = Vector(point1, point2);
                            var offDir = Flip(dir);
                            var halfWidth = laneWidth * lanes / 2 + 1;
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
            });

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            return File(stream, "image/png");
        }

        static int GetLanes(OverpassResponseElement way) {
            switch (way.tags.GetValueOrDefault("highway", "no")) {
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
                    var defaultLanes = way.tags.GetValueOrDefault("oneway", "no") == "yes" ? "1" : "2";
                    return int.Parse(way.tags.GetValueOrDefault("lanes", defaultLanes));
                default:
                    return 0;
            }
        }

        static void RenderWays(OverpassResponseElement[] ways, Action<OverpassResponseElement> render)
        {
            foreach (var way in ways)
            {
                render(way);
            }
        }

        static void RenderWaySegments(OverpassResponseElement way, Dictionary<long, OverpassResponseElement> nodesById, Action<OverpassResponseElement, OverpassResponseElement> render)
        {
            for (var i = 1; i < way.nodes.Length; i++)
            {
                render(nodesById[way.nodes[i - 1]], nodesById[way.nodes[i]]);
            }
        }
    }
}
