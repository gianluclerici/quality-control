
namespace Ficep.AnyCut.Mathematics
{
    public class Point : ICloneable
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Point()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public object Clone()
        {
            return new Point(X, Y, Z);
        }
    }
}
