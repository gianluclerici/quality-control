
namespace Ficep.AnyCut.Geo
{
    public class GeoLine
    {
        public GeoVec3f Position;
        public GeoVec3f Direction;

        public GeoLine()
        {
            Position = new GeoVec3f(0, 0, 0);
            Direction = new GeoVec3f(0, 0, 1);
        }

        public GeoLine(GeoVec3f origin, GeoVec3f point)
        {
            Position = new GeoVec3f(origin[0], origin[1], origin[2]);
            Direction = point - origin;
            Direction.Normalize();
        }

        public GeoLine(GeoLine line)
        {
            Position = new GeoVec3f(line.Position);
            Direction = new GeoVec3f(line.Direction);
        }

        public void SetValue(GeoVec3f origin, GeoVec3f point)
        {
            Position = new GeoVec3f(origin[0], origin[1], origin[2]);
            Direction = point - origin;
            Direction.Normalize();
        }

        public GeoVec3f GetClosestPoint(GeoVec3f iPoint)
        {
            double lMagnitude = (iPoint - Position).Dot(Direction);
            return (Position + Direction * lMagnitude);
        }

        public void Transform(GeoMatrix iMatrix)
        {
            iMatrix.MultVecMatrix(Position, ref Position);
            iMatrix.MultDirMatrix(Direction, ref Direction);
        }
    }
}
