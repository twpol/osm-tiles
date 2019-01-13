using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TileService.Models.Common;

namespace TileService.Models.Geometry
{
    public class CompletenessTile : GenericTile<CompletenessTile>
    {
        public static readonly TileCache<CompletenessTile> Cache = new TileCache<CompletenessTile>(10, 10, 10, 160, (zoom, x, y) => new CompletenessTile(zoom, x, y), (zoom, x, y, copy) => new CompletenessTile(zoom, x, y, copy));

        public float Completeness { get; private set; } = -1;

        CompletenessTile(int zoom, int x, int y)
            : base(zoom, x, y)
        {
        }

        CompletenessTile(int zoom, int x, int y, CompletenessTile copy)
            : base(zoom, x, y)
        {
            Debug.Assert(copy.Completeness != -1, "Cannot copy data from Tile without any data");

            Completeness = copy.Completeness;
        }

        async public override Task<CompletenessTile> Load()
        {
            Debug.Assert(Completeness == -1, "Cannot load data for Tile more than once");

            var overpass = await Overpass.Query.GetCompleteness(this);

            Completeness = (float)overpass.elements.Where(element => element.tags["total"] != "0").Count() / overpass.elements.Count();

            return this;
        }
    }
}
