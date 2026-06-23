
namespace Ficep.AnyCut.Geo
{
    public class GeoBox3f
    {
        public GeoVec3f Min;
        public GeoVec3f Max;

        public GeoBox3f()
        {
            Min = new GeoVec3f(double.MaxValue, double.MaxValue, double.MaxValue);
            Max = new GeoVec3f(-double.MaxValue, -double.MaxValue, -double.MaxValue);
        }

        public GeoBox3f(GeoVec3f iMin, GeoVec3f iMax)
        {
            Min = new GeoVec3f(iMin);
            Max = new GeoVec3f(iMax);
        }

        public GeoVec3f Center()
        {
            return new GeoVec3f((Max[0] + Min[0]) * 0.5f,
                                (Max[1] + Min[1]) * 0.5f,
                                (Max[2] + Min[2]) * 0.5f);
        }

        public void Extend(in GeoVec3f iPoint)
        {
            Min[0] = Math.Min(iPoint[0], Min[0]);
            Min[1] = Math.Min(iPoint[1], Min[1]);
            Min[2] = Math.Min(iPoint[2], Min[2]);

            Max[0] = Math.Max(iPoint[0], Max[0]);
            Max[1] = Math.Max(iPoint[1], Max[1]);
            Max[2] = Math.Max(iPoint[2], Max[2]);
        }

        public void Extend(in GeoBox3f iBox)
        {
            Min[0] = Math.Min(iBox.Min[0], Min[0]);
            Min[1] = Math.Min(iBox.Min[1], Min[1]);
            Min[2] = Math.Min(iBox.Min[2], Min[2]);

            Max[0] = Math.Max(iBox.Max[0], Max[0]);
            Max[1] = Math.Max(iBox.Max[1], Max[1]);
            Max[2] = Math.Max(iBox.Max[2], Max[2]);
        }

        public bool Intersect(in GeoVec3f iPoint)
        {
            return !(iPoint[0] < Min[0] ||
                     iPoint[0] > Max[0] ||
                     iPoint[1] < Min[1] ||
                     iPoint[1] > Max[1] ||
                     iPoint[2] < Min[2] ||
                     iPoint[2] > Max[2]);
        }

        public bool Intersect(in GeoBox3f iBox)
        {
            return !((iBox.Max[0] < Min[0]) ||
                     (iBox.Max[1] < Min[1]) ||
                     (iBox.Max[2] < Min[2]) ||
                     (iBox.Min[0] > Max[0]) ||
                     (iBox.Min[1] > Max[1]) ||
                     (iBox.Min[2] > Max[2]));
        }

        public void Clear()
        {
            Min = new GeoVec3f(double.MaxValue, double.MaxValue, double.MaxValue);
            Max = new GeoVec3f(-double.MaxValue, -double.MaxValue, -double.MaxValue);
        }

        public bool Empty()
        {
            return Max[0] < Min[0];
        }

        public void Transform(in GeoMatrix iMatrix)
        {
            if (Min[0] != double.MaxValue)
            {
                GeoVec3f lMin = Min;
                GeoVec3f lMax = Max;

                iMatrix.MultVecMatrix(lMin, ref lMin);
                iMatrix.MultVecMatrix(lMax, ref lMax);

                Min[0] = Math.Min(lMin[0], lMax[0]);
                Min[1] = Math.Min(lMin[1], lMax[1]);
                Min[2] = Math.Min(lMin[1], lMax[1]);

                Max[0] = Math.Min(lMin[0], lMax[0]);
                Max[1] = Math.Min(lMin[1], lMax[1]);
                Max[2] = Math.Min(lMin[1], lMax[1]);
            }
        }
    }
}