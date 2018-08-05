using System.Collections.Generic;

namespace osm_road_overlay.Models.Overpass
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
