
namespace Ficep.AnyCut.Geo
{
    public class GeoPlane
    {
        public GeoVec3f Normal;
        public double distance;

        public GeoPlane()
        {
        }

        public GeoPlane(GeoVec3f iNormal, double iDistance)
        {
            Normal = new GeoVec3f(iNormal);
            distance = iDistance;
            Normal.Normalize();
        }

        public GeoPlane(GeoVec3f p0, GeoVec3f p1, GeoVec3f p2)
        {
            Normal = (p1 - p0).Cross(p2 - p0);
            Normal.Normalize();
            distance = Normal.Dot(p0);
        }

        public GeoPlane(GeoVec3f iNormal, GeoVec3f iPoint)
        {
            Normal = new GeoVec3f(iNormal);
            Normal.Normalize();
            distance = Normal.Dot(iPoint);
        }

        public double Distance(in GeoVec3f iPoint)
        {
            return iPoint.Dot(Normal) - distance;
        }

        public bool Intersect(in GeoLine iLine, ref GeoVec3f iIntersect)
        {
            if (iLine.Direction.Dot(Normal) > 0)
            {
                double t = (distance - Normal.Dot(iLine.Position)) / Normal.Dot(iLine.Direction);
                iIntersect = iLine.Position + t * iLine.Direction;
                return true;
            }

            return false;
        }

        public void Transform(in GeoMatrix iMatrix)
        {
            GeoVec3f lPoint = Normal * distance;

            GeoMatrix lMatrix = iMatrix.Inverse().Transpose();
            lMatrix.MultDirMatrix(Normal, ref Normal);
            iMatrix.MultVecMatrix(lPoint, ref lPoint);
            Normal.Normalize();
            distance = Normal.Dot(lPoint);
        }
    }
}
