
namespace Ficep.AnyCut.Geo
{
    public class GeoVec3f
    {
        public double[] values = new double[3];
        readonly public static int[] dim = {3};
        readonly public static int d = 1;
        public GeoVec3f()
        {
        }
        public GeoVec3f(double[] v)
        {
            if (v.Length != dim[0])
                throw new ArgumentException("Array lenght not admissible");
            values[0] = v[0];
            values[1] = v[1];
            values[2] = v[2];
        }
        public GeoVec3f(GeoVec3f vector)
        {
            values[0] = vector[0];
            values[1] = vector[1];
            values[2] = vector[2];
        }
        public GeoVec3f(double x, double y, double z)
        {
            values[0] = x;
            values[1] = y;
            values[2] = z;
        }

        public double this[int i]
        {
            get { return values[i]; }
            set { values[i] = value; }
        }
        public static GeoVec3f operator *(GeoVec3f v, double iValue)
        {
            return new GeoVec3f(v[0] * iValue, v[1] * iValue, v[2] * iValue); 
        }
        public static GeoVec3f operator *(double iValue, GeoVec3f v)
        {
            return new GeoVec3f(v[0] * iValue, v[1] * iValue, v[2] * iValue); 
        }
        public static GeoVec3f operator *(GeoVec3f v1, GeoVec3f v2)
        {
            return new GeoVec3f(v1[0] * v2[0],
                            v1[1] * v2[1],
                            v1[2] * v2[2]);
        }
        public static GeoVec3f operator /(GeoVec3f v, double d)
        {
            return new GeoVec3f(v[0] / d,
                                v[1] / d,
                                v[2] / d);
        }
        public static GeoVec3f operator /(GeoVec3f v1, GeoVec3f v2)
        {
            return new GeoVec3f(v1[0] / v2[0],
                            v1[1] / v2[1],
                            v1[2] / v2[2]);
        }
        public static GeoVec3f operator +(GeoVec3f v1, GeoVec3f v2)
        {
            return new GeoVec3f(v1[0] + v2[0],
                                v1[1] + v2[1],
                                v1[2] + v2[2]);
        }
        public static GeoVec3f operator -(GeoVec3f v1, GeoVec3f v2)
        {
            return new GeoVec3f(v1[0] - v2[0],
                                v1[1] - v2[1],
                                v1[2] - v2[2]);
        }
        public static bool operator ==(GeoVec3f v1, GeoVec3f v2)
        {
            return v1.Equals(v2);
        }
        public static bool operator !=(GeoVec3f v1, GeoVec3f v2)
        {
            return !(v1 == v2);
        }
        public void SetValue(in GeoMatrix iMatrix)
        {
            values[0] = iMatrix[3,0];
            values[1] = iMatrix[3, 1];
            values[2] = iMatrix[3, 2];
        }

        public GeoVec3f Cross(in GeoVec3f v)
        {
            return new GeoVec3f(values[1] * v[2] - values[2] * v[1],
                                values[2] * v[0] - values[0] * v[2],
                                values[0] * v[1] - values[1] * v[0]);
        }
        public double Dot(in GeoVec3f v)
        {
            return values[0] * v[0] + values[1] * v[1]  + values[2] * v[2];
        }
        public double Length()
        {
            return (double)Math.Sqrt(values[0] * values[0] +
                       values[1] * values[1] +
                       values[2] * values[2]);
        }
        public double Normalize()
        {
            double len = Length();

            if (len > 0)
            {
                 values = values.Select(x => x/len).ToArray();
            }

            return len;
        }
        public override int GetHashCode()
        {
            unchecked // Use unchecked to ignore overflows during arithmetic operations
            {
                int hash = 17; // Start with a prime number to avoid collisions
                foreach (double value in values)
                {
                    hash = hash * 31 + value.GetHashCode(); // Multiply by a prime number and add the hash code of the value
                }
                return hash;
            }
        }
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is GeoVec3f))
                return false;
            return Equals((GeoVec3f)obj);
        }

        public bool Equals(in GeoVec3f other)
        {
            if (other is null)
                return false;
            if(ReferenceEquals(this, other))
                return true;
            if(this.GetHashCode() != other.GetHashCode())
                return false;
            return (values[0] == other[0] && values[1] == other[1] && values[2] == other[2]);

        }
    }
}

