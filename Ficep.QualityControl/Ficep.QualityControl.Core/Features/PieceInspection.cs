using System.Linq;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.QualityControl.Core.Generation;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;
using Ficep.QualityControl.Core.Sampling;

namespace Ficep.QualityControl.Core.Features;

/// <summary>Knobs for a whole-piece inspection (<see cref="PieceInspector"/>).</summary>
public sealed record InspectionOptions
{
    /// <summary>Register the scan onto the nominal (point-to-plane ICP) before measuring.</summary>
    public bool Align { get; init; } = true;

    /// <summary>On-surface band (mm) used to route scan points to feature cutters (segmentation).</summary>
    public double OnSurfaceToleranceMm { get; init; } = 0.5;

    /// <summary>Symmetric diameter tolerance half-width (mm) for holes.</summary>
    public double HoleToleranceMm { get; init; } = HoleInspection.DefaultDiameterToleranceMm;

    /// <summary>Per-parameter tolerance for notches (length/depth/radius).</summary>
    public NotchTolerance NotchTolerance { get; init; } = NotchTolerance.Default;
}

/// <summary>
/// The outcome of inspecting a whole machined piece: the alignment result, the per-feature deviation
/// split, and one dimensional verdict (<see cref="FeatureInspectionReport"/>) per measured feature.
/// </summary>
public sealed record PieceInspectionReport
{
    /// <summary>Whether the scan was registered onto the nominal before measuring.</summary>
    public required bool Aligned { get; init; }

    /// <summary>The ICP result (identity/NaN RMS when <see cref="Aligned"/> is false).</summary>
    public required RegistrationResult Alignment { get; init; }

    /// <summary>Per-feature deviation report (segmentation + signed distances).</summary>
    public required SegmentedDeviationReport Deviation { get; init; }

    /// <summary>One dimensional verdict per measured feature (holes, notches).</summary>
    public required IReadOnlyList<FeatureInspectionReport> Features { get; init; }

    /// <summary>True when at least one feature was measured and every one is within tolerance.</summary>
    public bool InTolerance => Features.Count > 0 && Features.All(f => f.InTolerance);
}

/// <summary>
/// Step 5.4: the end-to-end quality-control pipeline for a whole piece, shared by the headless demo
/// (the Generator's <c>inspect</c> command) and the GUI. It ties together every Step-5 building block:
/// from the nominal <see cref="MachinedBeam"/> (re-derived from the macro list) it builds the queryable
/// surface, registers the scan (ICP), segments the cloud per feature, then runs the right dimensional
/// inspection for each feature kind — <see cref="HoleInspection"/> for holes, <see cref="NotchInspection"/>
/// for notches — and collects the verdicts.
/// <para>
/// A macro can expand into several cutters (the demo notch becomes four). Cutters are grouped by their
/// originating macro: each hole cutter is measured individually (its own bore), while a notch's cutters
/// are inspected together (one contour, the profile cutter carries the fillet). This grouping mirrors the
/// hand-written wiring the Step-5 tests use.
/// </para>
/// </summary>
public sealed class PieceInspector
{
    /// <summary>
    /// Inspects <paramref name="scan"/> against the nominal <paramref name="machined"/> piece, using
    /// <paramref name="macros"/> for the nominal parameter values and <paramref name="brepToleranceMm"/>
    /// to tessellate the nominal surface.
    /// </summary>
    /// <param name="machined">Nominal solid + feature cutters (from <see cref="BeamFactory.BuildMachined"/>).</param>
    /// <param name="macros">The piece's macro list (nominal parameters), indexed by <see cref="FeatureDescriptor.MacroIndex"/>.</param>
    /// <param name="scan">The scanned point cloud.</param>
    /// <param name="brepToleranceMm">Chord tolerance (mm) for tessellating the nominal solid.</param>
    /// <param name="options">Alignment / tolerance knobs; defaults applied when null.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public PieceInspectionReport Inspect(
        MachinedBeam machined,
        IReadOnlyList<MacroSpec> macros,
        IReadOnlyList<SurfaceSample> scan,
        double brepToleranceMm,
        InspectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(machined);
        ArgumentNullException.ThrowIfNull(macros);
        ArgumentNullException.ThrowIfNull(scan);

        InspectionOptions opt = options ?? new InspectionOptions();
        NominalSurface nominal = BuildNominal(machined, brepToleranceMm);

        RegistrationResult alignment = opt.Align
            ? new IcpRegistration().Register(scan, nominal)
            : new RegistrationResult(RigidTransform.Identity, 0, double.NaN, false);

        return Build(machined, macros, scan, nominal, alignment, aligned: opt.Align, opt);
    }

    /// <summary>
    /// Inspects with a <paramref name="alignment"/> the caller already computed (e.g. the GUI's
    /// <c>Allinea</c> step), skipping ICP so the deviation map and the per-feature measurement share the
    /// exact same transform. The reported <see cref="PieceInspectionReport.Aligned"/> is true and the
    /// <see cref="PieceInspectionReport.Alignment"/> wraps <paramref name="alignment"/> (no RMS/iterations).
    /// </summary>
    public PieceInspectionReport Inspect(
        MachinedBeam machined,
        IReadOnlyList<MacroSpec> macros,
        IReadOnlyList<SurfaceSample> scan,
        double brepToleranceMm,
        RigidTransform alignment,
        InspectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(machined);
        ArgumentNullException.ThrowIfNull(macros);
        ArgumentNullException.ThrowIfNull(scan);

        InspectionOptions opt = options ?? new InspectionOptions();
        NominalSurface nominal = BuildNominal(machined, brepToleranceMm);
        var result = new RegistrationResult(alignment, 0, double.NaN, false);

        return Build(machined, macros, scan, nominal, result, aligned: true, opt);
    }

    /// <summary>
    /// Describes the nominal parameters of every measurable feature <b>without a scan</b> — the design
    /// values the GUI shows the moment the macro list is loaded, before any measurement. Each returned
    /// <see cref="FeatureInspectionReport"/> carries the same <see cref="FeatureDescriptor"/> a later
    /// <see cref="Inspect(MachinedBeam, IReadOnlyList{MacroSpec}, IReadOnlyList{SurfaceSample}, double, InspectionOptions?)"/>
    /// would produce (so the two can be matched by <see cref="FeatureDescriptor.Id"/>), with each
    /// parameter's <see cref="FeatureParameter.MeasuredMm"/> left as <see cref="double.NaN"/>.
    /// </summary>
    public IReadOnlyList<FeatureInspectionReport> DescribeNominal(
        MachinedBeam machined,
        IReadOnlyList<MacroSpec> macros,
        InspectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(machined);
        ArgumentNullException.ThrowIfNull(macros);

        InspectionOptions opt = options ?? new InspectionOptions();
        IReadOnlyList<FeatureCutter> cutters = machined.Features;

        var results = new List<FeatureInspectionReport>();
        foreach ((int mi, FeatureKind kind, List<int> idxs) in GroupByMacro(cutters))
        {
            if (mi < 0 || mi >= macros.Count)
                continue;
            MacroSpec macro = macros[mi];

            if (kind == FeatureKind.Hole)
            {
                double diameter = HoleInspection.NominalDiameterFromMacro(macro);
                foreach (int fi in idxs)
                    results.Add(NominalReport(cutters[fi].Descriptor, new[]
                    {
                        Nominal("Diameter", diameter, opt.HoleToleranceMm),
                    }));
            }
            else if (kind == FeatureKind.Notch)
            {
                NotchNominals nom = NotchInspection.NominalsFromMacro(macro);
                FeatureCutter profile = ProfileCutter(idxs.Select(fi => cutters[fi]).ToList());
                results.Add(NominalReport(profile.Descriptor, new[]
                {
                    Nominal("Length", nom.LengthMm, opt.NotchTolerance.LengthMm),
                    Nominal("Depth", nom.DepthMm, opt.NotchTolerance.DepthMm),
                    Nominal("Radius", nom.RadiusMm, opt.NotchTolerance.RadiusMm),
                }));
            }
        }
        return results;
    }

    /// <summary>Builds the queryable nominal surface from the machined solid (single tessellation).</summary>
    private static NominalSurface BuildNominal(MachinedBeam machined, double brepToleranceMm) =>
        NominalSurface.FromMesh(BrepTessellator.ToMesh(machined.Solid, brepToleranceMm));

    /// <summary>Shared back-end of both <c>Inspect</c> overloads: segment, measure, collect verdicts.</summary>
    private static PieceInspectionReport Build(
        MachinedBeam machined,
        IReadOnlyList<MacroSpec> macros,
        IReadOnlyList<SurfaceSample> scan,
        NominalSurface nominal,
        RegistrationResult alignment,
        bool aligned,
        InspectionOptions opt)
    {
        var segmentation = FeatureSegmentation.FromCutters(machined.Features, opt.OnSurfaceToleranceMm);
        SegmentedDeviationReport deviation =
            new FeatureMeasurement().Measure(scan, nominal, segmentation, alignment.Transform);

        var reports = new List<FeatureInspectionReport>();
        foreach (FeatureInspectionReport r in InspectFeatures(machined, macros, deviation, opt))
            reports.Add(r);

        return new PieceInspectionReport
        {
            Aligned = aligned,
            Alignment = alignment,
            Deviation = deviation,
            Features = reports,
        };
    }

    /// <summary>A nominal-only parameter (measured value left as NaN, verdict not yet meaningful).</summary>
    private static FeatureParameter Nominal(string name, double nominalMm, double toleranceMm) =>
        new(name, nominalMm, double.NaN, Math.Abs(toleranceMm), false);

    private static FeatureInspectionReport NominalReport(
        FeatureDescriptor feature, IReadOnlyList<FeatureParameter> parameters) =>
        new() { Feature = feature, Parameters = parameters, PointCount = 0 };

    /// <summary>The notch's contour-extrusion cutter (the one carrying the fillet); falls back to the first.</summary>
    private static FeatureCutter ProfileCutter(IReadOnlyList<FeatureCutter> notchCutters)
    {
        foreach (FeatureCutter fc in notchCutters)
        {
            if (ExtrudedProfile.HasFillet(fc.Cutter))
                return fc;
        }
        return notchCutters[0];
    }

    /// <summary>
    /// Groups cutter indices by their originating macro, preserving first-seen order, and yields the
    /// macro index, the group's feature kind (from its first cutter) and the cutter indices.
    /// </summary>
    private static IEnumerable<(int MacroIndex, FeatureKind Kind, List<int> CutterIndices)> GroupByMacro(
        IReadOnlyList<FeatureCutter> cutters)
    {
        var order = new List<int>();
        var byMacro = new Dictionary<int, List<int>>();
        for (int fi = 0; fi < cutters.Count; fi++)
        {
            int mi = cutters[fi].Descriptor.MacroIndex;
            if (!byMacro.TryGetValue(mi, out List<int>? list))
            {
                list = new List<int>();
                byMacro.Add(mi, list);
                order.Add(mi);
            }
            list.Add(fi);
        }

        foreach (int mi in order)
            yield return (mi, cutters[byMacro[mi][0]].Descriptor.Kind, byMacro[mi]);
    }

    /// <summary>
    /// Inspects each feature: cutters are grouped by their originating macro (a macro can yield several
    /// cutters). Holes are measured one bore at a time; a notch's cutters are inspected together.
    /// <see cref="SegmentedDeviationReport.Features"/> is parallel to <c>machined.Features</c> by index,
    /// so cutter <c>fi</c>'s scan points are <c>deviation.Features[fi]</c>.
    /// </summary>
    private static IEnumerable<FeatureInspectionReport> InspectFeatures(
        MachinedBeam machined,
        IReadOnlyList<MacroSpec> macros,
        SegmentedDeviationReport deviation,
        InspectionOptions opt)
    {
        IReadOnlyList<FeatureCutter> cutters = machined.Features;

        var results = new List<FeatureInspectionReport>();
        foreach ((int mi, FeatureKind kind, List<int> idxs) in GroupByMacro(cutters))
        {
            if (mi < 0 || mi >= macros.Count)
                continue;

            MacroSpec macro = macros[mi];

            if (kind == FeatureKind.Hole)
            {
                double diameter = HoleInspection.NominalDiameterFromMacro(macro);
                foreach (int fi in idxs)
                {
                    List<Point3D> pts = PointsOf(deviation, fi);
                    if (pts.Count < 3)
                        continue;
                    results.Add(new HoleInspection().Inspect(cutters[fi], pts, diameter, opt.HoleToleranceMm));
                }
            }
            else if (kind == FeatureKind.Notch)
            {
                var notchCutters = idxs.Select(fi => cutters[fi]).ToList();
                List<Point3D> pts = idxs.SelectMany(fi => PointsOf(deviation, fi)).ToList();
                if (pts.Count < 3)
                    continue;
                NotchNominals nominals = NotchInspection.NominalsFromMacro(macro);
                results.Add(new NotchInspection().Inspect(notchCutters, pts, nominals, opt.NotchTolerance));
            }
        }
        return results;
    }

    /// <summary>The scan points the segmentation routed to cutter <paramref name="featureIndex"/>.</summary>
    private static List<Point3D> PointsOf(SegmentedDeviationReport deviation, int featureIndex) =>
        deviation.Features[featureIndex].Report.Deviations.Select(d => d.Point).ToList();
}
