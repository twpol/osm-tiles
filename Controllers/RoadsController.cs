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

        LatLon GetLatLonFromTile(int zoom, int x, int y) {
            var n = Math.Pow(2, zoom);
            return new LatLon {
                Lat = 180 / Math.PI * Math.Atan(Math.Sinh(Math.PI - (2 * Math.PI * y / n))),
                Lon = (x / n * 360) - 180,
            };
        }

        void GetLatLonBoxFromTile(int zoom, int x, int y, out LatLon nw, out LatLon se)
        {
            nw = GetLatLonFromTile(zoom, x, y);
            se = GetLatLonFromTile(zoom, x + 1, y + 1);
        }

        string GetBoundingBoxFromLatLonBox(LatLon nw, LatLon se)
        {
            return $"{se.Lat},{nw.Lon},{nw.Lat},{se.Lon}";
        }

        PointF GetPointFromNode(LatLon nw, LatLon se, OverpassResponseElement node) {
            return new PointF(
                (float)(256 * (node.lon - nw.Lon) / (se.Lon - nw.Lon)),
                (float)(256 * (node.lat - nw.Lat) / (se.Lat - nw.Lat))
            );
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
            var bbox = GetBoundingBoxFromLatLonBox(nw, se);
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

            var image = new Image<Rgba32>(256, 256);
            image.Mutate(context => {
                context.DrawPolygon(
                    new Rgba32(255, 0, 0),
                    1,
                    new PointF(0, 0),
                    new PointF(256, 0),
                    new PointF(256, 256),
                    new PointF(0, 256),
                    new PointF(0, 0)
                );

                foreach (var way in ways) {
                    for (var i = 1; i < way.nodes.Length; i++) {
                        var node1 = nodesById[way.nodes[i - 1]];
                        var node2 = nodesById[way.nodes[i - 0]];
                        context.DrawLines(
                            new Rgba32(0, 0, 255),
                            1,
                            GetPointFromNode(nw, se, node1),
                            GetPointFromNode(nw, se, node2)
                        );
                    }
                }
            });

            var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;

            return File(stream, "image/png");
        }
    }
}
