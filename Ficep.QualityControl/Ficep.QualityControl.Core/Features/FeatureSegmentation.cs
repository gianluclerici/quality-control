using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// Assigns a point (typically an aligned scan point) to the machining feature it belongs to, by
/// nearest cutter surface. Each feature's cutter boundary coincides with the feature's surface on the
/// part, so a point sitting on (within <see cref="OnSurfaceToleranceMm"/> of) a cutter belongs to that
/// feature; a point far from every cutter is on the unmachined body and maps to "base".
/// <para>
/// All cutter triangles go into one labelled <see cref="TriangleBvh"/> (the same exact closest-point
/// primitive ICP and the deviation measurement use), so a query is ~O(log n) and scales to whole
/// clouds. Segmentation is purely geometric and therefore deterministic.
/// </para>
/// </summary>
public sealed class FeatureSegmentation
{
    /// <summary>Default tessellation chord tolerance (mm) for the cutter meshes.</summary>
    public const double DefaultChordToleranceMm = 0.2;

    /// <summary>
    /// Default on-surface band (mm): how close a point must be to a cutter boundary to be claimed by
    /// that feature. Generous enough to absorb scan noise and alignment error, tight enough that the
    /// flat body away from any cut stays "base".
    /// </summary>
    public const double DefaultOnSurfaceToleranceMm = 1.0;

    private readonly TriangleBvh? _bvh;
    private readonly int[] _triangleFeature; // parallel to cutter triangles → index into _features
    private readonly FeatureDescriptor[] _features;
    private readonly double _onSurfaceTol;

    private FeatureSegmentation(TriangleBvh? bvh, int[] triangleFeature, FeatureDescriptor[] features, double onSurfaceTol)
    {
        _bvh = bvh;
        _triangleFeature = triangleFeature;
        _features = features;
        _onSurfaceTol = onSurfaceTol;
    }

    /// <summary>The features this segmentation can assign to, in classification order (index = feature index).</summary>
    public IReadOnlyList<FeatureDescriptor> Features => _features;

    /// <summary>The on-surface band (mm) within which a point is claimed by its nearest cutter.</summary>
    public double OnSurfaceToleranceMm => _onSurfaceTol;

    /// <summary>
    /// Builds a segmentation from the feature cutters (e.g. <c>MachinedBeam.Features</c>).
    /// </summary>
    /// <param name="cutters">The labelled cutter solids.</param>
    /// <param name="onSurfaceToleranceMm">On-surface band; see <see cref="DefaultOnSurfaceToleranceMm"/>.</param>
    /// <param name="chordToleranceMm">Cutter tessellation chord tolerance; see <see cref="DefaultChordToleranceMm"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cutters"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A tolerance is not a positive number.</exception>
    public static FeatureSegmentation FromCutters(
        IReadOnlyList<FeatureCutter> cutters,
        double onSurfaceToleranceMm = DefaultOnSurfaceToleranceMm,
        double chordToleranceMm = DefaultChordToleranceMm)
    {
        ArgumentNullException.ThrowIfNull(cutters);
        if (onSurfaceToleranceMm <= 0 || double.IsNaN(onSurfaceToleranceMm))
            throw new ArgumentOutOfRangeException(nameof(onSurfaceToleranceMm), onSurfaceToleranceMm,
                "On-surface tolerance must be a positive distance in millimetres.");
        if (chordToleranceMm <= 0 || double.IsNaN(chordToleranceMm))
            throw new ArgumentOutOfRangeException(nameof(chordToleranceMm), chordToleranceMm,
                "Chord tolerance must be a positive distance in millimetres.");

        var descriptors = new FeatureDescriptor[cutters.Count];
        var a = new List<Vec3>();
        var b = new List<Vec3>();
        var c = new List<Vec3>();
        var label = new List<int>();

        for (int fi = 0; fi < cutters.Count; fi++)
        {
            FeatureCutter cutter = cutters[fi];
            descriptors[fi] = cutter.Descriptor;
            if (cutter.Cutter is null)
                continue;

            Mesh mesh = BrepTessellator.ToMesh(cutter.Cutter, chordToleranceMm);
            Point3D[] vertices = mesh.Vertices;
            IndexTriangle[] triangles = mesh.Triangles;
            if (vertices is null || triangles is null || vertices.Length == 0 || triangles.Length == 0)
                continue;

            foreach (IndexTriangle t in triangles)
            {
                a.Add(ToVec3(vertices[t.V1]));
                b.Add(ToVec3(vertices[t.V2]));
                c.Add(ToVec3(vertices[t.V3]));
                label.Add(fi);
            }
        }

        if (a.Count == 0)
            return new FeatureSegmentation(null, Array.Empty<int>(), descriptors, onSurfaceToleranceMm);

        Vec3[] aa = a.ToArray(), bb = b.ToArray(), cc = c.ToArray();
        TriangleBvh bvh = TriangleBvh.Build(aa, bb, cc);
        return new FeatureSegmentation(bvh, label.ToArray(), descriptors, onSurfaceToleranceMm);
    }

    /// <summary>
    /// Returns the index into <see cref="Features"/> of the feature <paramref name="point"/> belongs to,
    /// or -1 if it is on the unmachined body (base).
    /// </summary>
    public int Classify(Point3D point) => Classify(ToVec3(point));

    internal int Classify(Vec3 query)
    {
        if (_bvh is null)
            return -1;
        int t = _bvh.ClosestTriangle(query, out Vec3 cp);
        double distance = (query - cp).Length;
        return distance <= _onSurfaceTol ? _triangleFeature[t] : -1;
    }

    private static Vec3 ToVec3(Point3D p) => new(p.X, p.Y, p.Z);
}
