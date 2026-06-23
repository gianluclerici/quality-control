using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Noise;

/// <summary>
/// Strategy that perturbs a clean <see cref="SurfaceSample"/> so the resulting cloud
/// behaves like a real 3D-camera acquisition rather than a perfect CAD surface.
/// Implementations encapsulate a specific sensor-noise model; callers depend only on
/// this abstraction so the model can be upgraded (e.g. depth/angle-dependent σ, lateral
/// noise, outliers) without touching the pipeline. See <c>docs/research/depth-camera-noise.md</c>.
/// </summary>
public interface INoiseModel
{
    /// <summary>Returns a noisy copy of <paramref name="sample"/>.</summary>
    SurfaceSample Apply(SurfaceSample sample);

    /// <summary>Applies <see cref="Apply(SurfaceSample)"/> to every input sample, in order.</summary>
    IReadOnlyList<SurfaceSample> Apply(IReadOnlyList<SurfaceSample> samples);
}
