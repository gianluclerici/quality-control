using devDept.Geometry;

namespace Ficep.QualityControl.Core.Registration;

/// <summary>
/// A rigid-body transform (rotation + translation, no scale) — the alignment ICP solves for. Stored
/// as a 3×3 row-major rotation plus a translation. Exposes <see cref="Apply(Point3D)"/> for callers and
/// <see cref="ToTransformation"/> to hand the result to Eyeshot (<c>Entity.TransformBy</c>, etc.).
/// </summary>
public readonly struct RigidTransform
{
    // Rotation, row-major: indices 0..8 = r00 r01 r02 r10 r11 r12 r20 r21 r22.
    private readonly double _r0, _r1, _r2, _r3, _r4, _r5, _r6, _r7, _r8;
    private readonly double _tx, _ty, _tz;

    private RigidTransform(
        double r0, double r1, double r2,
        double r3, double r4, double r5,
        double r6, double r7, double r8,
        double tx, double ty, double tz)
    {
        _r0 = r0; _r1 = r1; _r2 = r2;
        _r3 = r3; _r4 = r4; _r5 = r5;
        _r6 = r6; _r7 = r7; _r8 = r8;
        _tx = tx; _ty = ty; _tz = tz;
    }

    /// <summary>The identity transform (no rotation, no translation).</summary>
    public static RigidTransform Identity { get; } =
        new(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0);

    /// <summary>Transforms an Eyeshot point.</summary>
    public Point3D Apply(Point3D p)
    {
        Vec3 r = Apply(new Vec3(p.X, p.Y, p.Z));
        return new Point3D(r.X, r.Y, r.Z);
    }

    internal Vec3 Apply(Vec3 p) => new(
        _r0 * p.X + _r1 * p.Y + _r2 * p.Z + _tx,
        _r3 * p.X + _r4 * p.Y + _r5 * p.Z + _ty,
        _r6 * p.X + _r7 * p.Y + _r8 * p.Z + _tz);

    /// <summary>
    /// Returns <c>this ∘ <paramref name="inner"/></c>: the transform whose effect is applying
    /// <paramref name="inner"/> first, then this one. Used to accumulate ICP increments.
    /// </summary>
    public RigidTransform Compose(RigidTransform inner)
    {
        // R = this.R * inner.R
        double r0 = _r0 * inner._r0 + _r1 * inner._r3 + _r2 * inner._r6;
        double r1 = _r0 * inner._r1 + _r1 * inner._r4 + _r2 * inner._r7;
        double r2 = _r0 * inner._r2 + _r1 * inner._r5 + _r2 * inner._r8;
        double r3 = _r3 * inner._r0 + _r4 * inner._r3 + _r5 * inner._r6;
        double r4 = _r3 * inner._r1 + _r4 * inner._r4 + _r5 * inner._r7;
        double r5 = _r3 * inner._r2 + _r4 * inner._r5 + _r5 * inner._r8;
        double r6 = _r6 * inner._r0 + _r7 * inner._r3 + _r8 * inner._r6;
        double r7 = _r6 * inner._r1 + _r7 * inner._r4 + _r8 * inner._r7;
        double r8 = _r6 * inner._r2 + _r7 * inner._r5 + _r8 * inner._r8;

        // t = this.R * inner.t + this.t
        double tx = _r0 * inner._tx + _r1 * inner._ty + _r2 * inner._tz + _tx;
        double ty = _r3 * inner._tx + _r4 * inner._ty + _r5 * inner._tz + _ty;
        double tz = _r6 * inner._tx + _r7 * inner._ty + _r8 * inner._tz + _tz;

        return new RigidTransform(r0, r1, r2, r3, r4, r5, r6, r7, r8, tx, ty, tz);
    }

    /// <summary>Converts to an Eyeshot <see cref="Transformation"/> (matrix indexed [row, column]).</summary>
    public Transformation ToTransformation()
    {
        var m = new double[4, 4];
        m[0, 0] = _r0; m[0, 1] = _r1; m[0, 2] = _r2; m[0, 3] = _tx;
        m[1, 0] = _r3; m[1, 1] = _r4; m[1, 2] = _r5; m[1, 3] = _ty;
        m[2, 0] = _r6; m[2, 1] = _r7; m[2, 2] = _r8; m[2, 3] = _tz;
        m[3, 0] = 0;   m[3, 1] = 0;   m[3, 2] = 0;   m[3, 3] = 1;
        return new Transformation { Matrix = m };
    }

    /// <summary>Pure translation.</summary>
    public static RigidTransform FromTranslation(double tx, double ty, double tz) =>
        new(1, 0, 0, 0, 1, 0, 0, 0, 1, tx, ty, tz);

    /// <summary>
    /// Builds a transform from a rotation expressed as a rotation vector (axis × angle, radians) and a
    /// translation. Uses Rodrigues' formula, so it stays a proper rotation for any angle — ICP feeds it
    /// the small per-iteration increment solved from the point-to-plane system.
    /// </summary>
    internal static RigidTransform FromRotationVector(double wx, double wy, double wz, double tx, double ty, double tz)
    {
        double theta = Math.Sqrt(wx * wx + wy * wy + wz * wz);
        if (theta < 1e-12)
            return new RigidTransform(1, 0, 0, 0, 1, 0, 0, 0, 1, tx, ty, tz);

        double kx = wx / theta, ky = wy / theta, kz = wz / theta;
        double c = Math.Cos(theta), s = Math.Sin(theta), t = 1 - c;

        double r0 = c + kx * kx * t;
        double r1 = kx * ky * t - kz * s;
        double r2 = kx * kz * t + ky * s;
        double r3 = ky * kx * t + kz * s;
        double r4 = c + ky * ky * t;
        double r5 = ky * kz * t - kx * s;
        double r6 = kz * kx * t - ky * s;
        double r7 = kz * ky * t + kx * s;
        double r8 = c + kz * kz * t;

        return new RigidTransform(r0, r1, r2, r3, r4, r5, r6, r7, r8, tx, ty, tz);
    }
}
