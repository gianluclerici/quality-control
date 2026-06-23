
namespace Ficep.AnyCut.Geo
{
    public class GeoVec2f
    {
        public double[] values = new double[2]; 
        public GeoVec2f()
        {
        }

        public GeoVec2f(double[] v)
        {
            if (v.Length != 2)
                throw new ArgumentException("Vector lenght not admissible");
            values[0] = v[0];
            values[1] = v[1];
        }

        public GeoVec2f(double x, double y)
        {
            values[0] = x;
            values[1] = y;
        }

        public double this[int i]
        {   
            get { return values[i]; }
            set { values[i] = value; }
        }
        public double Dot(in GeoVec2f v)
        {
            return values[0] * v[0] + values[1] * v[1];
        }

        public double Length()
        {
            return (double)Math.Sqrt(values[0] * values[0] + values[1] * values[1]);
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


        public static GeoVec2f operator +(GeoVec2f v1, GeoVec2f v2)
        {
            return new GeoVec2f(v1[0] + v2[0], v1[1] + v2[1]);
        }

        public static GeoVec2f operator -(GeoVec2f v1, GeoVec2f v2)
        {
            return new GeoVec2f(v1[0] - v2[0], v1[1] - v2[1]);
        }
        public static GeoVec2f operator *(GeoVec2f v, double iValue)
        {
            return new GeoVec2f(v[0] * iValue, v[1] * iValue);
        }
        public static GeoVec2f operator *(double iValue, GeoVec2f v)
        {
            return new GeoVec2f(v[0] * iValue, v[1] * iValue);
        }
        public static GeoVec2f operator /(GeoVec2f v, double iValue)
        {
            return new GeoVec2f(v[0] / iValue, v[1] / iValue);
        }
        public static GeoVec2f operator /(double iValue, GeoVec2f v)
        {
            return new GeoVec2f(v[0] / iValue, v[1] / iValue);
        }
        public static bool operator ==(GeoVec2f v1, GeoVec2f v2)
        {
            return (v1[0] == v2[0]) && (v1[1] == v2[1]);
        }

        public static bool operator !=(GeoVec2f v1, GeoVec2f v2)
        {
            return !(v1 == v2);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is GeoVec2f))
                return false;
            return Equals((GeoVec2f)obj);
        }

        public bool Equals(in GeoVec2f other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetHashCode() != other.GetHashCode())
                return false;
            return (values[0] == other[0] && values[1] == other[1]);

        }

        public override int GetHashCode()
        {
            unchecked // Use unchecked to ignore overflows during arithmetic operations
            {
                int hash = 7; // Start with a prime number to avoid collisions
                foreach (double value in values)
                {
                    hash = hash * 17 + value.GetHashCode(); // Multiply by a prime number and add the hash code of the value
                }
                return hash;
            }
        }
    }
}
