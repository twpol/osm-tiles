using System.Collections.Generic;

namespace TileService.Models.Overpass
{
    public struct Element
    {
        public string type;
        public long id;
        public double lat;
        public double lon;
        public long[] nodes;
        public Dictionary<string, string> tags;
    }
}
