using devDept.Eyeshot.Entities;
using devDept.Geometry;

namespace Ficep.QualityControl.Core.Sampling;

/// <summary>
/// Per-triangle <b>area-weighted uniform</b> surface sampler.
/// <para>
/// A triangle is chosen with probability proportional to its area (cumulative-area
/// table + binary search), then a point is drawn uniformly inside it using the
/// barycentric "√-trick". This yields an unbiased, constant <em>expected</em> density
/// over the true surface regardless of how the tessellation distributes triangle sizes.
/// The number of points is driven by a target <see cref="DensityPerMm2"/> so the cloud
/// resolution is controlled the way a scan resolution would be.
/// </para>
/// <para>
/// Each sample's normal is the triangle's geometric (flat) normal — exact for the
/// planar / developable faces of a machined steel beam. Sampling is fully deterministic
/// for a given seed. Rationale and alternatives: <c>docs/research/mesh-surface-sampling.md</c>.
/// </para>
/// </summary>
public sealed class MeshSurfaceSampler : IPointSampler
{
    private readonly Random _rng;

    /// <summary>Target sampling density in points per square millimetre.</summary>
    public double DensityPerMm2 { get; }

    /// <summary>
    /// Creates the sampler.
    /// </summary>
    /// <param name="densityPerMm2">Target density (points/mm²); must be positive.</param>
    /// <param name="seed">
    /// Optional RNG seed. When provided, sampling is reproducible (identical mesh + seed
    /// ⇒ identical cloud); when omitted a time-based seed is used.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">Density is not positive.</exception>
    public MeshSurfaceSampler(double densityPerMm2, int? seed = null)
    {
        if (densityPerMm2 <= 0 || double.IsNaN(densityPerMm2))
            throw new ArgumentOutOfRangeException(nameof(densityPerMm2), densityPerMm2,
                "Sampling density must be a positive number of points per mm².");

        DensityPerMm2 = densityPerMm2;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <inheritdoc/>
    public IReadOnlyList<SurfaceSample> Sample(Mesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        Point3D[] vertices = mesh.Vertices;
        IndexTriangle[] triangles = mesh.Triangles;
        if (vertices is null || triangles is null || vertices.Length == 0 || triangles.Length == 0)
            return Array.Empty<SurfaceSample>();

        // Pre-compute per-triangle geometric normal and a cumulative-area table.
        // Degenerate (zero-area) triangles contribute zero width and are never selected.
        var normals = new Vector3D[triangles.Length];
        var cumulativeArea = new double[triangles.Length];
        double runningArea = 0.0;

        for (int i = 0; i < triangles.Length; i++)
        {
            IndexTriangle t = triangles[i];
            Point3D a = vertices[t.V1];
            Point3D b = vertices[t.V2];
            Point3D c = vertices[t.V3];

            var ab = new Vector3D(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var ac = new Vector3D(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            Vector3D cross = Vector3D.Cross(ab, ac);

            double twiceArea = cross.Length;          // |AB × AC| = 2 · area
            runningArea += 0.5 * twiceArea;
            cumulativeArea[i] = runningArea;

            // Unit normal from the winding order (outward for a well-formed solid mesh).
            if (twiceArea > 0)
                cross.Normalize();
            normals[i] = cross;
        }

        double totalArea = runningArea;
        if (totalArea <= 0)
            return Array.Empty<SurfaceSample>();

        int nPoints = (int)Math.Round(DensityPerMm2 * totalArea, MidpointRounding.AwayFromZero);
        if (nPoints <= 0)
            return Array.Empty<SurfaceSample>();

        var samples = new List<SurfaceSample>(nPoints);
        for (int p = 0; p < nPoints; p++)
        {
            int ti = PickTriangle(cumulativeArea, totalArea);
            IndexTriangle t = triangles[ti];
            Point3D pt = PointInTriangle(vertices[t.V1], vertices[t.V2], vertices[t.V3]);
            samples.Add(new SurfaceSample(pt, normals[ti]));
        }

        return samples;
    }

    /// <summary>
    /// Selects a triangle index with probability proportional to its area by drawing
    /// <c>u ~ U(0, totalArea)</c> and binary-searching the cumulative-area table.
    /// </summary>
    private int PickTriangle(double[] cumulativeArea, double totalArea)
    {
        double u = _rng.NextDouble() * totalArea;
        int idx = Array.BinarySearch(cumulativeArea, u);
        if (idx < 0)
            idx = ~idx;                 // first cumulative entry strictly greater than u
        if (idx >= cumulativeArea.Length)
            idx = cumulativeArea.Length - 1;
        return idx;
    }

    /// <summary>
    /// Draws a uniformly distributed point inside triangle <c>(a, b, c)</c> using the
    /// barycentric √-trick: with <c>s = √r1</c>,
    /// <c>P = (1 − s)·a + s·(1 − r2)·b + s·r2·c</c>.
    /// </summary>
    private Point3D PointInTriangle(Point3D a, Point3D b, Point3D c)
    {
        double s = Math.Sqrt(_rng.NextDouble());
        double r2 = _rng.NextDouble();

        double wA = 1.0 - s;
        double wB = s * (1.0 - r2);
        double wC = s * r2;

        return new Point3D(
            wA * a.X + wB * b.X + wC * c.X,
            wA * a.Y + wB * b.Y + wC * c.Y,
            wA * a.Z + wB * b.Z + wC * c.Z);
    }
}
