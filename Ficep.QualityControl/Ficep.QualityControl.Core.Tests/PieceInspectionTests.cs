using System.Linq;
using devDept.Eyeshot.Entities;
using Ficep.QualityControl.Core.Features;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;
using Xunit;
using Xunit.Abstractions;

namespace Ficep.QualityControl.Core.Tests;

public class PieceInspectionTests
{
    private readonly ITestOutputHelper _output;

    public PieceInspectionTests(ITestOutputHelper output) => _output = output;

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
    public void Inspect_DemoPiece_MeasuresNotchAndHole_AllInTolerance()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        PieceInspectionReport report = new PieceInspector()
            .Inspect(machined, piece.Macros, cloud, factory.BrepTolerance,
                new InspectionOptions { OnSurfaceToleranceMm = 0.5 });

        foreach (string line in InspectionReportFormatter.Format(report))
            _output.WriteLine(line);

        // The demo yields exactly one notch (SCAI01) and one hole (INTC01) verdict.
        FeatureInspectionReport notch = report.Features.Single(f => f.Feature.Kind == FeatureKind.Notch);
        FeatureInspectionReport hole = report.Features.Single(f => f.Feature.Kind == FeatureKind.Hole);

        Assert.Equal(3, notch.Parameters.Count); // Length / Depth / Radius
        Assert.Single(hole.Parameters);          // Diameter

        Assert.True(report.Aligned);
        Assert.True(report.Alignment.Converged);
        Assert.True(report.InTolerance);
    }

    [Fact]
    public void DescribeNominal_DemoPiece_ListsNotchAndHole_WithNominalsAndNoMeasurement()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        IReadOnlyList<FeatureInspectionReport> nominal =
            new PieceInspector().DescribeNominal(machined, piece.Macros);

        // Same logical features the scan inspection produces: one notch (3 params) + one hole (1 param).
        FeatureInspectionReport notch = nominal.Single(f => f.Feature.Kind == FeatureKind.Notch);
        FeatureInspectionReport hole = nominal.Single(f => f.Feature.Kind == FeatureKind.Hole);

        Assert.Equal(3, notch.Parameters.Count);
        Assert.Equal(80, notch.Parameters.Single(p => p.Name == "Length").NominalMm);
        Assert.Equal(60, notch.Parameters.Single(p => p.Name == "Depth").NominalMm);
        Assert.Equal(10, notch.Parameters.Single(p => p.Name == "Radius").NominalMm);

        Assert.Single(hole.Parameters);
        Assert.Equal(40, hole.Parameters.Single().NominalMm);

        // Nominal-only: every measured value is NaN (no scan yet).
        Assert.All(nominal, f => Assert.All(f.Parameters, p => Assert.True(double.IsNaN(p.MeasuredMm))));
    }

    [Fact]
    public void DescribeNominal_ProducesSameFeatureIds_AsInspect()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        var inspector = new PieceInspector();
        IReadOnlyList<FeatureInspectionReport> nominal = inspector.DescribeNominal(machined, piece.Macros);
        PieceInspectionReport measured = inspector.Inspect(
            machined, piece.Macros, cloud, factory.BrepTolerance,
            new InspectionOptions { OnSurfaceToleranceMm = 0.5 });

        // The GUI matches nominal↔measured by FeatureDescriptor.Id, so the id sets must coincide.
        Assert.Equal(
            nominal.Select(f => f.Feature.Id).OrderBy(i => i),
            measured.Features.Select(f => f.Feature.Id).OrderBy(i => i));
    }

    [Fact]
    public void Inspect_WithIdentityTransform_MatchesNoAlignOnAlreadyRegisteredCloud()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        // The transform overload (GUI's "Misura" reusing an Allinea result) with identity must measure the
        // already-registered clean cloud just like the no-align path: same features, all in tolerance.
        PieceInspectionReport report = new PieceInspector().Inspect(
            machined, piece.Macros, cloud, factory.BrepTolerance,
            RigidTransform.Identity,
            new InspectionOptions { OnSurfaceToleranceMm = 0.5 });

        Assert.True(report.Aligned);
        Assert.Equal(2, report.Features.Count);
        Assert.True(report.InTolerance);
    }

    [Fact]
    public void Inspect_NoAlign_StillMeasuresOnAClean_AlreadyRegisteredCloud()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        // The sampled cloud sits in the nominal frame already, so skipping ICP must still pass.
        PieceInspectionReport report = new PieceInspector()
            .Inspect(machined, piece.Macros, cloud, factory.BrepTolerance,
                new InspectionOptions { Align = false, OnSurfaceToleranceMm = 0.5 });

        Assert.False(report.Aligned);
        Assert.Equal(2, report.Features.Count);
        Assert.True(report.InTolerance);
    }
}
