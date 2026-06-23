using devDept.Eyeshot.Entities;
using devDept.Geometry;

namespace Ficep.QualityControl.Core.Registration;

/// <summary>
/// The projection of a query point onto the nominal surface: the closest point on the model, the
/// (outward) surface normal there, and the unsigned distance to it.
/// </summary>
/// <param name="Point">Closest point on the nominal surface (mm).</param>
/// <param name="Normal">Unit outward normal of the nominal surface at <paramref name="Point"/>.</param>
/// <param name="Distance">Unsigned distance from the query point to <paramref name="Point"/> (mm).</param>
public readonly record struct SurfaceProjection(Point3D Point, Vector3D Normal, double Distance);

/// <summary>
/// A queryable nominal surface: the tessellated CAD reference (a triangle <see cref="Mesh"/>) wrapped
/// with a triangle BVH so any point can be projected to its closest point and surface normal. This is
/// the shared geometric primitive behind both registration (ICP needs the closest point and normal as
/// correspondences) and the deviation measurement (which needs the closest-point distance per scan
/// point). The query is exact and allocation-free, so it scales to whole clouds.
/// </summary>
public sealed class NominalSurface
{
    private readonly Vec3[] _a;
    private readonly Vec3[] _b;
    private readonly Vec3[] _c;
    private readonly Vec3[] _normal;
    private readonly TriangleBvh _bvh;

    private NominalSurface(Vec3[] a, Vec3[] b, Vec3[] c, Vec3[] normal, TriangleBvh bvh)
    {
        _a = a;
        _b = b;
        _c = c;
        _normal = normal;
        _bvh = bvh;
    }

    /// <summary>
    /// Builds a queryable surface from a triangle mesh (e.g. <c>BrepTessellator.ToMesh(nominalBrep, …)</c>).
    /// </summary>
    /// <param name="mesh">The tessellated nominal model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="mesh"/> is null.</exception>
    /// <exception cref="ArgumentException">The mesh has no vertices or triangles.</exception>
    public static NominalSurface FromMesh(Mesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        Point3D[] vertices = mesh.Vertices;
        IndexTriangle[] triangles = mesh.Triangles;
        if (vertices is null || triangles is null || vertices.Length == 0 || triangles.Length == 0)
            throw new ArgumentException("Mesh has no geometry to query.", nameof(mesh));

        int n = triangles.Length;
        var a = new Vec3[n];
        var b = new Vec3[n];
        var c = new Vec3[n];
        var normal = new Vec3[n];

        for (int i = 0; i < n; i++)
        {
            IndexTriangle t = triangles[i];
            Vec3 va = ToVec3(vertices[t.V1]);
            Vec3 vb = ToVec3(vertices[t.V2]);
            Vec3 vc = ToVec3(vertices[t.V3]);
            a[i] = va;
            b[i] = vb;
            c[i] = vc;
            normal[i] = Vec3.Cross(vb - va, vc - va).Normalized();
        }

        return new NominalSurface(a, b, c, normal, TriangleBvh.Build(a, b, c));
    }

    /// <summary>Projects <paramref name="query"/> onto the nominal surface.</summary>
    public SurfaceProjection ClosestPoint(Point3D query) => ClosestPoint(ToVec3(query));

    internal SurfaceProjection ClosestPoint(Vec3 query)
    {
        int t = _bvh.ClosestTriangle(query, out Vec3 cp);
        Vec3 nrm = _normal[t];
        return new SurfaceProjection(
            new Point3D(cp.X, cp.Y, cp.Z),
            new Vector3D(nrm.X, nrm.Y, nrm.Z),
            (query - cp).Length);
    }

    /// <summary>Reference closest-point that scans every triangle — for validating the BVH path in tests.</summary>
    internal SurfaceProjection ClosestPointBruteForce(Vec3 query)
    {
        double bestDistSq = double.MaxValue;
        Vec3 bestPoint = default;
        int bestTriangle = 0;
        for (int t = 0; t < _a.Length; t++)
        {
            Vec3 cp = PointTriangleDistance.ClosestPoint(query, _a[t], _b[t], _c[t]);
            double d2 = (query - cp).LengthSquared;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                bestPoint = cp;
                bestTriangle = t;
            }
        }
        Vec3 nrm = _normal[bestTriangle];
        return new SurfaceProjection(
            new Point3D(bestPoint.X, bestPoint.Y, bestPoint.Z),
            new Vector3D(nrm.X, nrm.Y, nrm.Z),
            Math.Sqrt(bestDistSq));
    }

    private static Vec3 ToVec3(Point3D p) => new(p.X, p.Y, p.Z);
}
