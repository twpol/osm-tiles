using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace osm_road_overlay.Models.Overpass
{
    public static class Query
    {
        const string OverpassAPIEndpoint = "http://overpass-api.de/api/interpreter";

        static readonly HttpClient Client = new HttpClient();

        public static async Task<Response> Get(string overpassQuery)
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
    }
}
