using devDept.Geometry;

namespace Ficep.QualityControl.Core.Sampling;

/// <summary>
/// A single point sampled on a surface: its 3D position plus the outward-facing
/// unit normal of the surface at that point. The normal is what the downstream
/// noise model perturbs along and what plane/cylinder fitting consumes during the
/// quality-control measurement step.
/// </summary>
/// <param name="Position">The sampled point in model space (mm).</param>
/// <param name="Normal">Unit-length, outward-oriented surface normal at <paramref name="Position"/>.</param>
public readonly record struct SurfaceSample(Point3D Position, Vector3D Normal);
