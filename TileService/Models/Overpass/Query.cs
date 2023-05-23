using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TileService.Models.Common;

namespace TileService.Models.Overpass
{
    public static class Query
    {
        const string OverpassAPIEndpoint = "https://overpass-api.de/api/interpreter";

        static readonly HttpClient Client = new();

        public static async Task<Response> GetTile(Tile tile)
        {
            // Gather the bounding box with 20m extra around it for capturing edges.
            var bbox = GetBoundingBoxFromTile(tile, 20);
            return await RunQuery($@"
                [out:json][timeout:60];
                (
                    way[""highway""]({bbox});
                    way[""railway""]({bbox});
                );
                node(w)->.nodes;
                (
                    way(bn.nodes)[""highway""];
                    way(bn.nodes)[""railway""];
                );
                out body;
                node(w);
                out skel qt;
            ");
        }

        static async Task<Response> RunQuery(string overpassQuery)
        {
            try
            {
                using var overpassResponse = await Client.PostAsync(
                    OverpassAPIEndpoint,
                    new FormUrlEncodedContent(new Dictionary<string, string>() {
                        { "data", overpassQuery },
                    })
                );
                overpassResponse.EnsureSuccessStatusCode();
                using var overpassReader = new StreamReader(await overpassResponse.Content.ReadAsStreamAsync());
                using var overpassJson = new JsonTextReader(overpassReader);
                return new JsonSerializer().Deserialize<Response>(overpassJson);
            }
            catch (HttpRequestException error)
            {
                Console.WriteLine($"Warning: {error}");
                // By returning an empty response, we stop repeated requests that keep hitting errors like 429 (Too Many Requests)
                return new Response
                {
                    elements = Array.Empty<Element>(),
                };
            }
        }

        static string GetBoundingBoxFromTile(Tile tile, int oversizeM = 0)
        {
            var oversizeScale = oversizeM * tile.ImageScale / 256;
            var latExtra = oversizeScale * (tile.NW.Lat - tile.SE.Lat);
            var lonExtra = oversizeScale * (tile.SE.Lon - tile.NW.Lon);
            return $"{tile.SE.Lat - latExtra},{tile.NW.Lon - lonExtra},{tile.NW.Lat + latExtra},{tile.SE.Lon + lonExtra}";
        }
    }
}
