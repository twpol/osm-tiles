using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Point = TileService.Models.Geometry.Point;

namespace TileService.Models.Common
{
    public abstract class Tile
    {
        protected const double C = 40075016.686;

        public int Zoom { get; }
        public int X { get; }
        public int Y { get; }
        public Point NW { get; }
        public Point SE { get; }
        public float ImageScale { get; }

        protected Tile(int zoom, int x, int y)
        {
            Zoom = zoom;
            X = x;
            Y = y;
            NW = GetPointFromTile(zoom, x, y);
            SE = GetPointFromTile(zoom, x + 1, y + 1);
            ImageScale = (float)(1 / (C * Math.Cos(NW.Lat * Math.PI / 180) / Math.Pow(2, zoom + 8)));
        }

        public override string ToString()
        {
            return $"{GetType().Name}({Zoom}, {X}, {Y})";
        }

        public PointF GetPointFromPoint(Point point)
        {
            return new PointF(
                (float)(256 * (point.Lon - NW.Lon) / (SE.Lon - NW.Lon)),
                (float)(256 * (point.Lat - NW.Lat) / (SE.Lat - NW.Lat))
            );
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

    public abstract class GenericTile<T> : Tile
    {
        protected GenericTile(int zoom, int x, int y)
            : base(zoom, x, y)
        {
        }

        public abstract Task<T> Load();
    }
}
