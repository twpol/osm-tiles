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

        static readonly HttpClient Client = new HttpClient();

        static async Task<Response> Get(string overpassQuery)
        {
            using (var overpassResponse = await Client.PostAsync(
                OverpassAPIEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>() {
                    { "data", overpassQuery },
                })
            ))
            using (var overpassReader = new StreamReader(await overpassResponse.Content.ReadAsStreamAsync()))
            using (var overpassJson = new JsonTextReader(overpassReader)) {
                return new JsonSerializer().Deserialize<Response>(overpassJson);
            }
        }

        public static async Task<World> GetGeometry(string overpassQuery)
        {
            var overpass = await Get(overpassQuery);
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

            return new World(ways);
        }
    }
}
