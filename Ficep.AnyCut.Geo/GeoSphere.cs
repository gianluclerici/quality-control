
namespace Ficep.AnyCut.Geo
{
    public class GeoSphere
    {
        public GeoVec3f Center;
        public double Radius;

        public GeoSphere(GeoVec3f iCenter, double iRadius)
        {
            Center = new GeoVec3f(iCenter);
            Radius = iRadius;
        }

        public bool Intersect(in GeoLine iLine, out GeoVec3f iEnter, out GeoVec3f iExit)
        {
            double b = 2.0f * (iLine.Position.Dot(iLine.Direction) - Center.Dot(iLine.Direction));
            double c = (iLine.Position[0] * iLine.Position[0] + iLine.Position[1] * iLine.Position[1] + iLine.Position[2] * iLine.Position[2]) +
                      (Center[0] * Center[0] + Center[1] * Center[1] + Center[2] * Center[2]) -
                      2.0f * iLine.Position.Dot(Center) - Radius * Radius;

            double lValue = b * b - 4.0f * c;
            if (lValue >= 0)
            {
                double t1 = (-b + (double)Math.Sqrt(lValue)) / 2.0f;
                double t2 = (-b - (double)Math.Sqrt(lValue)) / 2.0f;

                if (t1 > t2)
                {
                    double lTemp = t1;
                    t1 = t2;
                    t2 = lTemp;
                }

                iEnter = iLine.Position + t1 * iLine.Direction;
                iExit = iLine.Position + t2 * iLine.Direction;
                return true;
            }
            else
            {
                iEnter = new GeoVec3f();
                iExit = new GeoVec3f();
                return false;
            }
        }
    }
}
