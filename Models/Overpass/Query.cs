using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace osm_road_overlay.Models.Overpass
{
    public static class Query
    {
        const string OverpassAPIEndpoint = "https://overpass-api.de/api/interpreter";

        static readonly HttpClient Client = new HttpClient();

        public static async Task<Response> Get(Geometry.Tile tile)
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

        static string GetBoundingBoxFromTile(Geometry.Tile tile)
        {
            var oversizeScale = 20 * tile.ImageScale / 256;
            var latExtra = oversizeScale * (tile.NW.Lat - tile.SE.Lat);
            var lonExtra = oversizeScale * (tile.SE.Lon - tile.NW.Lon);
            return $"{tile.SE.Lat - latExtra},{tile.NW.Lon - lonExtra},{tile.NW.Lat + latExtra},{tile.SE.Lon + lonExtra}";
        }
    }
}
