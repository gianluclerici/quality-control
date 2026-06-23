
namespace Ficep.AnyCut.Geo
{
    public class GeoRotation
    {
        readonly public static int[] dim = { 4 };
        readonly public static int d = 1;
        public GeoVec4f values = new GeoVec4f();
        public GeoRotation()
        {
            values = new GeoVec4f(0, 0, 0, 1);
        }

        public GeoRotation(GeoVec3f axis, double iRadians)
        {
            SetValue(axis, iRadians);
        }

        public GeoRotation(double[] q)
        {
            if (q.Length != GeoVec4f.dim[0])
                throw new ArgumentException("Invalid vector lenght");
            SetValue(q);
        }

        public GeoRotation(double q0, double q1, double q2, double q3)
        {
            SetValue(q0, q1, q2, q3);
        }

        public GeoRotation(GeoMatrix m)
        {
            SetValue(m);
        }

        public GeoRotation(GeoVec3f rotateFrom, GeoVec3f rotateTo)
        {
            SetValue(rotateFrom, rotateTo);
        }

        public static bool operator==(GeoRotation left, GeoRotation right) =>  left.values == right.values;

        public static bool operator !=(GeoRotation left, GeoRotation right) => left.values != right.values;

        public static GeoRotation operator*(GeoRotation left, GeoRotation right)
        {
            GeoRotation q= new GeoRotation();
            q.SetValue(right.values[3] * left.values[0] + right.values[0] * left.values[3] + right.values[1] * left.values[2] - right.values[2] * left.values[1],
             right.values[3] * left.values[1] - right.values[0] * left.values[2] + right.values[1] * left.values[3] + right.values[2] * left.values[0],
             right.values[3] * left.values[2] + right.values[0] * left.values[1] - right.values[1] * left.values[0] + right.values[2] * left.values[3],
             right.values[3] * left.values[3] - right.values[0] * left.values[0] - right.values[1] * left.values[1] - right.values[2] * left.values[2]);
            return q;
        }
        public double[] GetValue()
        {
            return values.values;
        }

        public void SetValue(double q0, double q1, double q2, double q3)
        {
            values.values = new double[] { q0, q1, q2, q3 };
            values.Normalize();
        }

        public void GetValue(out GeoVec3f axis, out double iRadians)
        {
            iRadians = (double)Math.Acos(values[3]) * 2.0f;
            double scale = (double)Math.Sin(iRadians / 2.0f);

            if (scale != 0)
            {
                axis = new GeoVec3f(
                    values[0] / scale,
                    values[1] / scale,
                    values[2] / scale
                );
            }
            else
            {
                axis = new GeoVec3f(0, 0, 1);
            }
        }

        public void Invert()
        {
            double lLength = values.Length();
            values[0] = -values[0] / lLength;
            values[1] = -values[1] / lLength;
            values[2] = -values[2] / lLength;
            values[3] = values[3] / lLength;
        }

        public GeoRotation Inverse()
        {
            double lLength = values.Length();

            GeoRotation lRotation = new GeoRotation();
            lRotation.values[0] = -values[0] / lLength;
            lRotation.values[1] = -values[1] / lLength;
            lRotation.values[2] = -values[2] / lLength;
            lRotation.values[3] = values[3] / lLength;
            return lRotation;
        }

        public void SetValue(double[] q)
        {
            if (q.Length != GeoVec4f.dim[0])
                throw new ArgumentException("Invalid vector lenght");
            Array.Copy(q, values.values, q.Length);
            values.Normalize();
        }

        public void SetValue(in GeoMatrix m)
        {
            double scalerow = m[0, 0] + m[1, 1] + m[2, 2];

            if (scalerow > 0)
            {
                double s = (double)Math.Sqrt(scalerow + m[3, 3]);
                values[3] = s * 0.5f;
                s = 0.5f / s;

                values[0] = (m[1, 2] - m[2, 1]) * s;
                values[1] = (m[2, 0] - m[0, 2]) * s;
                values[2] = (m[0, 1] - m[1, 0]) * s;
            }
            else
            {
                int i = 0;
                if (m[1, 1] > m[0, 0]) i = 1;
                if (m[2, 2] > m[i, i]) i = 2;

                int j = (i + 1) % 3;
                int k = (j + 1) % 3;

                double s = (double)Math.Sqrt((m[i, i] - (m[j, j] + m[k, k])) + m[3, 3]);

                values[i] = s * 0.5f;
                s = 0.5f / s;

                values[3] = (m[j, k] - m[k, j]) * s;
                values[j] = (m[i, j] + m[j, i]) * s;
                values[k] = (m[i, k] + m[k, i]) * s;
            }
            if (m[3, 3] != 1)
            values *= (1 / (double)Math.Sqrt(m[3, 3])); 

        }

        public void SetValue(in GeoVec3f axis, double iRadians)
        {
            double sineval = (double)Math.Sin(iRadians / 2);

            GeoVec3f a = new GeoVec3f(axis[0], axis[1], axis[2]);
            a.Normalize();
            values[0] = a[0] * sineval;
            values[1] = a[1] * sineval;
            values[2] = a[2] * sineval;
            values[3] = (double)Math.Cos(iRadians / 2);
        }

        public void SetValue(in GeoVec3f rotateFrom, in GeoVec3f rotateTo)
        {
            GeoVec3f from = new GeoVec3f(rotateFrom[0], rotateFrom[1], rotateFrom[2]);
            from.Normalize();
            GeoVec3f to = new GeoVec3f(rotateTo[0], rotateTo[1], rotateTo[2]);
            to.Normalize();

            double dot = from.Dot(to);
            GeoVec3f crossvec = from.Cross(to);
            double crosslen = crossvec.Length();

            if (crosslen == 0)
            {
                if (dot > 0)
                {
                    SetValue(0, 0, 0, 1);
                }
                else
                {
                    GeoVec3f t = from.Cross(new GeoVec3f(1, 0, 0));
                    if (t.Length() == 0)
                    {
                        t = from.Cross(new GeoVec3f(0, 1, 0));
                    }
                    t.Normalize();
                    SetValue(t[0], t[1], t[2], 0);
                }
            }
            else
            {
                crossvec.Normalize();
                crossvec *= (double)Math.Sqrt(0.5f * Math.Abs(1 - dot));
                SetValue(crossvec[0], crossvec[1], crossvec[2], (double)Math.Sqrt(0.5 * Math.Abs(1.0 + dot)));
            }
        }
        public void GetEuler(out double iX, out double iY, out double iZ)
        {
            double test = values[0] * values[1] + values[2] * values[3];

            if (test > 0.499)
            {
                iY = (double)(2 * Math.Atan2(values[0], values[3]));
                iZ = (double)(Math.PI / 2);
                iX = 0;
                return;
            }

            if (test < -0.499)
            {
                iY = (double)(-2 * Math.Atan2(values[0], values[3]));
                iZ = (double)(-Math.PI / 2);
                iX = 0;
                return;
            }

            double sqx = values[0] * values[0];
            double sqy = values[1] * values[1];
            double sqz = values[2] * values[2];

            iY = (double)Math.Atan2((double)(2 * values[1] * values[3] - 2 * values[0] * values[2]), 1 - 2 * sqy - 2 * sqz);
            iZ = (double)Math.Asin(2 * test);
            iX = (double)Math.Atan2((double)(2 * values[0] * values[3] - 2 * values[1] * values[2]), 1 - 2 * sqx - 2 * sqz);
        }

        public void SetEuler(double iX, double iY, double iZ)
        {
            double c1 = Math.Cos(iY / 2);
            double s1 = Math.Sin(iY / 2);
            double c2 = Math.Cos(iZ / 2);
            double s2 = Math.Sin(iZ / 2);
            double c3 = Math.Cos(iX / 2);
            double s3 = Math.Sin(iX / 2);
            double c1c2 = c1 * c2;
            double s1s2 = s1 * s2;

            values[3] = (double)(c1c2 * c3 - s1s2 * s3);
            values[0] = (double)(c1c2 * s3 + s1s2 * c3);
            values[1] = (double)(s1 * c2 * c3 + c1 * s2 * s3);
            values[2] = (double)(c1 * s2 * c3 - s1 * c2 * s3);
        }

        public void MultVec(in GeoVec3f src, ref GeoVec3f dst)
        {
            GeoMatrix lMatrix = new GeoMatrix();
            lMatrix.SetTransform(this);
            lMatrix.MultVecMatrix(src, ref dst);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is GeoRotation))
                return false;
            return Equals((GeoRotation)obj);
        }

        public bool Equals(in GeoRotation other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (this.GetHashCode() != other.GetHashCode())
                return false;
            return (values[0] == other.values[0] && values[1] == other.values[1] && values[2] == other.values[2] && values[3] == other.values[3]);

        }

        public override int GetHashCode()
        {
            unchecked // Use unchecked to ignore overflows during arithmetic operations
            {
                int hash = 5; // Start with a prime number to avoid collisions
                foreach (double value in values.values)
                {
                    hash = hash * 11 + value.GetHashCode(); // Multiply by a prime number and add the hash code of the value
                }
                return hash;
            }
        }
    }
}
