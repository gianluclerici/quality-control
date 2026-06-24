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

    /// <summary>The demo cope with a sharp corner (R=0): no fillet face, so the contour cutter is keyed on its wall count.</summary>
    private static PieceSpec SharpDemoPiece() => new()
    {
        Beam = BeamSpec.Ipe300(1000.0),
        Macros = new[]
        {
            new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
                .With("A", 80).With("B", 60).With("C", 40).With("D", 60).With("E", 40).With("R", 0),
        },
    };

    [Fact]
    public void Inspect_CleanNotch_MeasuresAllSixParameters_FeatureRelative()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        // Datums from the base bucket (Phase A) — exactly how the PieceInspector will gather them in Phase C.
        BeamDatumFrame datums = BeamDatums.Estimate(
            cloud.Where(s => segmentation.Classify(s.Position) < 0).ToList());

        var notchCutters = machined.Features.Where(f => f.Descriptor.Kind == FeatureKind.Notch).ToList();
        IReadOnlyList<Point3D> notchPoints = report.Features
            .Where(f => f.Feature.Kind == FeatureKind.Notch)
            .SelectMany(f => f.Report.Deviations.Select(d => d.Point))
            .ToList();

        MacroSpec macro = piece.Macros.First(m => m.MacroClassName == "SCAI01");
        NotchFullNominals nominals = NotchInspection.FullNominalsFromMacro(macro);

        FeatureInspectionReport inspection = new NotchInspection()
            .Inspect(notchCutters, notchPoints, nominals, datums, macro.Vx, macro.Vy);

        double Measured(string nm) => inspection.Parameters.Single(p => p.Name == nm).MeasuredMm;
        _output.WriteLine($"notch points={inspection.PointCount}");
        _output.WriteLine($"A={Measured("A"):F3} B={Measured("B"):F3} C={Measured("C"):F3} " +
            $"D={Measured("D"):F3} E={Measured("E"):F3} R={Measured("R"):F3}");

        Assert.Equal(80.0, Measured("A"), 1);
        Assert.Equal(60.0, Measured("B"), 1);
        Assert.Equal(60.0, Measured("D"), 1);
        // C and E come from the inclined top edge (a TLS line fit), so they carry ~0.1 mm faceting noise — judge
        // them by tolerance, not to 0.05 mm. Likewise R carries its intrinsic faceting bias (see TightRadiusBand).
        Assert.True(System.Math.Abs(Measured("C") - 40.0) < 0.25, $"C={Measured("C"):F3}");
        Assert.True(System.Math.Abs(Measured("E") - 40.0) < 0.25, $"E={Measured("E"):F3}");
        Assert.True(System.Math.Abs(inspection.Parameters.Single(p => p.Name == "R").DeviationMm) < 0.5);
        Assert.True(inspection.InTolerance);
    }

    [Fact]
    public void Inspect_AllSixParameters_AreTranslationInvariant()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);
        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        var baseSamples = cloud.Where(s => segmentation.Classify(s.Position) < 0).ToList();
        var notchCutters = machined.Features.Where(f => f.Descriptor.Kind == FeatureKind.Notch).ToList();
        IReadOnlyList<Point3D> notchPoints = report.Features
            .Where(f => f.Feature.Kind == FeatureKind.Notch)
            .SelectMany(f => f.Report.Deviations.Select(d => d.Point))
            .ToList();
        MacroSpec macro = piece.Macros.First(m => m.MacroClassName == "SCAI01");
        NotchFullNominals nominals = NotchInspection.FullNominalsFromMacro(macro);

        // A small residual mis-registration (< the wall band) shifts the cope points AND the datum frame
        // together: each feature→datum length/height is preserved. This is Step 5.5's alignment-invariance.
        var shift = new Vec3(0.4, -0.3, 0.2);
        var shiftedPoints = notchPoints
            .Select(p => new Point3D(p.X + shift.X, p.Y + shift.Y, p.Z + shift.Z)).ToList();
        var shiftedBase = baseSamples
            .Select(s => new SurfaceSample(
                new Point3D(s.Position.X + shift.X, s.Position.Y + shift.Y, s.Position.Z + shift.Z), s.Normal))
            .ToList();

        var inspector = new NotchInspection();
        FeatureInspectionReport a = inspector.Inspect(
            notchCutters, notchPoints, nominals, BeamDatums.Estimate(baseSamples), macro.Vx, macro.Vy);
        FeatureInspectionReport b = inspector.Inspect(
            notchCutters, shiftedPoints, nominals, BeamDatums.Estimate(shiftedBase), macro.Vx, macro.Vy);

        // The shift (0.5 mm) cancels to well under the measurement noise: a sub-0.02 mm residual against a
        // 0.5 mm registration error is the alignment-invariance Step 5.5 buys (TLS membership at the band edges
        // keeps it from being bit-exact).
        foreach (string name in new[] { "A", "B", "C", "D", "E" })
        {
            double va = a.Parameters.Single(p => p.Name == name).MeasuredMm;
            double vb = b.Parameters.Single(p => p.Name == name).MeasuredMm;
            _output.WriteLine($"{name}: {va:F4} vs {vb:F4}  diff {vb - va:F4}");
            Assert.True(System.Math.Abs(va - vb) < 0.02, $"{name}: {va:F4} vs {vb:F4}");
        }
    }

    [Fact]
    public void Inspect_SharpCorner_NoFillet_ReportsRadiusZero_AndMeasuresPositions()
    {
        var factory = new BeamFactory();
        PieceSpec piece = SharpDemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        BeamDatumFrame datums = BeamDatums.Estimate(
            cloud.Where(s => segmentation.Classify(s.Position) < 0).ToList());

        var notchCutters = machined.Features.Where(f => f.Descriptor.Kind == FeatureKind.Notch).ToList();
        IReadOnlyList<Point3D> notchPoints = report.Features
            .Where(f => f.Feature.Kind == FeatureKind.Notch)
            .SelectMany(f => f.Report.Deviations.Select(d => d.Point))
            .ToList();

        MacroSpec macro = piece.Macros.First(m => m.MacroClassName == "SCAI01");
        NotchFullNominals nominals = NotchInspection.FullNominalsFromMacro(macro);
        Assert.Equal(0.0, nominals.RadiusMm, 9); // R=0 is decided from the input, not the geometry

        FeatureInspectionReport inspection = new NotchInspection()
            .Inspect(notchCutters, notchPoints, nominals, datums, macro.Vx, macro.Vy);

        double Measured(string nm) => inspection.Parameters.Single(p => p.Name == nm).MeasuredMm;
        FeatureParameter radius = inspection.Parameters.Single(p => p.Name == "R");

        Assert.Equal(0.0, radius.MeasuredMm, 9); // absent element: reported 0, not fitted
        Assert.True(radius.InTolerance);
        Assert.Equal(80.0, Measured("A"), 1);
        Assert.Equal(60.0, Measured("B"), 1);
        Assert.Equal(60.0, Measured("D"), 1);
        // Inclined-top-edge params: judge by tolerance (TLS fit, ~0.1 mm faceting noise).
        Assert.True(System.Math.Abs(Measured("C") - 40.0) < 0.25, $"C={Measured("C"):F3}");
        Assert.True(System.Math.Abs(Measured("E") - 40.0) < 0.25, $"E={Measured("E"):F3}");
        Assert.True(inspection.InTolerance);
    }
}
