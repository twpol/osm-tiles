using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace osm_road_overlay.Models.Geometry
{
    public class Tile
    {
        const double C = 40075016.686;

        public int Zoom { get; }
        public int X { get; }
        public int Y { get; }
        public Point NW { get; }
        public Point SE { get; }
        public double ImageScale { get; }
        public ImmutableList<Way> Ways { get; private set; }

        public Tile(int zoom, int x, int y)
        {
            Zoom = zoom;
            X = x;
            Y = y;
            NW = GetPointFromTile(zoom, x, y);
            SE = GetPointFromTile(zoom, x + 1, y + 1);
            ImageScale = 1 / (C * Math.Cos(NW.Lat) / Math.Pow(2, zoom + 8));
        }

        public async Task LoadGeometry()
        {
            Ways = ImmutableList.ToImmutableList(await Models.Overpass.Query.GetGeometry(this));
        }

        override public string ToString()
        {
            return $"Tile({Zoom}, {X}, {Y})";
        }

        static Point GetPointFromTile(int zoom, int x, int y)
        {
            var n = Math.Pow(2, zoom);
            return new Point(
                180 / Math.PI * Math.Atan(Math.Sinh(Math.PI - (2 * Math.PI * y / n))),
                (x / n * 360) - 180
            );
        }
    }
}
