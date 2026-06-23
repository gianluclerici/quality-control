using Ficep.QualityControl.Core.Noise;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Generation;

/// <summary>
/// Tunable parameters of the synthetic-scan generation: how densely the surface is
/// sampled, how much range noise is added, and the RNG seed for reproducibility.
/// </summary>
/// <param name="DensityPerMm2">Surface sampling density in points/mm² (passed to <see cref="MeshSurfaceSampler"/>).</param>
/// <param name="SigmaMm">Along-normal Gaussian noise σ in mm (passed to <see cref="GaussianRangeNoise"/>).</param>
/// <param name="Seed">
/// Optional master seed. When set, the whole generation is reproducible; per-stage and
/// per-piece seeds are derived from it deterministically. When null, a random seed is used.
/// </param>
public readonly record struct GenerationOptions(double DensityPerMm2, double SigmaMm, int? Seed)
{
    /// <summary>
    /// Default density: 1 point/mm² (~one point per mm), a plausible mid-range scan
    /// resolution that keeps a 1 m beam's cloud to a manageable size. Raise with
    /// <c>--density</c> for finer scans.
    /// </summary>
    public const double DefaultDensityPerMm2 = 1.0;

    /// <summary>Sensible defaults: 5 pts/mm², σ = 0.1 mm, no fixed seed.</summary>
    public static GenerationOptions Default => new(DefaultDensityPerMm2, GaussianRangeNoise.DefaultSigmaMm, null);
}
