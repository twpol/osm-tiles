namespace TileService.Models.Geometry
{
    public class Point
    {
        public double Lat { get; }
        public double Lon { get; }
        public Angle Angle { get; internal set; }

        public Point(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public override string ToString()
        {
            return $"{GetType().Name}({Lat:F6}, {Lon:F6})";
        }

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.Lat + b.Lat, a.Lon + b.Lon);
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.Lat - b.Lat, a.Lon - b.Lon);
        }

        public static Point operator *(Point a, double b)
        {
            return new Point(a.Lat * b, a.Lon * b);
        }

        public static Point operator /(Point a, double b)
        {
            return new Point(a.Lat / b, a.Lon / b);
        }
    }
}
