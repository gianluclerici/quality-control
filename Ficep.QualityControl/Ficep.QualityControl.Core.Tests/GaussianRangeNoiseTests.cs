using devDept.Geometry;
using Ficep.QualityControl.Core.Noise;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class GaussianRangeNoiseTests
{
    private static SurfaceSample MakeSample() =>
        new(new Point3D(1.0, 2.0, 3.0), new Vector3D(0.0, 0.0, 1.0));

    [Fact]
    public void Apply_DisplacementAlongNormal_HasExpectedMeanAndStdDev()
    {
        const double sigma = 0.1;
        const int n = 20000;
        var noise = new GaussianRangeNoise(sigma, seed: 1234);

        SurfaceSample original = MakeSample();
        var displacements = new double[n];
        for (int i = 0; i < n; i++)
        {
            SurfaceSample perturbed = noise.Apply(original);
            // Normal is +Z, so the along-normal displacement is the Z delta.
            displacements[i] = perturbed.Position.Z - original.Position.Z;

            // X and Y must be untouched (displacement only along the normal).
            Assert.Equal(original.Position.X, perturbed.Position.X, 12);
            Assert.Equal(original.Position.Y, perturbed.Position.Y, 12);
        }

        double mean = displacements.Average();
        double variance = displacements.Select(d => (d - mean) * (d - mean)).Sum() / (n - 1);
        double std = Math.Sqrt(variance);

        // Mean ~ 0: a few standard errors. stderr = sigma/sqrt(n) ≈ 0.0007; allow 5 stderr.
        double meanTol = 5.0 * sigma / Math.Sqrt(n);
        Assert.True(Math.Abs(mean) < meanTol, $"mean {mean} exceeded tol {meanTol}");

        // Sample std within 5% of sigma.
        Assert.InRange(std, sigma * 0.95, sigma * 1.05);
    }

    [Fact]
    public void Apply_SigmaZero_ReturnsSampleUnchanged()
    {
        var noise = new GaussianRangeNoise(0.0, seed: 5);
        SurfaceSample original = MakeSample();
        SurfaceSample result = noise.Apply(original);

        Assert.Equal(original.Position.X, result.Position.X, 12);
        Assert.Equal(original.Position.Y, result.Position.Y, 12);
        Assert.Equal(original.Position.Z, result.Position.Z, 12);
    }

    [Fact]
    public void Apply_SameSeed_IsDeterministic()
    {
        SurfaceSample original = MakeSample();
        var a = new GaussianRangeNoise(0.1, seed: 777);
        var b = new GaussianRangeNoise(0.1, seed: 777);

        for (int i = 0; i < 100; i++)
        {
            SurfaceSample ra = a.Apply(original);
            SurfaceSample rb = b.Apply(original);
            Assert.Equal(ra.Position.X, rb.Position.X, 12);
            Assert.Equal(ra.Position.Y, rb.Position.Y, 12);
            Assert.Equal(ra.Position.Z, rb.Position.Z, 12);
        }
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    public void Constructor_RejectsNegativeSigma(double sigma)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GaussianRangeNoise(sigma, seed: 0));
    }

    [Fact]
    public void Apply_PreservesNormal()
    {
        var noise = new GaussianRangeNoise(0.1, seed: 3);
        SurfaceSample original = MakeSample();
        SurfaceSample result = noise.Apply(original);

        Assert.Equal(original.Normal.X, result.Normal.X, 12);
        Assert.Equal(original.Normal.Y, result.Normal.Y, 12);
        Assert.Equal(original.Normal.Z, result.Normal.Z, 12);
    }
}
