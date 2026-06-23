using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Io;

/// <summary>
/// Strategy for persisting a sampled point cloud to a file. Implementations choose the
/// on-disk format (PLY, LAS, ...); callers depend only on this abstraction.
/// </summary>
public interface IPointCloudWriter
{
    /// <summary>Writes <paramref name="samples"/> (position + normal per point) to <paramref name="path"/>, overwriting it.</summary>
    void Write(string path, IReadOnlyList<SurfaceSample> samples);
}
