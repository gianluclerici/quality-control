using System.Linq;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Sampling;
using Xunit;
using Xunit.Abstractions;

namespace Ficep.QualityControl.Core.Tests;

/// <summary>
/// Step 5.5 — Phase A spike: estimating the beam's bounding datum planes (and hence length / width /
/// height) from the scan's base bucket, the references a feature-relative measurement is expressed against.
/// </summary>
public class BeamDatumsTests
{
    private readonly ITestOutputHelper _output;

    public BeamDatumsTests(ITestOutputHelper output) => _output = output;

    // Demo IPE300 (L=1000, SB=150, SA=300) with one notch + one hole, same as the rest of Step 5.
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

    private static List<SurfaceSample> BaseBucket(MachinedBeam machined, IReadOnlyList<SurfaceSample> cloud)
    {
        // The base bucket is exactly the unmachined-body points: those the segmentation assigns to no
        // feature cutter (Classify < 0). This mirrors how the PieceInspector will gather them in Phase C.
        var segmentation = FeatureSegmentation.FromCutters(machined.Features);
        return cloud.Where(s => segmentation.Classify(s.Position) < 0).ToList();
    }

    [Fact]
    public void Estimate_DemoCleanCloud_RecoversBeamDimensions()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 7).Sample(mesh);

        List<SurfaceSample> baseSamples = BaseBucket(machined, cloud);
        BeamDatumFrame datums = BeamDatums.Estimate(baseSamples);

        _output.WriteLine($"X[{datums.XMinMm:F3}, {datums.XMaxMm:F3}] -> length {datums.MeasuredLengthMm:F3}");
        _output.WriteLine($"Y[{datums.YMinMm:F3}, {datums.YMaxMm:F3}] -> height {datums.MeasuredHeightMm:F3}");
        _output.WriteLine($"Z[{datums.ZMinMm:F3}, {datums.ZMaxMm:F3}] -> width  {datums.MeasuredWidthMm:F3}");

        // Flat faces sampled directly → the spans recover the nominal IPE300 L=1000, SA=300, SB=150.
        Assert.Equal(1000.0, datums.MeasuredLengthMm, 1);
        Assert.Equal(300.0, datums.MeasuredHeightMm, 1);
        Assert.Equal(150.0, datums.MeasuredWidthMm, 1);
    }

    [Fact]
    public void Estimate_Dimensions_AreTranslationInvariant()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 7).Sample(mesh);
        List<SurfaceSample> baseSamples = BaseBucket(machined, cloud);

        // A pure translation leaves normals (hence face selection) and face-to-face distances unchanged:
        // this is the alignment-invariance the feature-relative measurement relies on.
        List<SurfaceSample> shifted = baseSamples
            .Select(s => new SurfaceSample(
                new Point3D(s.Position.X + 12.3, s.Position.Y - 4.5, s.Position.Z + 6.7), s.Normal))
            .ToList();

        BeamDatumFrame a = BeamDatums.Estimate(baseSamples);
        BeamDatumFrame b = BeamDatums.Estimate(shifted);

        Assert.Equal(a.MeasuredLengthMm, b.MeasuredLengthMm, 6);
        Assert.Equal(a.MeasuredHeightMm, b.MeasuredHeightMm, 6);
        Assert.Equal(a.MeasuredWidthMm, b.MeasuredWidthMm, 6);
    }
}
