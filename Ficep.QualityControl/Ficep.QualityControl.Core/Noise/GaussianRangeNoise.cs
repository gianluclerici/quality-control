using devDept.Geometry;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Noise;

/// <summary>
/// Range noise applied <b>along each point's surface normal</b> with a constant
/// Gaussian standard deviation σ.
/// <para>
/// This reproduces the dominant, fit-relevant error of optical depth sensors — the
/// axial (along-the-ray) component that displaces points perpendicular to the surface —
/// without modelling a specific viewpoint yet. Under full-surface sampling the surface
/// normal is the best available proxy for the viewing ray. The normal itself is left
/// unchanged (it stays the nominal surface direction). Perturbation is reproducible for
/// a given seed. Rationale and the planned upgrade to a depth/angle-dependent σ:
/// <c>docs/research/depth-camera-noise.md</c>.
/// </para>
/// </summary>
public sealed class GaussianRangeNoise : INoiseModel
{
    /// <summary>Default range-noise standard deviation (mm), a typical close-range depth-sensor figure.</summary>
    public const double DefaultSigmaMm = 0.1;

    private readonly Random _rng;
    private double? _spareGaussian;     // second value from each Box–Muller pair, cached

    /// <summary>Range-noise standard deviation in millimetres.</summary>
    public double SigmaMm { get; }

    /// <summary>
    /// Creates the noise model.
    /// </summary>
    /// <param name="sigmaMm">Standard deviation of the along-normal Gaussian (mm); must be ≥ 0. Zero disables noise.</param>
    /// <param name="seed">Optional RNG seed for reproducible perturbation.</param>
    /// <exception cref="ArgumentOutOfRangeException">σ is negative or NaN.</exception>
    public GaussianRangeNoise(double sigmaMm = DefaultSigmaMm, int? seed = null)
    {
        if (sigmaMm < 0 || double.IsNaN(sigmaMm))
            throw new ArgumentOutOfRangeException(nameof(sigmaMm), sigmaMm,
                "Noise sigma must be a non-negative number of millimetres.");

        SigmaMm = sigmaMm;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// <inheritdoc/>
    public SurfaceSample Apply(SurfaceSample sample)
    {
        if (SigmaMm == 0)
            return sample;

        double d = NextGaussian() * SigmaMm;
        Vector3D n = sample.Normal;
        Point3D p = sample.Position;
        var moved = new Point3D(p.X + d * n.X, p.Y + d * n.Y, p.Z + d * n.Z);
        return sample with { Position = moved };
    }

    /// <inheritdoc/>
    public IReadOnlyList<SurfaceSample> Apply(IReadOnlyList<SurfaceSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var result = new List<SurfaceSample>(samples.Count);
        foreach (SurfaceSample s in samples)
            result.Add(Apply(s));
        return result;
    }

    /// <summary>
    /// Standard-normal sample via the polar Box–Muller transform. Each call to the
    /// transform yields two independent values; the spare is cached for the next call.
    /// </summary>
    private double NextGaussian()
    {
        if (_spareGaussian is double spare)
        {
            _spareGaussian = null;
            return spare;
        }

        double u, v, s;
        do
        {
            u = 2.0 * _rng.NextDouble() - 1.0;
            v = 2.0 * _rng.NextDouble() - 1.0;
            s = u * u + v * v;
        }
        while (s >= 1.0 || s == 0.0);

        double factor = Math.Sqrt(-2.0 * Math.Log(s) / s);
        _spareGaussian = v * factor;
        return u * factor;
    }
}
