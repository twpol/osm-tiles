namespace osm_road_overlay.Models.Geometry
{
    public class Point
    {
        public double Lat { get; }
        public double Lon { get; }
        public double AngleRad { get; internal set; }

        public Point(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }
    }
}
