using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osm_road_overlay.Models.Geometry;

namespace osm_road_overlay.Models.Overpass
{
    public static class Query
    {
        const string OverpassAPIEndpoint = "http://overpass-api.de/api/interpreter";
        const int MaximumZoomLevel = 14;
        const int MaximumCachedGeometry = 16;

        static readonly HttpClient Client = new HttpClient();

        static readonly Dictionary<string, Task<Way[]>> GeometryCache = new Dictionary<string, Task<Way[]>>();
        static readonly List<string> GeometryCacheOrder = new List<string>();

        static async Task<Response> Get(Tile tile)
        {
            // Gather the bounding box with 20m extra around it for capturing edges.
            var bbox = GetBoundingBoxFromTile(tile);
            var overpassQuery = $"[out:json][timeout:60];(way[\"highway\"]({bbox}););out body;>;out skel qt;";

            using (var overpassResponse = await Client.PostAsync(
                OverpassAPIEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "data", overpassQuery },
                })
            )) {
                overpassResponse.EnsureSuccessStatusCode();
                using (var overpassReader = new StreamReader(await overpassResponse.Content.ReadAsStreamAsync()))
                using (var overpassJson = new JsonTextReader(overpassReader)) {
                    return new JsonSerializer().Deserialize<Response>(overpassJson);
                }
            }
        }

        static string GetBoundingBoxFromTile(Tile tile)
        {
            var oversizeScale = 20 * tile.ImageScale / 256;
            var latExtra = oversizeScale * (tile.NW.Lat - tile.SE.Lat);
            var lonExtra = oversizeScale * (tile.SE.Lon - tile.NW.Lon);
            return $"{tile.SE.Lat - latExtra},{tile.NW.Lon - lonExtra},{tile.NW.Lat + latExtra},{tile.SE.Lon + lonExtra}";
        }

        public static Task<Way[]> GetGeometry(Tile tile)
        {
            if (tile.Zoom > MaximumZoomLevel) {
                var zoomDiff = tile.Zoom - MaximumZoomLevel;
                tile = new Tile(MaximumZoomLevel, (int)(tile.X / Math.Pow(2, zoomDiff)), (int)(tile.Y / Math.Pow(2, zoomDiff)));
            }

            var tileKey = tile.ToString();
            Task<Way[]> ways;

            lock (GeometryCache) {
                if (GeometryCache.TryGetValue(tileKey, out var cachedWays)) {
                    GeometryCacheOrder.Remove(tileKey);
                    GeometryCacheOrder.Add(tileKey);
                    return cachedWays;
                }

                ways = GetWays(tile);

                Console.WriteLine($"Caching geometry for {tileKey} ({GeometryCacheOrder.Count + 1} / {MaximumCachedGeometry})");
                GeometryCache.Add(tileKey, ways);
                GeometryCacheOrder.Add(tileKey);

                while (GeometryCacheOrder.Count > MaximumCachedGeometry) {
                    GeometryCache.Remove(GeometryCacheOrder[0]);
                    GeometryCacheOrder.RemoveAt(0);
                }
            }

            return ways;
        }

        static async Task<Way[]> GetWays(Tile tile)
        {
            var overpass = await Get(tile);
            var overpassWays = overpass.elements.Where(element => element.type == "way").ToArray();
            var overpassNodes = overpass.elements.Where(element => element.type == "node").ToArray();
            var overpassNodesById = new Dictionary<long, Element>(
                overpassNodes.Select(node => {
                    return new KeyValuePair<long, Element>(
                        node.id,
                        node
                    );
                })
            );

            var ways = overpassWays.Select(way => {
                var geoPoints = way.nodes.Select(nodeId => {
                    var node = overpassNodesById[nodeId];
                    return new Point(node.lat, node.lon);
                }).ToArray();
                return new Way(
                    way.tags,
                    Enumerable.Range(0, geoPoints.Length - 1).Select(index => {
                        return new Line(geoPoints[index], geoPoints[index + 1]);
                    })
                );
            }).ToArray();

            return ways;
        }
    }
}
