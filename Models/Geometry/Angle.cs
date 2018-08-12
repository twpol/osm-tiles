using System;

namespace osm_road_overlay.Models.Geometry
{
    public struct Angle
    {
        public static readonly Angle Zero = new Angle(0);
        public static readonly Angle QuarterTurn = new Angle(Math.PI / 2);
        public static readonly Angle HalfTurn = new Angle(Math.PI);
        public static readonly Angle FullTurn = new Angle(Math.PI * 2);

        public readonly double Radians;

        public Angle(double radians)
        {
            Radians = radians;
            while (Radians < Math.PI) Radians += 2 * Math.PI;
            while (Radians > Math.PI) Radians -= 2 * Math.PI;
        }

        public override string ToString()
        {
            return $"{Radians * 180 / Math.PI:F3}°";
        }

        public static Angle Add(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians + angle2.Radians);
        }

        public static Angle Subtract(Angle angle1, Angle angle2)
        {
            return new Angle(angle1.Radians - angle2.Radians);
        }

        public static Angle Average(Angle angle1, Angle angle2)
        {
            return new Angle(Math.Atan2(
                Math.Sin(angle1.Radians) + Math.Sin(angle2.Radians),
                Math.Cos(angle1.Radians) + Math.Cos(angle2.Radians)
            ));
        }

        public static Angle Difference(Angle angle1, Angle angle2)
        {
            var angle = Math.Abs(angle1.Radians - angle2.Radians);
            return new Angle(angle > Math.PI ? 2 * Math.PI - angle : angle);
        }
    }
}
