namespace osm_road_overlay.Models.Geometry
{
    public class Point
    {
        public double Lat { get; private set; }
        public double Lon { get; private set; }

        public Point(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }
    }
}
