using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// A cope wall as seen by the inspector: its world plane (unit <see cref="Normal"/> and signed
/// <see cref="Offset"/> = n̂·p), plus the same line expressed in the profile plane
/// (<c>Nu·a + Nv·b = D2</c>) used to locate the corner and to test whether a point lies on the wall.
/// </summary>
internal readonly record struct WallLine(Vec3 Normal, double Offset, double Nu, double Nv, double D2);

/// <summary>
/// The geometry of a contour-extrusion cutter (a cope), derived from its Brep faces. The cope is a 2D
/// profile extruded along an axis, so its faces are two end caps (normal ∥ axis), several planar walls
/// (normal ⊥ axis) and one cylindrical fillet (a non-planar face). This type recovers, exactly from the
/// cutter:
/// <list type="bullet">
///   <item>the extrusion <b>axis</b> and an orthonormal profile basis (<see cref="U"/>, <see cref="V"/>)
///   about a point <see cref="Origin"/> on the axis;</item>
///   <item>the <b>back</b> and <b>depth</b> walls (the two walls whose offsets match the nominal length
///   and depth — the same two that the fillet rounds);</item>
///   <item>the profile-plane <b>corner</b> where those two walls meet.</item>
/// </list>
/// With these known, each cloud measurement collapses to its minimal unknown (offset of a wall, radius
/// of the fillet) — see <see cref="NotchInspection"/> and the research note.
/// </summary>
internal sealed class ExtrudedProfile
{
    public required Vec3 Origin { get; init; }
    public required Vec3 U { get; init; }
    public required Vec3 V { get; init; }
    public required WallLine BackWall { get; init; }
    public required WallLine DepthWall { get; init; }
    public required double CornerA { get; init; }
    public required double CornerB { get; init; }

    private const double Eps = 0.02;

    /// <summary>True if <paramref name="cutter"/> has at least one non-planar (fillet) face.</summary>
    public static bool HasFillet(Brep cutter)
    {
        ArgumentNullException.ThrowIfNull(cutter);
        Brep.Face[]? faces = cutter.Faces;
        if (faces is null)
            return false;
        foreach (Brep.Face f in faces)
            if (f.IsPlanar() is null)
                return true;
        return false;
    }

    /// <summary>Derives the cope geometry from <paramref name="cutter"/>, using the nominal length/depth to label the two bounding walls.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="cutter"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The cutter is not a recognisable contour extrusion.</exception>
    public static ExtrudedProfile FromCutter(Brep cutter, NotchNominals nominal)
    {
        ArgumentNullException.ThrowIfNull(cutter);

        // Collect the planar faces as (world normal, world offset) and a frame origin from any cap.
        var planar = new List<(Vec3 N, double Off, Point3D Org)>();
        Brep.Face[]? faces = cutter.Faces;
        if (faces is not null)
        {
            foreach (Brep.Face f in faces)
            {
                if (f.IsPlanar() is not Plane pl)
                    continue;
                Vec3 n = new Vec3(pl.AxisZ.X, pl.AxisZ.Y, pl.AxisZ.Z).Normalized();
                if (n.LengthSquared <= 0)
                    continue;
                double off = n.X * pl.Origin.X + n.Y * pl.Origin.Y + n.Z * pl.Origin.Z;
                planar.Add((n, off, pl.Origin));
            }
        }
        if (planar.Count < 3)
            throw new InvalidOperationException("The cope cutter has too few planar faces to derive its frame.");

        // The extrusion axis is the planar normal perpendicular to the most other planar normals
        // (every wall is ⊥ to the axis; the caps are ∥ to it).
        Vec3 axis = PickAxis(planar);
        Vec3 origin = FirstCapOrigin(planar, axis);
        (Vec3 u, Vec3 v) = OrthonormalBasis(axis);

        // Walls are the planar faces whose normal is ⊥ to the axis.
        var walls = new List<WallLine>();
        foreach ((Vec3 N, double Off, Point3D _) in planar)
        {
            if (Math.Abs(Vec3.Dot(N, axis)) > Eps)
                continue; // a cap, not a wall
            double nu = Vec3.Dot(N, u);
            double nv = Vec3.Dot(N, v);
            double d2 = Off - Vec3.Dot(origin, N);
            walls.Add(new WallLine(N, Off, nu, nv, d2));
        }
        if (walls.Count < 2)
            throw new InvalidOperationException("The cope cutter has fewer than two lateral walls.");

        WallLine back = NearestWall(walls, nominal.LengthMm);
        WallLine depth = NearestWall(walls, nominal.DepthMm, exclude: back);

        (double cornerA, double cornerB) = Intersect(back, depth);

        return new ExtrudedProfile
        {
            Origin = origin, U = u, V = v,
            BackWall = back, DepthWall = depth,
            CornerA = cornerA, CornerB = cornerB,
        };
    }

    private static Vec3 PickAxis(List<(Vec3 N, double Off, Point3D Org)> planar)
    {
        Vec3 best = planar[0].N;
        int bestCount = -1;
        foreach ((Vec3 N, double _, Point3D _) in planar)
        {
            int count = 0;
            foreach ((Vec3 M, double _, Point3D _) in planar)
                if (Math.Abs(Vec3.Dot(N, M)) < Eps)
                    count++;
            if (count > bestCount)
            {
                bestCount = count;
                best = N;
            }
        }
        return best.Normalized();
    }

    private static Vec3 FirstCapOrigin(List<(Vec3 N, double Off, Point3D Org)> planar, Vec3 axis)
    {
        foreach ((Vec3 N, double _, Point3D Org) in planar)
            if (Math.Abs(Vec3.Dot(N, axis)) > 1 - Eps)
                return new Vec3(Org.X, Org.Y, Org.Z);
        Point3D o = planar[0].Org;
        return new Vec3(o.X, o.Y, o.Z);
    }

    /// <summary>The wall whose |offset| is closest to <paramref name="target"/> (optionally skipping one).</summary>
    private static WallLine NearestWall(List<WallLine> walls, double target, WallLine? exclude = null)
    {
        WallLine best = default;
        double bestErr = double.PositiveInfinity;
        bool found = false;
        foreach (WallLine w in walls)
        {
            if (exclude is WallLine e && w.Equals(e))
                continue;
            double err = Math.Abs(Math.Abs(w.Offset) - target);
            if (err < bestErr)
            {
                bestErr = err;
                best = w;
                found = true;
            }
        }
        if (!found)
            throw new InvalidOperationException("Could not identify a cope bounding wall.");
        return best;
    }

    /// <summary>Profile-plane intersection (a, b) of two wall lines <c>Nu·a + Nv·b = D2</c>.</summary>
    private static (double A, double B) Intersect(WallLine p, WallLine q)
    {
        double det = p.Nu * q.Nv - q.Nu * p.Nv;
        if (Math.Abs(det) < 1e-9)
            throw new InvalidOperationException("The two cope walls are parallel; no corner.");
        double a = (p.D2 * q.Nv - q.D2 * p.Nv) / det;
        double b = (p.Nu * q.D2 - q.Nu * p.D2) / det;
        return (a, b);
    }

    private static (Vec3 U, Vec3 V) OrthonormalBasis(Vec3 axis)
    {
        Vec3 a = axis.Normalized();
        Vec3 seed = Math.Abs(a.X) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
        Vec3 u = Vec3.Cross(a, seed).Normalized();
        Vec3 v = Vec3.Cross(a, u).Normalized();
        return (u, v);
    }
}
