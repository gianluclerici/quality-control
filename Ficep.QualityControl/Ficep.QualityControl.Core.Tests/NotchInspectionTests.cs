using System.Collections.Generic;
using System.Linq;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Measurement;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;
using Xunit;
using Xunit.Abstractions;

namespace Ficep.QualityControl.Core.Tests;

public class NotchInspectionTests
{
    private readonly ITestOutputHelper _output;

    public NotchInspectionTests(ITestOutputHelper output) => _output = output;

    /// <summary>The demo machined piece: an IPE300 with an end web-notch (SCAI01) and a web hole (INTC01 Ø40).</summary>
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
    public void NominalsFromMacro_SCAI01_ReturnsAbrParameters()
    {
        MacroSpec notch = DemoPiece().Macros.First(m => m.MacroClassName == "SCAI01");
        NotchNominals nominal = NotchInspection.NominalsFromMacro(notch);

        Assert.Equal(80.0, nominal.LengthMm, 9);
        Assert.Equal(60.0, nominal.DepthMm, 9);
        Assert.Equal(10.0, nominal.RadiusMm, 9);
    }

    [Fact]
    public void Inspect_CleanNotch_MeasuresNominalParameters_InTolerance()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        var notchCutters = machined.Features.Where(f => f.Descriptor.Kind == FeatureKind.Notch).ToList();
        IReadOnlyList<Point3D> notchPoints = report.Features
            .Where(f => f.Feature.Kind == FeatureKind.Notch)
            .SelectMany(f => f.Report.Deviations.Select(d => d.Point))
            .ToList();

        NotchNominals nominals = NotchInspection.NominalsFromMacro(
            piece.Macros.First(m => m.MacroClassName == "SCAI01"));

        FeatureInspectionReport inspection = new NotchInspection()
            .Inspect(notchCutters, notchPoints, nominals);

        FeatureParameter length = inspection.Parameters.Single(p => p.Name == "Length");
        FeatureParameter depth = inspection.Parameters.Single(p => p.Name == "Depth");
        FeatureParameter radius = inspection.Parameters.Single(p => p.Name == "Radius");

        _output.WriteLine($"notch points={inspection.PointCount}");
        _output.WriteLine($"length  nominal={length.NominalMm:F1} measured={length.MeasuredMm:F3} dev={length.DeviationMm:F3}");
        _output.WriteLine($"depth   nominal={depth.NominalMm:F1} measured={depth.MeasuredMm:F3} dev={depth.DeviationMm:F3}");
        _output.WriteLine($"radius  nominal={radius.NominalMm:F1} measured={radius.MeasuredMm:F3} dev={radius.DeviationMm:F3}");

        Assert.True(System.Math.Abs(length.DeviationMm) < 0.5, $"length dev {length.DeviationMm:F3}");
        Assert.True(System.Math.Abs(depth.DeviationMm) < 0.5, $"depth dev {depth.DeviationMm:F3}");
        Assert.True(System.Math.Abs(radius.DeviationMm) < 0.5, $"radius dev {radius.DeviationMm:F3}");
        Assert.True(inspection.InTolerance);
    }

    [Fact]
    public void Inspect_CleanNotch_TightRadiusBand_Rejects()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);
        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        var notchCutters = machined.Features.Where(f => f.Descriptor.Kind == FeatureKind.Notch).ToList();
        IReadOnlyList<Point3D> notchPoints = report.Features
            .Where(f => f.Feature.Kind == FeatureKind.Notch)
            .SelectMany(f => f.Report.Deviations.Select(d => d.Point))
            .ToList();
        NotchNominals nominals = NotchInspection.NominalsFromMacro(
            piece.Macros.First(m => m.MacroClassName == "SCAI01"));

        var inspector = new NotchInspection();
        // The fillet measures ~0.05 mm off nominal (faceting); a 0.02 mm band rejects it, a 1.0 mm one accepts.
        FeatureInspectionReport tight = inspector.Inspect(
            notchCutters, notchPoints, nominals, new NotchTolerance(0.5, 0.5, 0.02));
        FeatureInspectionReport loose = inspector.Inspect(
            notchCutters, notchPoints, nominals, new NotchTolerance(0.5, 0.5, 1.0));

        Assert.False(tight.InTolerance);
        Assert.True(loose.InTolerance);
    }
}
