using devDept.Geometry;
using Ficep.QualityControl.Core.Model;
using Ficep.QualityControl.Core.Registration;

namespace Ficep.QualityControl.Core.Features;

/// <summary>
/// The full nominal geometry of a bore (mm): diameter, through depth and the centre expressed in the
/// beam corner-datum frame (<see cref="BeamDatumFrame"/>). See <see cref="HoleInspection.NominalsFromMacro"/>.
/// </summary>
/// <param name="DiameterMm">Nominal bore diameter (macro <c>C</c>/<c>F</c>).</param>
/// <param name="DepthMm">Nominal bore depth along its axis (through-web bore: the web thickness).</param>
/// <param name="CenterXMm">Nominal centre length from the Vx end cap (macro <c>A</c>).</param>
/// <param name="CenterYMm">Nominal centre width from the flange-side edge (web bore: half the flange width).</param>
/// <param name="CenterZMm">Nominal centre height above the bottom flange (macro <c>B</c>).</param>
public readonly record struct HoleNominals(
    double DiameterMm, double DepthMm, double CenterXMm, double CenterYMm, double CenterZMm);

/// <summary>Symmetric tolerance half-widths (mm) for the hole parameters.</summary>
/// <param name="DiameterMm">Diameter band.</param>
/// <param name="DepthMm">Depth band.</param>
/// <param name="CenterMm">Band applied to each of the three centre coordinates.</param>
public readonly record struct HoleTolerance(double DiameterMm, double DepthMm, double CenterMm)
{
    /// <summary>A uniform ±0.5 mm band on diameter, depth and each centre coordinate.</summary>
    public static HoleTolerance Default => new(0.5, 0.5, 0.5);
}

/// <summary>
/// Step 5.2: the first dimensional verdict. For a <see cref="FeatureKind.Hole"/> feature it measures the
/// bore diameter from the scan and judges it against the nominal.
/// <para>
/// The cylinder axis is taken from the cutter geometry (<see cref="CutterAxis"/>), so it is known exactly
/// and the only unknowns left are the in-plane centre and radius: the segmented hole points are projected
/// onto the plane perpendicular to the axis and fitted with an algebraic 2D circle
/// (<see cref="CircleFit"/>). The nominal diameter comes from the macro parameters (the value is part of
/// the inspection input — e.g. INTC01's <c>C</c> is the bore diameter), see
/// <see cref="NominalDiameterFromMacro"/>.
/// </para>
/// </summary>
public sealed class HoleInspection
{
    /// <summary>Default diameter tolerance half-width (mm).</summary>
    public const double DefaultDiameterToleranceMm = 0.5;

    /// <summary>
    /// Measures the bore diameter of <paramref name="hole"/> from <paramref name="scanPoints"/> (the
    /// aligned points the segmentation routed to this hole) and judges it against
    /// <paramref name="nominalDiameterMm"/> within ±<paramref name="diameterToleranceMm"/>.
    /// </summary>
    /// <param name="hole">The hole feature, whose cutter provides the (known) cylinder axis.</param>
    /// <param name="scanPoints">Aligned scan points belonging to the hole (≥ 3).</param>
    /// <param name="nominalDiameterMm">Nominal bore diameter (mm); see <see cref="NominalDiameterFromMacro"/>.</param>
    /// <param name="diameterToleranceMm">Symmetric tolerance half-width (mm).</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nominalDiameterMm"/> is not positive.</exception>
    /// <exception cref="ArgumentException">Fewer than 3 scan points.</exception>
    /// <exception cref="InvalidOperationException">Axis/circle could not be derived from the inputs.</exception>
    public FeatureInspectionReport Inspect(
        FeatureCutter hole,
        IReadOnlyList<Point3D> scanPoints,
        double nominalDiameterMm,
        double diameterToleranceMm = DefaultDiameterToleranceMm)
    {
        ArgumentNullException.ThrowIfNull(hole);
        ArgumentNullException.ThrowIfNull(scanPoints);
        if (nominalDiameterMm <= 0 || double.IsNaN(nominalDiameterMm))
            throw new ArgumentOutOfRangeException(nameof(nominalDiameterMm), nominalDiameterMm,
                "Nominal diameter must be a positive length in millimetres.");
        if (scanPoints.Count < 3)
            throw new ArgumentException("A hole diameter fit needs at least 3 scan points.", nameof(scanPoints));

        CylinderAxis axis = CutterAxis.FromCutter(hole.Cutter);
        (Vec3 uHat, Vec3 vHat) = OrthonormalBasis(axis.Direction);

        int n = scanPoints.Count;
        var u = new double[n];
        var v = new double[n];
        for (int i = 0; i < n; i++)
        {
            Point3D p = scanPoints[i];
            Vec3 w = new Vec3(p.X, p.Y, p.Z) - axis.Point;
            u[i] = Vec3.Dot(w, uHat);
            v[i] = Vec3.Dot(w, vHat);
        }

        CircleFitResult fit = CircleFit.Fit(u, v);
        double measuredDiameterMm = 2.0 * fit.Radius;

        var diameter = FeatureParameter.Judge(
            "Diameter", nominalDiameterMm, measuredDiameterMm, diameterToleranceMm);

        return new FeatureInspectionReport
        {
            Feature = hole.Descriptor,
            Parameters = new[] { diameter },
            PointCount = n,
        };
    }

    /// <summary>
    /// Step 5.5: the <b>complete</b> bore verdict — diameter, through depth and centre (x, y, z) — with the
    /// centre expressed <b>feature-relative</b> in the beam corner-datum frame (<paramref name="datums"/>),
    /// so it is invariant to residual registration error (see <see cref="BeamDatumFrame"/>).
    /// <para>
    /// The cylinder axis is known from the cutter (<see cref="CutterAxis"/>): the in-plane centre and radius
    /// come from a 2D circle fit (<see cref="CircleFit"/>), while the depth is the axial span of the bore
    /// points and the centre's axial coordinate is the mid-span (unbiased to symmetric end truncation). The
    /// world centre is then dropped onto the datum planes via <paramref name="vx"/> (end cap) and
    /// <paramref name="side"/> (flange edge).
    /// </para>
    /// </summary>
    /// <param name="hole">The hole feature, whose cutter provides the (known) cylinder axis.</param>
    /// <param name="scanPoints">Aligned scan points belonging to the hole (≥ 3).</param>
    /// <param name="nominal">Nominal diameter/depth/centre; see <see cref="NominalsFromMacro"/>.</param>
    /// <param name="datums">The beam datum frame the centre is measured against (Phase A).</param>
    /// <param name="vx">Length-end the centre anchors to: "I" (start) or "F" (far). Default "I".</param>
    /// <param name="side">Flange edge the width anchors to: "A" (Z-min) or "B" (Z-max). Default "A".</param>
    /// <param name="tolerance">Per-parameter symmetric tolerance; defaults to ±0.5 mm.</param>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException">Fewer than 3 scan points.</exception>
    /// <exception cref="InvalidOperationException">Axis/circle could not be derived from the inputs.</exception>
    public FeatureInspectionReport Inspect(
        FeatureCutter hole,
        IReadOnlyList<Point3D> scanPoints,
        HoleNominals nominal,
        BeamDatumFrame datums,
        string vx = "I",
        string side = "A",
        HoleTolerance? tolerance = null)
    {
        ArgumentNullException.ThrowIfNull(hole);
        ArgumentNullException.ThrowIfNull(scanPoints);
        if (scanPoints.Count < 3)
            throw new ArgumentException("A hole measurement needs at least 3 scan points.", nameof(scanPoints));

        HoleTolerance tol = tolerance ?? HoleTolerance.Default;

        CylinderAxis axis = CutterAxis.FromCutter(hole.Cutter);
        Vec3 dir = axis.Direction.Normalized();
        (Vec3 uHat, Vec3 vHat) = OrthonormalBasis(dir);

        int n = scanPoints.Count;
        var u = new double[n];
        var v = new double[n];
        double tMin = double.PositiveInfinity, tMax = double.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            Point3D p = scanPoints[i];
            Vec3 w = new Vec3(p.X, p.Y, p.Z) - axis.Point;
            u[i] = Vec3.Dot(w, uHat);
            v[i] = Vec3.Dot(w, vHat);
            double t = Vec3.Dot(w, dir);
            if (t < tMin) tMin = t;
            if (t > tMax) tMax = t;
        }

        CircleFitResult fit = CircleFit.Fit(u, v);
        double measuredDiameter = 2.0 * fit.Radius;
        double measuredDepth = tMax - tMin;

        // World centre = on-axis point + in-plane circle centre + the mid-span along the axis.
        double tMid = 0.5 * (tMin + tMax);
        Vec3 center = axis.Point + uHat * fit.CenterU + vHat * fit.CenterV + dir * tMid;

        // Feature-relative centre: x = length, y = width, z = height (see BeamDatumFrame.ToFeatureFrame).
        (double cx, double cy, double cz) = datums.ToFeatureFrame(center.X, center.Y, center.Z, vx, side);

        var parameters = new[]
        {
            FeatureParameter.Judge("Diameter", nominal.DiameterMm, measuredDiameter, tol.DiameterMm),
            FeatureParameter.Judge("Depth", nominal.DepthMm, measuredDepth, tol.DepthMm),
            FeatureParameter.Judge("CenterX", nominal.CenterXMm, cx, tol.CenterMm),
            FeatureParameter.Judge("CenterY", nominal.CenterYMm, cy, tol.CenterMm),
            FeatureParameter.Judge("CenterZ", nominal.CenterZMm, cz, tol.CenterMm),
        };

        return new FeatureInspectionReport
        {
            Feature = hole.Descriptor,
            Parameters = parameters,
            PointCount = n,
        };
    }

    /// <summary>
    /// The nominal bore diameter encoded in an INTC (hole) macro's parameters. INTC01 builds the bore as a
    /// cylinder of radius <c>C/2</c> (or <c>F/2</c> on round profiles), so the diameter is the <c>C</c>
    /// parameter (falling back to <c>F</c>). This is the design value the scan is checked against.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="macro"/> is null.</exception>
    /// <exception cref="ArgumentException">The macro is not a hole macro or carries no diameter parameter.</exception>
    public static double NominalDiameterFromMacro(MacroSpec macro)
    {
        ArgumentNullException.ThrowIfNull(macro);
        if (FeatureKinds.FromMacroClassName(macro.MacroClassName) != FeatureKind.Hole)
            throw new ArgumentException(
                $"Macro '{macro.MacroClassName}' is not a hole macro.", nameof(macro));

        double c = macro.Parameters.GetValueOrDefault("C");
        double f = macro.Parameters.GetValueOrDefault("F");
        double diameter = c > 0 ? c : f;
        if (diameter <= 0)
            throw new ArgumentException(
                $"Hole macro '{macro.MacroClassName}' carries no positive diameter parameter (C or F).",
                nameof(macro));
        return diameter;
    }

    /// <summary>
    /// The full nominal geometry of an INTC (hole) macro on a given beam: the bore diameter (from the macro)
    /// plus the centre placement and through depth. The centre's length (<c>A</c>) and height (<c>B</c>) are
    /// macro parameters; the width datum is the half flange width (a web bore is centred across the section)
    /// and the depth defaults to the web thickness it drills through — a sensible default that the caller can
    /// override once an explicit depth parameter exists (see <c>docs/STEP5.5-HANDOFF.md</c> §5 Phase B).
    /// </summary>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="ArgumentException">The macro is not a hole macro or carries no diameter parameter.</exception>
    public static HoleNominals NominalsFromMacro(MacroSpec macro, BeamSpec beam)
    {
        ArgumentNullException.ThrowIfNull(macro);
        ArgumentNullException.ThrowIfNull(beam);

        double diameter = NominalDiameterFromMacro(macro);
        double centerX = macro.Parameters.GetValueOrDefault("A"); // length along the beam
        double centerY = beam.SB / 2.0;                           // width: web bore centred across the flange
        double centerZ = macro.Parameters.GetValueOrDefault("B"); // height up the web
        double depth = beam.TA;
        return new HoleNominals(diameter, depth, centerX, centerY, centerZ);
    }

    /// <summary>Builds a right-handed orthonormal basis (u, v) spanning the plane perpendicular to <paramref name="axis"/>.</summary>
    private static (Vec3 U, Vec3 V) OrthonormalBasis(Vec3 axis)
    {
        Vec3 a = axis.Normalized();
        Vec3 seed = Math.Abs(a.X) < 0.9 ? new Vec3(1, 0, 0) : new Vec3(0, 1, 0);
        Vec3 u = Vec3.Cross(a, seed).Normalized();
        Vec3 v = Vec3.Cross(a, u).Normalized();
        return (u, v);
    }
}
