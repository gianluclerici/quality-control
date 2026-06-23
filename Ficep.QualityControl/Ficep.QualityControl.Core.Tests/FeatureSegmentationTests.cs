using System.Linq;
using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;
using Xunit.Abstractions;

namespace Ficep.QualityControl.Core.Tests;

public class FeatureSegmentationTests
{
    private readonly ITestOutputHelper _output;

    public FeatureSegmentationTests(ITestOutputHelper output) => _output = output;

    /// <summary>The demo machined piece: an IPE300 with an end web-notch (SCAI01) and a web hole (INTC01).</summary>
    private static PieceSpec DemoPiece() => new()
    {
        Beam = BeamSpec.Ipe300(1000.0),
        Macros = new[]
        {
            new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 10),
            new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 500).With("B", 150).With("C", 40),
        },
    };

    [Fact]
    public void BuildMachined_ExposesCuttersTaggedByMacro_WithHoleAndNotch()
    {
        var factory = new BeamFactory();
        MachinedBeam machined = factory.BuildMachined(DemoPiece());

        foreach (FeatureCutter f in machined.Features)
            _output.WriteLine($"  feature {f.Descriptor.Id}: {f.Descriptor.Label} ({f.Descriptor.Kind}, macro #{f.Descriptor.MacroIndex})");

        // SCAI01 expands into several cutter solids; INTC01 into one hole. We assert the invariants,
        // not the exact count, since the macro decomposition is the macro library's business.
        Assert.NotEmpty(machined.Features);
        Assert.Contains(machined.Features, f => f.Descriptor.Kind == FeatureKind.Hole && f.Descriptor.MacroClassName == "INTC01");
        Assert.Contains(machined.Features, f => f.Descriptor.Kind == FeatureKind.Notch && f.Descriptor.MacroClassName == "SCAI01");
        Assert.All(machined.Features, f => Assert.NotNull(f.Cutter));

        // Ids are unique and 1-based contiguous, so they index segmentation buckets cleanly.
        int[] ids = machined.Features.Select(f => f.Descriptor.Id).ToArray();
        Assert.Equal(Enumerable.Range(1, ids.Length), ids);
    }

    [Fact]
    public void Segmentation_RoutesHoleAndNotchPointsByKind_BaseIsMajority()
    {
        var factory = new BeamFactory();
        MachinedBeam machined = factory.BuildMachined(DemoPiece());

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.3, seed: 7).Sample(mesh);

        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        int holePoints = report.Features.Where(f => f.Feature.Kind == FeatureKind.Hole).Sum(f => f.Report.Deviations.Count);
        int notchPoints = report.Features.Where(f => f.Feature.Kind == FeatureKind.Notch).Sum(f => f.Report.Deviations.Count);
        _output.WriteLine($"cloud={cloud.Count}  base={report.Base.Deviations.Count}  hole={holePoints}  notch={notchPoints}");

        Assert.True(holePoints > 0, "the hole should capture scan points");
        Assert.True(notchPoints > 0, "the notch should capture scan points");

        // The unmachined body dominates the cloud.
        int featurePoints = report.Features.Sum(f => f.Report.Deviations.Count);
        Assert.True(report.Base.Deviations.Count > featurePoints,
            $"base ({report.Base.Deviations.Count}) should outnumber feature points ({featurePoints})");

        // The buckets partition the cloud exactly.
        Assert.Equal(cloud.Count, report.Base.Deviations.Count + featurePoints);
        Assert.Equal(cloud.Count, report.Overall.Deviations.Count);

        // On a clean (noise-free) cloud the points sit on the nominal, so the deviation reads ~0.
        Assert.True(report.Overall.Statistics.RmsMm < 1e-6,
            $"clean cloud RMS should be ~0, was {report.Overall.Statistics.RmsMm}");
    }

    [Fact]
    public void Segmentation_OnRawBlankWithNoFeatures_AssignsEverythingToBase()
    {
        var factory = new BeamFactory();
        var raw = new PieceSpec { Beam = BeamSpec.Ipe300(1000.0) }; // no macros
        MachinedBeam machined = factory.BuildMachined(raw);
        Assert.Empty(machined.Features);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.2, seed: 3).Sample(mesh);

        var segmentation = FeatureSegmentation.FromCutters(machined.Features);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        Assert.Empty(report.Features);
        Assert.Equal(cloud.Count, report.Base.Deviations.Count);
    }
}
