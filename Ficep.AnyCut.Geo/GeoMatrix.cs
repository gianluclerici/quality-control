
namespace Ficep.AnyCut.Geo
{
    public class GeoMatrix
    {
        public static GeoMatrix Identity = new GeoMatrix(new double[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } });
        public static int[] Dim = {4, 4};
        public static int D = 2;

        public double[,] Values { get; set; } = new double[4,4];

        private bool dirty;
        private bool isIdentity;
        public GeoMatrix() 
        {
            dirty = true;
            isIdentity = false;
        }

        public GeoMatrix(GeoMatrix matrix) : this()
        {
            if (matrix is null)
                throw new ArgumentNullException(nameof(matrix));
            Values = (double[,])matrix.Values.Clone();
        }

        public GeoMatrix(GeoVec3f vector) : this()
        {
            Array.Copy(Identity.Values, Values, Identity.Values.Length);
            Values[3, 0] = vector[0];
            Values[3,1] = vector[1];
            Values[3,2] = vector[2];
        }

        public GeoMatrix(double[,] values) : this() 
        {
            if (values == null) 
                throw new ArgumentNullException(nameof(values));
            if(values.Length != 16)
                throw new ArgumentException("The parameter " + nameof(values) + " do not respect the size of GeoMatrix");
            Values = (double[,])values.Clone();
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (!(obj is GeoMatrix))
                return false;
            return Equals((GeoMatrix)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        hash = hash * 23 + Values[i, j].GetHashCode();
                    }
                }
                return hash;
            }
        }

        public bool Equals(in GeoMatrix other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if(this.GetHashCode() != other.GetHashCode())
                return false;
            return Values.Cast<double>()
        .SequenceEqual(other.Values.Cast<double>());
        }
        public double this[int i, int j]
        {
            get { return Values[i, j]; }
            set { Values[i, j] = value; }
        }
        public double[] this[int i]
        {
            get { return new double[4] { Values[i,0], Values[i, 1], Values[i, 2], Values[i, 3] }; }
        }
        public static bool operator ==(GeoMatrix left, GeoMatrix right) =>  left.Equals(right);
        public static bool operator !=(GeoMatrix left, GeoMatrix right) => !left.Equals(right);

        private double Det3(int r1, int r2, int r3, int c1, int c2, int c3)
        {
            double a11 = Values[r1,c1];
            double a12 = Values[r1,c2];
            double a13 = Values[r1,c3];
            double a21 = Values[r2,c1];
            double a22 = Values[r2,c2];
            double a23 = Values[r2,c3];
            double a31 = Values[r3,c1];
            double a32 = Values[r3,c2];
            double a33 = Values[r3,c3];

            double M11 = a22 * a33 - a32 * a23;
            double M21 = -(a12 * a33 - a32 * a13);
            double M31 = a12 * a23 - a22 * a13;

            return (a11 * M11 + a21 * M21 + a31 * M31);
        }
        private double Det4()
        {
            double det = 0;
            det += Values[0,0] * Det3(1, 2, 3, 1, 2, 3);
            det -= Values[1,0] * Det3(0, 2, 3, 1, 2, 3);
            det += Values[2,0] * Det3(0, 1, 3, 1, 2, 3);
            det -= Values[3,0] * Det3(0, 1, 2, 1, 2, 3);
            return det;
        }

        public void MakeIdentity()
        {
            Values = new double[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            dirty = false;
            isIdentity = true;
        }
        public GeoMatrix Inverse()
        {
            double det = Det4();
            if (det == 00)
                throw new Exception("The matrix is singular");

            GeoMatrix result = new GeoMatrix();

            result.Values[0,0] = Det3(1, 2, 3, 1, 2, 3);
            result.Values[1,0] = -Det3(1, 2, 3, 0, 2, 3);
            result.Values[2,0] = Det3(1, 2, 3, 0, 1, 3);
            result.Values[3,0] = -Det3(1, 2, 3, 0, 1, 2);
            result.Values[0,1] = -Det3(0, 2, 3, 1, 2, 3);
            result.Values[1,1] = Det3(0, 2, 3, 0, 2, 3);
            result.Values[2,1] = -Det3(0, 2, 3, 0, 1, 3);
            result.Values[3,1] = Det3(0, 2, 3, 0, 1, 2);
            result.Values[0,2] = Det3(0, 1, 3, 1, 2, 3);
            result.Values[1,2] = -Det3(0, 1, 3, 0, 2, 3);
            result.Values[2,2] = Det3(0, 1, 3, 0, 1, 3);
            result.Values[3,2] = -Det3(0, 1, 3, 0, 1, 2);
            result.Values[0,3] = -Det3(0, 1, 2, 1, 2, 3);
            result.Values[1,3] = Det3(0, 1, 2, 0, 2, 3);
            result.Values[2,3] = -Det3(0, 1, 2, 0, 1, 3);
            result.Values[3,3] = Det3(0, 1, 2, 0, 1, 2);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.Values[i,j] /= det;
                }
            }

            return result;
        }
        public GeoMatrix Transpose()
        {
            double[,] lTemp = new double[Values.GetLength(0),Values.GetLength(1)];
            Array.Copy(Values,lTemp,Values.Length);
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    double lValue = lTemp[i,j];
                    lTemp[i,j] = lTemp[j,i];
                    lTemp[j,i] = lValue;
                }
            }
            return new GeoMatrix(lTemp);
        }
        public void SetTransform(in GeoRotation iRotation)
        {
            double x = iRotation.values[0];
            double y = iRotation.values[1];
            double z = iRotation.values[2];
            double w = iRotation.values[3];

            Values[0,0] = w * w + x * x - y * y - z * z;
            Values[0,1] = 2 * x * y + 2 * w * z;
            Values[0,2] = 2 * x * z - 2 * w * y;
            Values[0,3] = 0;
                    
            Values[1,0] = 2 * x * y - 2 * w * z;
            Values[1,1] = w * w - x * x + y * y - z * z;
            Values[1,2] = 2 * y * z + 2 * w * x;
            Values[1,3] = 0;
                    
            Values[2,0] = 2 * x * z + 2 * w * y;
            Values[2,1] = 2 * y * z - 2 * w * x;
            Values[2,2] = w * w - x * x - y * y + z * z;
            Values[2,3] = 0;
                    
            Values[3,0] = 0;
            Values[3,1] = 0;
            Values[3,2] = 0;
            Values[3,3] = w * w + x * x + y * y + z * z;

            dirty = true;
        }
        public void SetTransform(in GeoVec3f iTranslate)
        {
            Values[0,0] = 1;
            Values[0,1] = 0;
            Values[0,2] = 0;
            Values[0,3] = 0;
                    
            Values[1,0] = 0;
            Values[1,1] = 1;
            Values[1,2] = 0;
            Values[1,3] = 0;
                    
            Values[2,0] = 0;
            Values[2,1] = 0;
            Values[2,2] = 1;
            Values[2,3] = 0;
                    
            Values[3,0] = iTranslate[0];
            Values[3,1] = iTranslate[1];
            Values[3,2] = iTranslate[2];
            Values[3,3] = 1;

            dirty = true;
        }
        public void SetTransform(in GeoVec3f iTranslate, in GeoRotation iRotation)
        {
            SetTransform(iRotation);

            Values[3,0] = iTranslate[0];
            Values[3,1] = iTranslate[1];
            Values[3,2] = iTranslate[2];

            dirty = true;
        }
        public void SetTransform(in GeoVec3f iTranslate, in GeoRotation iRotation, in GeoVec3f iCenter)
        {
            iCenter.values = iCenter.values.Select(x => -x).ToArray();
            SetTransform(iCenter);

            GeoMatrix R = new GeoMatrix();
            R.SetTransform(iRotation);
            MultRight(R);

            GeoMatrix T = new GeoMatrix();
            T.SetTransform(iCenter + iTranslate);
            MultRight(T);
        }
        public void MultRight(in GeoMatrix matrix)
        {
            if (Values.GetLength(1) != matrix.Values.GetLength(0))
                throw new ArgumentException("Matrices cannot be multiplied due to dimensions");
            if (!matrix.IsIdentity())
            {
                double[,] lTemp = new double[Values.GetLength(0),Values.GetLength(1)];
                Array.Copy(Values, lTemp, Values.Length);
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Values[i,j] =
                        lTemp[i,0] * matrix.Values[0,j] +
                        lTemp[i,1] * matrix.Values[1,j] +
                        lTemp[i,2] * matrix.Values[2,j] +
                        lTemp[i,3] * matrix.Values[3,j];
                    }
                }
            }
        }

        public void MultLeft(in GeoMatrix matrix)
        {
            if (!matrix.IsIdentity())
            {
                double[,] lTemp = new double[Values.GetLength(0),Values.GetLength(1)];
                Array.Copy(Values, lTemp, Values.Length);

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        Values[i, j] =
                        lTemp[0,j] * matrix.Values[i,0] +
                        lTemp[1,j] * matrix.Values[i,1] +
                        lTemp[2,j] * matrix.Values[i,2] +
                        lTemp[3,j] * matrix.Values[i,3];
                    }
                }
            }
        }
        public void ScaleBy(in GeoMatrix matrix)
        {
            Values[3,0] *= matrix.Values[0,0];
            Values[3,1] *= matrix.Values[1,1];
            Values[3,2] *= matrix.Values[2,2];

            dirty = true;
        }

        public void MultMatrixVec(in GeoVec3f src, ref GeoVec3f dst)
        {
            double[] t0 = new double[] { Values[0, 0], Values[0, 1], Values[0, 2], Values[0, 3] };
            double[] t1 = new double[] { Values[1, 0], Values[1, 1], Values[1, 2], Values[1, 3] };
            double[] t2 = new double[] { Values[2, 0], Values[2, 1], Values[2, 2], Values[2, 3] };
            double[] t3 = new double[] { Values[3, 0], Values[3, 1], Values[3, 2], Values[3, 3] };

            double W = src[0] * t3[0] + src[1] * t3[1] + src[2] * t3[2] + t3[3];
                       
            dst[0] = (src[0] * t0[0] + src[1] * t0[1] + src[2] * t0[2] + t0[3]) / W;
            dst[1] = (src[0] * t1[0] + src[1] * t1[1] + src[2] * t1[2] + t1[3]) / W;
            dst[2] = (src[0] * t2[0] + src[1] * t2[1] + src[2] * t2[2] + t2[3]) / W;
        }
        public void MultVecMatrix(in GeoVec4f src, ref GeoVec4f dst)
        {
            double[] t0 = new double[] { Values[0, 0], Values[0, 1], Values[0, 2], Values[0, 3] };
            double[] t1 = new double[] { Values[1, 0], Values[1, 1], Values[1, 2], Values[1, 3] };
            double[] t2 = new double[] { Values[2, 0], Values[2, 1], Values[2, 2], Values[2, 3] };
            double[] t3 = new double[] { Values[3, 0], Values[3, 1], Values[3, 2], Values[3, 3] };

            dst[0] = src[0] * t0[0] + src[1] * t1[0] + src[2] * t2[0] + t3[0];
            dst[1] = src[0] * t0[1] + src[1] * t1[1] + src[2] * t2[1] + t3[1];
            dst[2] = src[0] * t0[2] + src[1] * t1[2] + src[2] * t2[2] + t3[2];
            dst[3] = src[0] * t0[3] + src[1] * t1[3] + src[2] * t2[3] + t3[3];
        }
        public void MultDirMatrix(in GeoVec3f src, ref GeoVec3f dst)
        {
            double[] t0 = new double[] { Values[0, 0], Values[0, 1], Values[0, 2], Values[0, 3] };
            double[] t1 = new double[] { Values[1, 0], Values[1, 1], Values[1, 2], Values[1, 3] };
            double[] t2 = new double[] { Values[2, 0], Values[2, 1], Values[2, 2], Values[2, 3] };
            double[] t3 = new double[] { Values[3, 0], Values[3, 1], Values[3, 2], Values[3, 3] };

            dst[0] = src[0] * t0[0] + src[1] * t1[0] + src[2] * t2[0];
            dst[1] = src[0] * t0[1] + src[1] * t1[1] + src[2] * t2[1];
            dst[2] = src[0] * t0[2] + src[1] * t1[2] + src[2] * t2[2];
        }
        public void MultVecMatrix(in GeoVec3f src, ref GeoVec3f dst)
        {
            double[] t0 = new double[] { Values[0, 0], Values[0, 1], Values[0, 2], Values[0, 3] };
            double[] t1 = new double[] { Values[1, 0], Values[1, 1], Values[1, 2], Values[1, 3] };
            double[] t2 = new double[] { Values[2, 0], Values[2, 1], Values[2, 2], Values[2, 3] };
            double[] t3 = new double[] { Values[3, 0], Values[3, 1], Values[3, 2], Values[3, 3] };

            double W = src[0] * t0[3] + src[1] * t1[3] + src[2] * t2[3] + t3[3];

            dst[0] = (src[0] * t0[0] + src[1] * t1[0] + src[2] * t2[0] + t3[0]) / W;
            dst[1] = (src[0] * t0[1] + src[1] * t1[1] + src[2] * t2[1] + t3[1]) / W;
            dst[2] = (src[0] * t0[2] + src[1] * t1[2] + src[2] * t2[2] + t3[2]) / W;
        }
        public bool IsIdentity()
        {
            if (dirty)
            {
                isIdentity = true;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        if (Values[i,j] != Identity.Values[i,j])
                        {
                            isIdentity = false;
                            break;
                        }
                    }
                }
                dirty = false;
            }

            return isIdentity;
        }
    }
}
