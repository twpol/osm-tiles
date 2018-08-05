using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace osm_road_overlay.Models.Geometry
{
    public class Tile
    {
        const double C = 40075016.686;

        public Point NW { get; }
        public Point SE { get; }
        public double ImageScale { get; }
        public ImmutableList<Way> Ways { get; private set; }

        public Tile(int zoom, int x, int y)
        {
            NW = GetPointFromTile(zoom, x, y);
            SE = GetPointFromTile(zoom, x + 1, y + 1);
            ImageScale = 1 / (C * Math.Cos(NW.Lat) / Math.Pow(2, zoom + 8));
        }

        public async Task LoadGeometry()
        {
            var bbox = GetBoundingBoxFromTile(this, 0.5);
            var overpassQuery = $"[out:json][timeout:60];(way[\"highway\"]({bbox}););out body;>;out skel qt;";
            Ways = ImmutableList.ToImmutableList(await Models.Overpass.Query.GetGeometry(overpassQuery));
        }

        static Point GetPointFromTile(int zoom, int x, int y)
        {
            var n = Math.Pow(2, zoom);
            return new Point(
                180 / Math.PI * Math.Atan(Math.Sinh(Math.PI - (2 * Math.PI * y / n))),
                (x / n * 360) - 180
            );
        }

        static string GetBoundingBoxFromTile(Tile tile, double oversize)
        {
            var latExtra = oversize * (tile.NW.Lat - tile.SE.Lat);
            var lonExtra = oversize * (tile.SE.Lon - tile.NW.Lon);
            return $"{tile.SE.Lat - latExtra},{tile.NW.Lon - lonExtra},{tile.NW.Lat + latExtra},{tile.SE.Lon + lonExtra}";
        }
    }
}
