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
    }
}
