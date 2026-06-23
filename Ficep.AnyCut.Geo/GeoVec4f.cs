
namespace Ficep.AnyCut.Geo
{
    public class GeoVec4f
    {
        public double[] values = new double[4];
        public static int[] dim = { 4};
        public static int d = 1;

        public GeoVec4f()
        {
        }

        public GeoVec4f(double[] v)
        {
            if (v.Length != dim[0])
                throw new ArgumentException("Vector lenght not admissible");
            values[0] = v[0];
            values[1] = v[1];
            values[2] = v[2];
            values[3] = v[3];
        }

        public GeoVec4f(double x, double y, double z, double w)
        {
            values[0] = x;
            values[1] = y;
            values[2] = z;
            values[3] = w;
        }

        public double this[int i]
        {
            get { return values[i]; }
            set { values[i] = value; }
        }
        public double Dot(in GeoVec4f v)
        {
            return values[0] * v[0] + values[1] * v[1] + values[2] * v[2] + values[3] * v[3];
        }

        public double Length()
        {
            return (double)Math.Sqrt(values[0] * values[0] + values[1] * values[1] + values[2] * values[2] + values[3] * values[3]);
        }

        public void Negate()
        {
            values[0] = -values[0];
            values[1] = -values[1];
            values[2] = -values[2];
            values[3] = -values[3];
        }

        public double Normalize()
        {
            double len = Length();

            if (len > 0)
            {
                values = values.Select(x => x / len).ToArray();
            }

            return len;
        }



        public static GeoVec4f operator *(GeoVec4f v, double d)
        {
            return new GeoVec4f(v[0] * d, v[1] * d, v[2] * d, v[3] * d);
        }

        public static GeoVec4f operator *(double d, GeoVec4f v)
        {
            return v * d;
        }

        public static GeoVec4f operator /(GeoVec4f v, double d)
        {
            return new GeoVec4f(v[0] / d, v[1] / d, v[2] / d, v[3] / d);
        }

        public static GeoVec4f operator +(GeoVec4f v1, GeoVec4f v2)
        {
            return new GeoVec4f(v1[0] + v2[0],
                            v1[1] + v2[1],
                            v1[2] + v2[2],
                            v1[3] + v2[3]);
        }

        public static GeoVec4f operator -(GeoVec4f v1, GeoVec4f v2)
        {
            return new GeoVec4f(v1[0] - v2[0],
                            v1[1] - v2[1],
                            v1[2] - v2[2],
                            v1[3] - v2[3]);
        }

        public static bool operator ==(GeoVec4f v1, GeoVec4f v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(GeoVec4f v1, GeoVec4f v2)
        {
            return !(v1 == v2);
        }


        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is GeoVec4f))
                return false;
            return Equals((GeoVec4f)obj);
        }

        public bool Equals(in GeoVec4f other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetHashCode() != other.GetHashCode())
                return false;
            return (values[0] == other[0] && values[1] == other[1] && values[2] == other[2] && values[3] == other[3]);
        }

        public override int GetHashCode()
        {
            unchecked // Use unchecked to ignore overflows during arithmetic operations
            {
                int hash = 7; // Start with a prime number to avoid collisions
                foreach (double value in values)
                {
                    hash = hash * 31 + value.GetHashCode(); // Multiply by a prime number and add the hash code of the value
                }
                return hash;
            }
        }
    }
}
