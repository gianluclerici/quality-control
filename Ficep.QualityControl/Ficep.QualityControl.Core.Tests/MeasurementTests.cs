using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Measurement;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Tests;

public class MeasurementTests
{
    private static (NominalSurface surface, IReadOnlyList<SurfaceSample> cloud) BuildBeam()
    {
        var factory = new BeamFactory();
        Brep brep = factory.BuildRaw(BeamSpec.Ipe300(1000.0));
        Mesh mesh = BrepTessellator.ToMesh(brep, factory.BrepTolerance);
        NominalSurface surface = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.05, seed: 2024).Sample(mesh);
        return (surface, cloud);
    }

    [Fact]
    public void PerfectScan_OnSurface_IsConformAndNearZero()
    {
        var (surface, cloud) = BuildBeam();
        var band = ToleranceBand.Symmetric(0.1);

        DeviationReport report = new DeviationMeasurement().Measure(cloud, surface, alignment: null, tolerance: band);

        Assert.Equal(cloud.Count, report.Deviations.Count);
        Assert.True(report.Statistics.RmsMm < 1e-6, $"on-surface RMS should be ~0, was {report.Statistics.RmsMm}");
        Assert.True(report.IsConform, "a perfect on-surface scan must be conform");
        Assert.Equal(0, report.OutOfToleranceCount);
        Assert.Equal(1.0, report.ConformanceRatio, 9);
    }

    [Fact]
    public void OffsetAlongNormal_GivesPositiveSignedDeviationEqualToOffset()
    {
        var (surface, cloud) = BuildBeam();
        const double offset = 0.5;
        var moved = new List<SurfaceSample>(cloud.Count);
        foreach (SurfaceSample s in cloud)
        {
            var p = new Point3D(
                s.Position.X + s.Normal.X * offset,
                s.Position.Y + s.Normal.Y * offset,
                s.Position.Z + s.Normal.Z * offset);
            moved.Add(new SurfaceSample(p, s.Normal));
        }

        DeviationReport report = new DeviationMeasurement().Measure(moved, surface);

        // Excess material along the outward normal → positive signed deviation, mean ≈ +offset.
        Assert.True(Math.Abs(report.Statistics.MeanMm - offset) < 0.02,
            $"mean signed deviation should be ≈ {offset}, was {report.Statistics.MeanMm:F4}");
        Assert.True(report.Statistics.MinMm > 0, $"all deviations should be positive, min was {report.Statistics.MinMm:F4}");
        Assert.True(Math.Abs(report.Statistics.RmsMm - offset) < 0.05);
    }

    [Fact]
    public void OffsetInward_GivesNegativeSignedDeviation()
    {
        var (surface, cloud) = BuildBeam();
        const double offset = 0.4;
        var moved = new List<SurfaceSample>(cloud.Count);
        foreach (SurfaceSample s in cloud)
        {
            var p = new Point3D(
                s.Position.X - s.Normal.X * offset,
                s.Position.Y - s.Normal.Y * offset,
                s.Position.Z - s.Normal.Z * offset);
            moved.Add(new SurfaceSample(p, s.Normal));
        }

        DeviationReport report = new DeviationMeasurement().Measure(moved, surface);

        Assert.True(report.Statistics.MeanMm < 0, $"inward offset should read negative, mean was {report.Statistics.MeanMm:F4}");
        Assert.True(Math.Abs(report.Statistics.MeanMm + offset) < 0.02);
    }

    [Fact]
    public void Tolerance_RejectsAnOutOfBandOffset()
    {
        var (surface, cloud) = BuildBeam();
        const double offset = 0.5;
        var moved = new List<SurfaceSample>(cloud.Count);
        foreach (SurfaceSample s in cloud)
        {
            var p = new Point3D(
                s.Position.X + s.Normal.X * offset,
                s.Position.Y + s.Normal.Y * offset,
                s.Position.Z + s.Normal.Z * offset);
            moved.Add(new SurfaceSample(p, s.Normal));
        }

        // A 0.1 mm band cannot accept a uniform 0.5 mm excess.
        DeviationReport report = new DeviationMeasurement().Measure(moved, surface, tolerance: ToleranceBand.Symmetric(0.1));

        Assert.False(report.IsConform);
        Assert.Equal(moved.Count, report.OutOfToleranceCount);
        Assert.Equal(0.0, report.ConformanceRatio, 9);
    }

    [Fact]
    public void Alignment_IsAppliedBeforeMeasuring()
    {
        var (surface, cloud) = BuildBeam();

        // Disturb the cloud by a known rigid transform, then hand its inverse-recovering ICP transform
        // to Measure: the measured deviations must collapse back to ~0, proving the alignment is applied.
        var misalign = RigidTransform.FromRotationVector(0.03, -0.02, 0.015, 3.0, -2.0, 1.5);
        var misaligned = new List<SurfaceSample>(cloud.Count);
        foreach (SurfaceSample s in cloud)
            misaligned.Add(new SurfaceSample(misalign.Apply(s.Position), s.Normal));

        RegistrationResult reg = new IcpRegistration().Register(misaligned, surface);

        DeviationReport before = new DeviationMeasurement().Measure(misaligned, surface);
        DeviationReport after = new DeviationMeasurement().Measure(misaligned, surface, alignment: reg.Transform);

        Assert.True(before.Statistics.RmsMm > 1.0, $"pre-alignment RMS should be large, was {before.Statistics.RmsMm:F3}");
        Assert.True(after.Statistics.RmsMm < 0.05, $"post-alignment RMS should be ~0, was {after.Statistics.RmsMm:F4}");
    }

    [Fact]
    public void Statistics_Compute_MatchesHandValues()
    {
        var data = new[] { -2.0, -1.0, 0.0, 1.0, 2.0 };
        DeviationStatistics s = DeviationStatistics.Compute(data);

        Assert.Equal(5, s.Count);
        Assert.Equal(-2.0, s.MinMm, 9);
        Assert.Equal(2.0, s.MaxMm, 9);
        Assert.Equal(0.0, s.MeanMm, 9);
        Assert.Equal(Math.Sqrt(2.0), s.RmsMm, 9);   // sqrt((4+1+0+1+4)/5) = sqrt(2)
        Assert.Equal(1.2, s.MeanAbsMm, 9);          // (2+1+0+1+2)/5
        Assert.Equal(2.0, s.MaxAbsMm, 9);
    }
}
