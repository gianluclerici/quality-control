namespace Ficep.QualityControl.Core.Registration;

/// <summary>
/// Lightweight 3D vector used in the registration hot loops. A value type so that the per-point
/// closest-surface and ICP math (run over hundreds of thousands of points) allocates nothing, where
/// Eyeshot's reference-type <c>Point3D</c>/<c>Vector3D</c> would create GC pressure. Converted to and
/// from the Eyeshot types only at the public boundary.
/// </summary>
internal readonly struct Vec3
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public Vec3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator *(Vec3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);

    public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public double LengthSquared => X * X + Y * Y + Z * Z;

    public double Length => Math.Sqrt(LengthSquared);

    /// <summary>Returns the unit vector; returns the zero vector unchanged.</summary>
    public Vec3 Normalized()
    {
        double len = Length;
        return len > 0 ? new Vec3(X / len, Y / len, Z / len) : this;
    }
}
