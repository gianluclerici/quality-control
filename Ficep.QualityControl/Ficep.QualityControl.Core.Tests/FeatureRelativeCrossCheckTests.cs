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

/// <summary>
/// End-to-end "prova del 9" for the Step 5.5 feature-relative measurement. Each test builds a <b>reference</b>
/// (design) 3D with a notch + a hole, then a <b>machined</b> 3D whose beam and macro parameters are nudged by
/// a known, small amount — the simulated real part. A point cloud is sampled from the machined solid and
/// measured against the <i>reference</i> geometry (its cutters give the known orientations; the datums are
/// estimated from the cloud). Because we know exactly how much each parameter was changed, the cross-check is:
/// <b>the measured value must land on the machined value, not the reference</b> — i.e. the measured deviation
/// reproduces the change we applied.
/// </summary>
public class FeatureRelativeCrossCheckTests
{
    private readonly ITestOutputHelper _output;

    public FeatureRelativeCrossCheckTests(ITestOutputHelper output) => _output = output;

    /// <summary>A beam plus the two macros under test (an SCAI01 cope and an INTC01 web hole).</summary>
    private sealed record Spec(BeamSpec Beam, MacroSpec Notch, MacroSpec Hole)
    {
        public PieceSpec ToPiece() => new() { Beam = Beam, Macros = new[] { Notch, Hole } };
    }

    private static MacroSpec NotchMacro(double a, double b, double c, double d, double e, double r) =>
        new MacroSpec { MacroClassName = "SCAI01", Side = "C", Vx = "I", Vy = "A" }
            .With("A", a).With("B", b).With("C", c).With("D", d).With("E", e).With("R", r);

    private static MacroSpec HoleMacro(double a, double b, double c) =>
        new MacroSpec { MacroClassName = "INTC01", Side = "C", Vx = "I", Vy = "A" }
            .With("A", a).With("B", b).With("C", c);

    [Fact]
    public void CrossCheck_NormalPiece_RecoversAppliedChanges()
    {
        var reference = new Spec(
            BeamSpec.Ipe300(1000.0),
            NotchMacro(80, 60, 40, 60, 40, 10),
            HoleMacro(500, 150, 40));
        var machined = new Spec(
            reference.Beam with { Length = 1000.5, SA = 300.4, SB = 150.3, TA = 7.4 },
            NotchMacro(80.3, 60.2, 40.3, 60.2, 39.7, 10.4),
            HoleMacro(500.4, 150.3, 40.5));

        RunAndVerify("NORMAL piece", reference, machined);
    }

    [Fact]
    public void CrossCheck_SharpCorner_R0_RecoversAppliedChanges()
    {
        var reference = new Spec(
            BeamSpec.Ipe300(1000.0),
            NotchMacro(80, 60, 40, 60, 40, 0),   // R = 0: sharp corner, no fillet
            HoleMacro(500, 150, 40));
        var machined = new Spec(
            reference.Beam with { Length = 999.6, SA = 299.7, SB = 149.8, TA = 6.9 },
            NotchMacro(80.2, 59.8, 40.2, 59.8, 40.3, 0),
            HoleMacro(499.7, 149.8, 39.6));

        RunAndVerify("SHARP corner (R=0)", reference, machined);
    }

    [Fact]
    public void CrossCheck_DifferentParameters_RecoversAppliedChanges()
    {
        // A larger cope with a larger fillet and different magnitudes (still a horizontal lower edge, D = B —
        // the cope shape the SCAI macro actually produces; a sloped lower edge, D != B, is a known limitation).
        var reference = new Spec(
            BeamSpec.Ipe300(1200.0),
            NotchMacro(100, 45, 35, 45, 55, 12),
            HoleMacro(600, 120, 50));
        var machined = new Spec(
            reference.Beam with { Length = 1200.4, SA = 300.5, SB = 150.4, TA = 7.5 },
            NotchMacro(100.4, 45.3, 35.2, 45.3, 55.4, 12.3),
            HoleMacro(600.5, 120.4, 50.4));

        RunAndVerify("DIFFERENT params (larger cope, D=B)", reference, machined);
    }

    /// <summary>
    /// Builds both solids, samples the cloud from the machined one, measures it against the reference geometry,
    /// prints the reference/machined/measured table and asserts every measured value lands on the machined one.
    /// </summary>
    private void RunAndVerify(string title, Spec reference, Spec machined)
    {
        var factory = new BeamFactory();
        MachinedBeam refBeam = factory.BuildMachined(reference.ToPiece());      // design → known cutters
        MachinedBeam machinedBeam = factory.BuildMachined(machined.ToPiece());  // the "real" part

        NominalSurface nominalRef = NominalSurface.FromMesh(BrepTessellator.ToMesh(refBeam.Solid, factory.BrepTolerance));
        Mesh machinedMesh = BrepTessellator.ToMesh(machinedBeam.Solid, factory.BrepTolerance);
        IReadOnlyList<SurfaceSample> cloud = new MeshSurfaceSampler(densityPerMm2: 0.5, seed: 11).Sample(machinedMesh);

        var segmentation = FeatureSegmentation.FromCutters(refBeam.Features, onSurfaceToleranceMm: 0.7);
        SegmentedDeviationReport report = new FeatureMeasurement().Measure(cloud, nominalRef, segmentation);

        BeamDatumFrame datums = BeamDatums.Estimate(
            cloud.Where(s => segmentation.Classify(s.Position) < 0).ToList());

        // --- notch ---
        var notchCutters = refBeam.Features.Where(f => f.Descriptor.Kind == FeatureKind.Notch).ToList();
        IReadOnlyList<Point3D> notchPoints = report.Features
            .Where(f => f.Feature.Kind == FeatureKind.Notch)
            .SelectMany(f => f.Report.Deviations.Select(d => d.Point))
            .ToList();
        NotchFullNominals notchNom = NotchInspection.FullNominalsFromMacro(reference.Notch);
        FeatureInspectionReport notch = new NotchInspection()
            .Inspect(notchCutters, notchPoints, notchNom, datums, reference.Notch.Vx, reference.Notch.Vy);

        // --- hole ---
        FeatureCutter holeCutter = refBeam.Features.First(f => f.Descriptor.Kind == FeatureKind.Hole);
        IReadOnlyList<Point3D> holePoints = report.Features
            .First(f => f.Feature.Kind == FeatureKind.Hole)
            .Report.Deviations.Select(d => d.Point)
            .ToList();
        HoleNominals holeNom = HoleInspection.NominalsFromMacro(reference.Hole, reference.Beam);
        FeatureInspectionReport hole = new HoleInspection()
            .Inspect(holeCutter, holePoints, holeNom, datums, reference.Hole.Vx, reference.Hole.Vy);

        // --- cross-check table + assertions ---
        double NM(string l) => machined.Notch.Parameters.GetValueOrDefault(l);
        double HM(string l) => machined.Hole.Parameters.GetValueOrDefault(l);
        double M(FeatureInspectionReport r, string n) => r.Parameters.Single(p => p.Name == n).MeasuredMm;

        _output.WriteLine($"=== {title} ===");
        _output.WriteLine($"{"param",-12}{"reference",12}{"machined",12}{"measured",12}{"err",10}");

        var failures = new List<string>();
        void Check(string name, double refVal, double machVal, double measVal, double eps)
        {
            double err = measVal - machVal;
            _output.WriteLine($"{name,-12}{refVal,12:F3}{machVal,12:F3}{measVal,12:F3}{err,10:F3}");
            if (Math.Abs(err) >= eps)
                failures.Add($"{name}: measured {measVal:F3} vs machined {machVal:F3} (err {err:+0.000;-0.000}, eps {eps})");
        }

        const double epsPos = 0.25, epsDatum = 0.20, epsRound = 0.30;

        // Datums (beam dimensions) re-derived from the cloud — must track the machined beam, not the reference.
        Check("len (L)", reference.Beam.Length, machined.Beam.Length, datums.MeasuredLengthMm, epsDatum);
        Check("hgt (SA)", reference.Beam.SA, machined.Beam.SA, datums.MeasuredHeightMm, epsDatum);
        Check("wid (SB)", reference.Beam.SB, machined.Beam.SB, datums.MeasuredWidthMm, epsDatum);

        // Notch — feature-relative, anchored to the near datums (so beam-dim changes do not leak in).
        Check("notch A", notchNom.LengthMm, NM("A"), M(notch, "A"), epsPos);
        Check("notch B", notchNom.DepthMm, NM("B"), M(notch, "B"), epsPos);
        Check("notch C", notchNom.TopRiseMm, NM("C"), M(notch, "C"), epsPos);
        Check("notch D", notchNom.ShoulderMm, NM("D"), M(notch, "D"), epsPos);
        Check("notch E", notchNom.SlantMm, NM("E"), M(notch, "E"), epsPos);
        Check("notch R", notchNom.RadiusMm, NM("R"), M(notch, "R"), epsRound);

        // Hole — diameter, depth (web thickness) and centre (x=length, y=width, z=height).
        Check("hole D", holeNom.DiameterMm, HM("C"), M(hole, "Diameter"), epsRound);
        Check("hole depth", holeNom.DepthMm, machined.Beam.TA, M(hole, "Depth"), epsPos);
        Check("hole Xc", holeNom.CenterXMm, HM("A"), M(hole, "CenterX"), epsPos);
        Check("hole Yc", holeNom.CenterYMm, machined.Beam.SB / 2.0, M(hole, "CenterY"), epsPos);
        Check("hole Zc", holeNom.CenterZMm, HM("B"), M(hole, "CenterZ"), epsPos);

        Assert.True(failures.Count == 0, $"{title}:\n  " + string.Join("\n  ", failures));
    }
}
