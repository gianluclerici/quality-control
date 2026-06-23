
namespace Ficep.AnyCut.Geo
{
    public class GeoCylinder
    {
        public GeoLine Axis;
        public double Radius;

        public GeoCylinder()
        {
            Axis = new GeoLine(new GeoVec3f(0, 0, 0), new GeoVec3f(0, 1, 0));
            Radius = 1;
        }

        public GeoCylinder(GeoLine imAxis, double imRadius)
        {
            Axis = new GeoLine(imAxis);
            Radius = imRadius;
        }

        public bool Intersect(in GeoLine iLine, ref GeoVec3f iEnter, ref GeoVec3f iExit)
        {
            GeoVec3f lNormal = iLine.Direction.Cross(Axis.Direction);
            double length = lNormal.Length();
            lNormal.Normalize();

            if (length > 0.0)
            {
                GeoVec3f lDirection = iLine.Position - Axis.Position;
                double lDistance = Math.Abs(lDirection.Dot(lNormal));
                if (lDistance < Radius)
                {
                    GeoVec3f lTemp = lDirection.Cross(Axis.Direction);
                    double t = -lTemp.Dot(lNormal) / length;
                    lTemp = lNormal.Cross(Axis.Direction);
                    lTemp.Normalize();

                    double s = Math.Abs(Math.Sqrt(Radius * Radius - lDistance * lDistance) / iLine.Direction.Dot(lTemp));

                    double lP1 = t - s;
                    double lP2 = t + s;
                    if (lP1 > lP2)
                    {
                        iEnter = iLine.Position + lP2 * iLine.Direction;
                        iExit = iLine.Position + lP1 * iLine.Direction;
                    }
                    else
                    {
                        iEnter = iLine.Position + lP1 * iLine.Direction;
                        iExit = iLine.Position + lP2 * iLine.Direction;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
