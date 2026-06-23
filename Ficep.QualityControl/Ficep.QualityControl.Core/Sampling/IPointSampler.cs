using devDept.Eyeshot.Entities;

namespace Ficep.QualityControl.Core.Sampling;

/// <summary>
/// Strategy for drawing a set of <see cref="SurfaceSample"/>s from the triangulated
/// surface of a solid. Implementations decide <em>how</em> points are placed
/// (uniform area-weighted, blue-noise, single-viewpoint scan, ...); callers depend
/// only on this abstraction so the sampling technique can be swapped without changing
/// the generation pipeline. See <c>docs/research/mesh-surface-sampling.md</c>.
/// </summary>
public interface IPointSampler
{
    /// <summary>
    /// Samples points on the given triangle <paramref name="mesh"/> (typically obtained
    /// from <c>Brep.ConvertToMesh</c>). The returned points carry an outward unit normal.
    /// </summary>
    /// <param name="mesh">The triangulated surface to sample. Must have vertices and triangles.</param>
    /// <returns>The sampled surface points; never <c>null</c> (may be empty for a degenerate mesh).</returns>
    IReadOnlyList<SurfaceSample> Sample(Mesh mesh);
}
