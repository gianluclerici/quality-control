using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Io;

/// <summary>
/// Strategy for loading a point cloud back from a file into <see cref="SurfaceSample"/>s.
/// The inverse of <see cref="IPointCloudWriter"/>: callers depend only on this abstraction,
/// not on the concrete on-disk format (PLY, LAS, ...).
/// </summary>
public interface IPointCloudReader
{
    /// <summary>Reads the point cloud (position + normal per point) stored at <paramref name="path"/>.</summary>
    IReadOnlyList<SurfaceSample> Read(string path);
}
