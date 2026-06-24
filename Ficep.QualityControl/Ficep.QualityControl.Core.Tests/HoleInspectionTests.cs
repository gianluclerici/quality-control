using System;
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

public class HoleInspectionTests
{
    private readonly ITestOutputHelper _output;

    public HoleInspectionTests(ITestOutputHelper output) => _output = output;

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
    public void NominalDiameterFromMacro_INTC01_ReturnsCParameter()
    {
        MacroSpec hole = DemoPiece().Macros.First(m => m.MacroClassName == "INTC01");
        Assert.Equal(40.0, HoleInspection.NominalDiameterFromMacro(hole), 9);
    }

    [Fact]
    public void CircleFit_RecoversKnownCircle()
    {
        const double cx = 12.5, cy = -7.25, r = 19.0;
        var u = new List<double>();
        var v = new List<double>();
        for (int k = 0; k < 24; k++)
        {
            double t = 2 * Math.PI * k / 24;
            u.Add(cx + r * Math.Cos(t));
            v.Add(cy + r * Math.Sin(t));
        }

        CircleFitResult fit = CircleFit.Fit(u, v);

        Assert.Equal(cx, fit.CenterU, 6);
        Assert.Equal(cy, fit.CenterV, 6);
        Assert.Equal(r, fit.Radius, 6);
    }

    [Fact]
    public void Inspect_CleanHole_MeasuresNominalDiameter_InTolerance()
    {
        var factory = new BeamFactory();
        PieceSpec piece = DemoPiece();
        MachinedBeam machined = factory.BuildMachined(piece);

        Mesh mesh = BrepTessellator.ToMesh(machined.Solid, factory.BrepTolerance);
        NominalSurface nominal = NominalSurface.FromMesh(mesh);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(mesh);

        var segmentation = FeatureSegmentation.FromCutters(machined.Features, onSurfaceToleranceMm: 0.5);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominal, segmentation);

        FeatureDeviation holeDev = report.Features.First(f => f.Feature.Kind == FeatureKind.Hole);
        FeatureCutter holeCutter = machined.Features.First(f => f.Descriptor.Kind == FeatureKind.Hole);
        IReadOnlyList<Point3D> holePoints = holeDev.Report.Deviations.Select(d => d.Point).ToList();

        double nominalDiameter = HoleInspection.NominalDiameterFromMacro(
            piece.Macros[holeDev.Feature.MacroIndex]);

        FeatureInspectionReport inspection =
            new HoleInspection().Inspect(holeCutter, holePoints, nominalDiameter, diameterToleranceMm: 0.5);

        FeatureParameter diameter = inspection.Parameters.Single(p => p.Name == "Diameter");
        _output.WriteLine($"hole points={holePoints.Count}  nominal Ø={nominalDiameter:F3}  measured Ø={diameter.MeasuredMm:F4}  dev={diameter.DeviationMm:F4}");

        Assert.Equal(40.0, nominalDiameter, 9);
        Assert.True(Math.Abs(diameter.DeviationMm) < 0.3,
            $"clean hole diameter should match nominal, deviation was {diameter.DeviationMm:F4} mm");
        Assert.True(diameter.InTolerance);
        Assert.True(inspection.InTolerance);
    }

    [Fact]
    public void Inspect_OversizedHole_RejectedByTightBand()
    {
        var factory = new BeamFactory();
        MachinedBeam machined = factory.BuildMachined(DemoPiece());
        FeatureCutter holeCutter = machined.Features.First(f => f.Descriptor.Kind == FeatureKind.Hole);

        // Synthesize a bore drilled 0.5 mm over radius (1.0 mm over diameter) about the real cutter axis.
        const double nominalDiameter = 40.0;
        const double delta = 0.5;
        IReadOnlyList<Point3D> oversized = SampleCylinderWall(holeCutter.Cutter, nominalDiameter / 2 + delta);

        var inspector = new HoleInspection();
        FeatureParameter tight = inspector
            .Inspect(holeCutter, oversized, nominalDiameter, diameterToleranceMm: 0.2)
            .Parameters.Single(p => p.Name == "Diameter");
        FeatureParameter loose = inspector
            .Inspect(holeCutter, oversized, nominalDiameter, diameterToleranceMm: 2.0)
            .Parameters.Single(p => p.Name == "Diameter");

        _output.WriteLine($"measured Ø={tight.MeasuredMm:F4}  dev={tight.DeviationMm:F4}");

        // Diameter reads nominal + 2δ; a tight band rejects it, a loose one accepts it.
        Assert.Equal(nominalDiameter + 2 * delta, tight.MeasuredMm, 3);
        Assert.False(tight.InTolerance);
        Assert.True(loose.InTolerance);
    }

    /// <summary>Generates points on a cylinder wall of the given radius about a cutter's axis.</summary>
    private static IReadOnlyList<Point3D> SampleCylinderWall(Brep cutter, double radius)
    {
        CylinderAxis axis = CutterAxis.FromCutter(cutter);
        Vec3 a = axis.Direction.Normalized();
        Vec3 seed = Math.Abs(a.X) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
        Vec3 u = Vec3.Cross(a, seed).Normalized();
        Vec3 v = Vec3.Cross(a, u).Normalized();

        var points = new List<Point3D>();
        for (int zi = 0; zi < 5; zi++)
        {
            double h = -10 + 5 * zi;
            for (int k = 0; k < 36; k++)
            {
                double t = 2 * Math.PI * k / 36;
                Vec3 p = axis.Point + a * h + (u * Math.Cos(t) + v * Math.Sin(t)) * radius;
                points.Add(new Point3D(p.X, p.Y, p.Z));
            }
        }
        return points;
    }
}
